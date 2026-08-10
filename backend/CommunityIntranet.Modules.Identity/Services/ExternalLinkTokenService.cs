using System.Globalization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace CommunityIntranet.Modules.Identity.Services;

public sealed class ExternalLinkTokenService(IDataProtectionProvider dataProtectionProvider, TimeProvider timeProvider)
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(
        "CommunityIntranet.Identity.ExternalLink.v1");

    public string Create(Guid userId)
    {
        var expires = timeProvider.GetUtcNow().AddMinutes(10).ToUnixTimeSeconds();
        return _protector.Protect($"{userId:N}|{expires.ToString(CultureInfo.InvariantCulture)}");
    }

    public bool TryRead(string? token, out Guid userId)
    {
        userId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }
        try
        {
            var parts = _protector.Unprotect(token).Split('|');
            return parts.Length == 2
                && Guid.TryParseExact(parts[0], "N", out userId)
                && long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var expires)
                && expires >= timeProvider.GetUtcNow().ToUnixTimeSeconds();
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException)
        {
            return false;
        }
    }
}
