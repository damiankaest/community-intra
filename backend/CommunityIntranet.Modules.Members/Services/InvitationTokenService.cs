using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace CommunityIntranet.Modules.Members.Services;

public sealed class InvitationTokenService
{
    public InvitationToken Create()
    {
        var rawToken = WebEncoders.Base64UrlEncode(
            RandomNumberGenerator.GetBytes(32));
        return new InvitationToken(rawToken, Hash(rawToken));
    }

    public static string Hash(string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
    }
}

public sealed record InvitationToken(string RawToken, string TokenHash);
