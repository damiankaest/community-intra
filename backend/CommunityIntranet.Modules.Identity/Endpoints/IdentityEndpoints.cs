using System.Security.Claims;
using CommunityIntranet.Modules.Identity.Contracts;
using CommunityIntranet.Modules.Identity.Domain;
using CommunityIntranet.Modules.Identity.Persistence;
using CommunityIntranet.Modules.Identity.Security;
using CommunityIntranet.Modules.Identity.Services;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CommunityIntranet.Modules.Identity.Endpoints;

public static partial class IdentityEndpoints
{
    private const string RefreshCookieName = "community_refresh";

    public static IEndpointRouteBuilder MapIdentityEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth").WithTags("Authentication");

        group.MapPost("/register", RegisterAsync)
            .RequireRateLimiting("authentication");
        group.MapPost("/login", LoginAsync)
            .RequireRateLimiting("authentication");
        group.MapPost("/refresh", RefreshAsync)
            .RequireRateLimiting("authentication");
        group.MapPost("/logout", LogoutAsync);
        group.MapPost("/forgot-password", ForgotPasswordAsync)
            .RequireRateLimiting("authentication");
        group.MapPost("/reset-password", ResetPasswordAsync)
            .RequireRateLimiting("authentication");
        group.MapGet("/me", GetCurrentUserAsync)
            .RequireAuthorization();

