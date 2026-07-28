namespace CommunityIntranet.Modules.Identity.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public string SigningKey { get; init; } = string.Empty;

    public int AccessTokenMinutes { get; init; } = 15;

    public int RefreshTokenDays { get; init; } = 30;

    public bool IsValid() =>
        Uri.TryCreate(Issuer, UriKind.Absolute, out _)
        && !string.IsNullOrWhiteSpace(Audience)
        && SigningKey.Length >= 32
        && AccessTokenMinutes is >= 5 and <= 60
        && RefreshTokenDays is >= 1 and <= 90;
}
