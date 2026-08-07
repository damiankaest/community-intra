using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace CommunityIntranet.Modules.Parties.Services;

public static class PartySlugGenerator
{
    private const string Alphabet = "abcdefghjkmnpqrstuvwxyz23456789";

    public static string Create(string name, int year)
    {
        var normalized = name.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        var separator = false;
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsAsciiLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                separator = false;
            }
            else if (!separator && builder.Length > 0)
            {
                builder.Append('-');
                separator = true;
            }
        }

        var baseSlug = builder.ToString().Trim('-');
        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            baseSlug = "party";
        }

        baseSlug = baseSlug[..Math.Min(baseSlug.Length, 120)];
        return $"{baseSlug}-{year}-{RandomSuffix()}";
    }

    private static string RandomSuffix()
    {
        Span<byte> bytes = stackalloc byte[5];
        RandomNumberGenerator.Fill(bytes);
        Span<char> result = stackalloc char[5];
        for (var index = 0; index < bytes.Length; index++)
        {
            result[index] = Alphabet[bytes[index] % Alphabet.Length];
        }

        return new string(result);
    }
}
