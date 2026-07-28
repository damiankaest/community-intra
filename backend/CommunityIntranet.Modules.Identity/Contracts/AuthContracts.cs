namespace CommunityIntranet.Modules.Identity.Contracts;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string DisplayName);

public sealed record LoginRequest(string Email, string Password);

public sealed record CurrentUserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    string? AvatarUrl);

public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    CurrentUserResponse User);
