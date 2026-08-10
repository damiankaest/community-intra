using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace CommunityIntranet.Modules.Identity.Services;

public static class PasswordResetTokenCodec
{
    public static string Encode(string token) =>
        WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

    public static bool TryDecode(string encodedToken, out string token)
    {
        token = string.Empty;
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken));
            return token.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