        ExternalIdentityEndpoints.Map(endpoints);

        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        IValidator<RegisterRequest> validator,
        UserManager<ApplicationUser> userManager,
        IIdentityDbContext dbContext,
        TokenService tokenService,
        IWebHostEnvironment environment,
        HttpContext httpContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(ToValidationDictionary(validation.Errors));
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            DisplayName = request.DisplayName.Trim(),
            CreatedAt = timeProvider.GetUtcNow(),
            IsActive = true
        };
        var identityResult = await userManager.CreateAsync(user, request.Password);

        if (!identityResult.Succeeded)
        {
            var errors = identityResult.Errors
                .GroupBy(error => error.Code)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(error => error.Description).ToArray());
            return Results.ValidationProblem(errors);
        }

        return await CreateSessionAsync(
            user,
            Guid.NewGuid(),
            dbContext,
            tokenService,
            environment,
            httpContext,
            cancellationToken);
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        IValidator<LoginRequest> validator,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IIdentityDbContext dbContext,
        TokenService tokenService,
        IWebHostEnvironment environment,
        HttpContext httpContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(ToValidationDictionary(validation.Errors));
        }

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive)
        {
            return Results.Unauthorized();
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);
        if (!signInResult.Succeeded)
        {
            return Results.Unauthorized();
        }

        user.LastLoginAt = timeProvider.GetUtcNow();
        await userManager.UpdateAsync(user);

        return await CreateSessionAsync(
            user,
            Guid.NewGuid(),
            dbContext,
            tokenService,
            environment,
            httpContext,
            cancellationToken);
    }

    private static async Task<IResult> RefreshAsync(
        UserManager<ApplicationUser> userManager,
        IIdentityDbContext dbContext,
        TokenService tokenService,
        IWebHostEnvironment environment,
        HttpContext httpContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (!httpContext.Request.Cookies.TryGetValue(
                RefreshCookieName,
                out var rawToken)
            || string.IsNullOrWhiteSpace(rawToken))
        {
            return Results.Unauthorized();
        }

        var tokenHash = TokenService.HashRefreshToken(rawToken);
        var storedToken = await dbContext.RefreshTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(
                token => token.TokenHash == tokenHash,
                cancellationToken);
        var now = timeProvider.GetUtcNow();

        if (storedToken is null)
        {
            ClearRefreshCookie(httpContext, environment);
            return Results.Unauthorized();
        }

        if (storedToken.RevokedAt is not null)
        {
            await RevokeFamilyAsync(
                dbContext,
                storedToken.FamilyId,
                now,
                "Refresh token reuse detected",
                cancellationToken);
            ClearRefreshCookie(httpContext, environment);
            return Results.Unauthorized();
        }

        if (storedToken.ExpiresAt <= now)
        {
            ClearRefreshCookie(httpContext, environment);
            return Results.Unauthorized();
        }

        var user = await userManager.FindByIdAsync(storedToken.UserId.ToString());
        if (user is null || !user.IsActive)
        {
            await RevokeFamilyAsync(
                dbContext,
                storedToken.FamilyId,
                now,
                "User unavailable",
                cancellationToken);
            ClearRefreshCookie(httpContext, environment);
            return Results.Unauthorized();
        }

        var nextRefreshToken = tokenService.CreateRefreshToken(
            user.Id,
            storedToken.FamilyId,
            GetClientIp(httpContext));
        var updated = await dbContext.RefreshTokens
            .Where(token => token.Id == storedToken.Id && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAt, now)
                    .SetProperty(
                        token => token.ReplacedByTokenHash,
                        nextRefreshToken.Token.TokenHash)
                    .SetProperty(token => token.RevocationReason, "Rotated"),
                cancellationToken);

        if (updated != 1)
        {
            await RevokeFamilyAsync(
                dbContext,
                storedToken.FamilyId,
                now,
                "Concurrent refresh detected",
                cancellationToken);
            ClearRefreshCookie(httpContext, environment);
            return Results.Unauthorized();
        }

        dbContext.RefreshTokens.Add(nextRefreshToken.Token);
        await dbContext.SaveChangesAsync(cancellationToken);
        WriteRefreshCookie(
            httpContext,
            environment,
            nextRefreshToken.RawToken,
            nextRefreshToken.Token.ExpiresAt);

        return Results.Ok(CreateAuthResponse(user, tokenService.CreateAccessToken(user)));
    }

    private static async Task<IResult> LogoutAsync(
        IIdentityDbContext dbContext,
        IWebHostEnvironment environment,
        HttpContext httpContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (httpContext.Request.Cookies.TryGetValue(
                RefreshCookieName,
                out var rawToken)
            && !string.IsNullOrWhiteSpace(rawToken))
        {
            var tokenHash = TokenService.HashRefreshToken(rawToken);
            var now = timeProvider.GetUtcNow();
            await dbContext.RefreshTokens
                .Where(token => token.TokenHash == tokenHash && token.RevokedAt == null)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(token => token.RevokedAt, now)
                        .SetProperty(token => token.RevocationReason, "Logout"),
                    cancellationToken);
        }

        ClearRefreshCookie(httpContext, environment);
        return Results.NoContent();
    }

    private static async Task<IResult> GetCurrentUserAsync(
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.GetUserAsync(principal);
        return user is null || !user.IsActive
            ? Results.Unauthorized()
            : Results.Ok(ToCurrentUser(user));
    }

    private static async Task<IResult> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        UserManager<ApplicationUser> userManager,
        IIdentityEmailSender emailSender,
        IOptions<IdentityPublicOptions> publicOptions,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var response = new
        {
            message = "Wenn ein Konto zu dieser E-Mail-Adresse existiert, wurde ein Reset-Link versendet."
        };
        if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Length > 320)
        {
            return Results.Accepted(value: response);
        }

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive || string.IsNullOrWhiteSpace(user.Email))
        {
            return Results.Accepted(value: response);
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = PasswordResetTokenCodec.Encode(token);
        var baseUrl = publicOptions.Value.PublicAppUrl.TrimEnd('/');
        var resetUrl = $"{baseUrl}/reset-password?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(encodedToken)}";
        try
        {
            await emailSender.SendPasswordResetAsync(
                user.Email,
                user.DisplayName,
                resetUrl,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            LogResetEmailFailure(loggerFactory.CreateLogger("PasswordReset"), exception);
        }

        return Results.Accepted(value: response);
    }

    private static async Task<IResult> ResetPasswordAsync(
        ResetPasswordRequest request,
        UserManager<ApplicationUser> userManager,
        IIdentityDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Token)
            || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["reset"] = ["Der Reset-Link oder das neue Passwort ist ungültig."]
            });
        }

        if (!PasswordResetTokenCodec.TryDecode(request.Token, out var token))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["token"] = ["Der Reset-Link ist ungültig oder abgelaufen."]
            });
        }

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["token"] = ["Der Reset-Link ist ungültig oder abgelaufen."]
            });
        }

        var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);
        if (!result.Succeeded)
        {
            return Results.ValidationProblem(result.Errors
                .GroupBy(error => error.Code)
                .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray()));
        }

        await userManager.UpdateSecurityStampAsync(user);
        var now = timeProvider.GetUtcNow();
        await dbContext.RefreshTokens
            .Where(refreshToken => refreshToken.UserId == user.Id && refreshToken.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(refreshToken => refreshToken.RevokedAt, now)
                .SetProperty(refreshToken => refreshToken.RevocationReason, "Password reset"), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> CreateSessionAsync(
        ApplicationUser user,
        Guid tokenFamilyId,
        IIdentityDbContext dbContext,
        TokenService tokenService,
        IWebHostEnvironment environment,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var accessToken = tokenService.CreateAccessToken(user);
        var refreshToken = tokenService.CreateRefreshToken(
            user.Id,
            tokenFamilyId,
            GetClientIp(httpContext));

        dbContext.RefreshTokens.Add(refreshToken.Token);
        await dbContext.SaveChangesAsync(cancellationToken);
        WriteRefreshCookie(
            httpContext,
            environment,
            refreshToken.RawToken,
            refreshToken.Token.ExpiresAt);

        return Results.Ok(CreateAuthResponse(user, accessToken));
    }

    private static AuthResponse CreateAuthResponse(
        ApplicationUser user,
        AccessTokenResult accessToken) =>
        new(
            accessToken.Token,
            accessToken.ExpiresAt,
            ToCurrentUser(user));

    private static CurrentUserResponse ToCurrentUser(ApplicationUser user) =>
        new(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            user.AvatarUrl);

    private static async Task RevokeFamilyAsync(
        IIdentityDbContext dbContext,
        Guid familyId,
        DateTimeOffset revokedAt,
        string reason,
        CancellationToken cancellationToken)
    {
        await dbContext.RefreshTokens
            .Where(token => token.FamilyId == familyId && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAt, revokedAt)
                    .SetProperty(token => token.RevocationReason, reason),
                cancellationToken);
    }

    private static void WriteRefreshCookie(
        HttpContext httpContext,
        IWebHostEnvironment environment,
        string rawToken,
        DateTimeOffset expiresAt)
    {
        httpContext.Response.Cookies.Append(
            RefreshCookieName,
            rawToken,
            CreateCookieOptions(environment, expiresAt));
    }

    private static void ClearRefreshCookie(
        HttpContext httpContext,
        IWebHostEnvironment environment)
    {
        httpContext.Response.Cookies.Delete(
            RefreshCookieName,
            CreateCookieOptions(environment, DateTimeOffset.UnixEpoch));
    }

    private static CookieOptions CreateCookieOptions(
        IWebHostEnvironment environment,
        DateTimeOffset expiresAt) =>
        new()
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth",
            Expires = expiresAt,
            IsEssential = true
        };

    private static string GetClientIp(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static Dictionary<string, string[]> ToValidationDictionary(
        IEnumerable<FluentValidation.Results.ValidationFailure> failures) =>
        failures
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray());

    [LoggerMessage(EventId = 2410, Level = LogLevel.Error,
        Message = "Password reset email delivery failed")]
    private static partial void LogResetEmailFailure(ILogger logger, Exception exception);
}
