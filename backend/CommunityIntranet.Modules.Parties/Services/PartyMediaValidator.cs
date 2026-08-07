namespace CommunityIntranet.Modules.Parties.Services;

public sealed record PartyMediaValidation(string MediaType, string Extension, long MaximumSize);

public static class PartyMediaValidator
{
    public const long MaximumImageSize = 12 * 1024 * 1024;
    public const long MaximumVideoSize = 100 * 1024 * 1024;

    private static readonly IReadOnlyDictionary<string, PartyMediaValidation> Rules =
        new Dictionary<string, PartyMediaValidation>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = new("image", ".jpg", MaximumImageSize),
            ["image/png"] = new("image", ".png", MaximumImageSize),
            ["image/webp"] = new("image", ".webp", MaximumImageSize),
            ["image/gif"] = new("image", ".gif", MaximumImageSize),
            ["video/mp4"] = new("video", ".mp4", MaximumVideoSize),
            ["video/quicktime"] = new("video", ".mov", MaximumVideoSize),
            ["video/webm"] = new("video", ".webm", MaximumVideoSize)
        };

    public static PartyMediaValidation? GetRule(string contentType) =>
        Rules.GetValueOrDefault(contentType.Split(';')[0].Trim());

    public static async Task<bool> HasValidSignatureAsync(
        Stream stream,
        string contentType,
        CancellationToken cancellationToken)
    {
        if (!stream.CanSeek)
        {
            return false;
        }

        var buffer = new byte[16];
        var read = await stream.ReadAsync(buffer, cancellationToken);
        stream.Position = 0;
        if (read < 4)
        {
            return false;
        }

        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => buffer[0] == 0xff && buffer[1] == 0xd8 && buffer[2] == 0xff,
            "image/png" => buffer.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            "image/gif" => buffer.AsSpan(0, 6).SequenceEqual("GIF87a"u8) || buffer.AsSpan(0, 6).SequenceEqual("GIF89a"u8),
            "image/webp" => buffer.AsSpan(0, 4).SequenceEqual("RIFF"u8) && buffer.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            "video/mp4" or "video/quicktime" => read >= 12 && buffer.AsSpan(4, 4).SequenceEqual("ftyp"u8),
            "video/webm" => buffer.AsSpan(0, 4).SequenceEqual(new byte[] { 0x1a, 0x45, 0xdf, 0xa3 }),
            _ => false
        };
    }
}
