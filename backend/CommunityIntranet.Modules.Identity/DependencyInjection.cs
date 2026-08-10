using System.Text;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using CommunityIntranet.Modules.Identity.Domain;
using CommunityIntranet.Modules.Identity.Security;
using CommunityIntranet.Modules.Identity.Services;
using CommunityIntranet.Modules.Identity.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CommunityIntranet.Modules.Identity;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityModule<TContext>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TContext : DbContext
    {
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();
        var externalSection = configuration.GetSection(ExternalLoginOptions.SectionName);
        var externalOptions = externalSection.Get<ExternalLoginOptions>() ?? new ExternalLoginOptions();

        services
            .AddOptions<JwtOptions>()
            .Bind(jwtSection)
            .Validate(options => options.IsValid(), "JWT configuration is invalid.")
            .ValidateOnStart();

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<TContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        var authentication = services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };
            })
            .AddCookie(IdentityConstants.ExternalScheme, options =>
            {
                options.Cookie.Name = "community_external";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            });

        if (externalOptions.Google.IsConfigured)
        {
            authentication.AddGoogle("Google", "Google", options =>
            {
                options.ClientId = externalOptions.Google.ClientId;
                options.ClientSecret = externalOptions.Google.ClientSecret;
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.CallbackPath = "/api/auth/external/google-signin";
                options.SaveTokens = false;
                options.Scope.Add("email");
                options.ClaimActions.MapJsonKey("picture", "picture");
                options.ClaimActions.MapJsonKey("email_verified", "email_verified");
            });
        }

        if (externalOptions.Discord.IsConfigured)
        {
            authentication.AddOAuth("Discord", "Discord", options =>
            {
                options.ClientId = externalOptions.Discord.ClientId;
                options.ClientSecret = externalOptions.Discord.ClientSecret;
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.AuthorizationEndpoint = "https://discord.com/oauth2/authorize";
                options.TokenEndpoint = "https://discord.com/api/oauth2/token";
                options.UserInformationEndpoint = "https://discord.com/api/users/@me";
                options.CallbackPath = "/api/auth/external/discord-signin";
                options.Scope.Add("identify");
                options.Scope.Add("email");
                options.UsePkce = true;
                options.SaveTokens = false;
                options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
                options.ClaimActions.MapJsonKey(ClaimTypes.Name, "global_name");
                options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
                options.ClaimActions.MapJsonKey("email_verified", "verified");
                options.Events = new OAuthEvents
                {
                    OnCreatingTicket = async context =>
                    {
                        using var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                        using var response = await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted);
                        response.EnsureSuccessStatusCode();
                        await using var stream = await response.Content.ReadAsStreamAsync(context.HttpContext.RequestAborted);
                        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: context.HttpContext.RequestAborted);
                        context.RunClaimActions(json.RootElement);
                    }
                };
            });
        }

        services.AddAuthorization();
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
        services.AddScoped<TokenService>();
        services.Configure<Microsoft.AspNetCore.Identity.DataProtectionTokenProviderOptions>(options =>
            options.TokenLifespan = TimeSpan.FromHours(1));
        services.AddOptions<IdentityPublicOptions>().BindConfiguration(IdentityPublicOptions.SectionName);
        services.AddOptions<IdentityEmailOptions>().BindConfiguration(IdentityEmailOptions.SectionName);
        services.AddOptions<ExternalLoginOptions>().Bind(externalSection);
        services.AddDataProtection().SetApplicationName("CommunityIntranet");
        services.AddScoped<IIdentityEmailSender, IdentityEmailSender>();
        services.AddSingleton<ExternalLinkTokenService>();
        services.AddHttpClient("SteamIdentity", client => client.Timeout = TimeSpan.FromSeconds(12));

        return services;
    }
}
