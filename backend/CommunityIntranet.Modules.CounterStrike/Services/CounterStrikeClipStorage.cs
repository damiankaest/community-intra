using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CommunityIntranet.Modules.CounterStrike.Services;

public sealed record StoredCounterStrikeClip(string Path, string OriginalFileName, string MimeType, long Size);

public interface ICounterStrikeClipStorage
{
    int MaximumMegabytes { get; }
    Task<StoredCounterStrikeClip> SaveAsync(Guid organizationId, IFormFile file, CancellationToken cancellationToken);
    Stream OpenRead(string path);
    void Delete(string path);
}

public sealed class CounterStrikeClipStorage(IOptions<CounterStrikeOptions> options) : ICounterStrikeClipStorage
{
    private const int MaximumSizeMb = 100;
    private static readonly HashSet<string> AllowedTypes =
        ["video/mp4", "video/quicktime", "video/webm"];
    private static readonly byte[] WebmHeader = [0x1a, 0x45, 0xdf, 0xa3];

    public int MaximumMegabytes => MaximumSizeMb;

    public async Task<StoredCounterStrikeClip> SaveAsync(
        Guid organizationId, IFormFile file, CancellationToken cancellationToken)
    {
        var mimeType = file.ContentType.ToLowerInvariant();
        if (!AllowedTypes.Contains(mimeType))
        {
            throw new CounterStrikeUploadException("file", "Erlaubt sind MP4-, WebM- und MOV-Videos.");
        }
        var maximumBytes = MaximumSizeMb * 1024L * 1024L;
        if (file.Length <= 0 || file.Length > maximumBytes)
        {
            throw new CounterStrikeUploadException("file", $"Der Clip darf höchstens {MaximumSizeMb} MB groß sein.");
        }

        var extension = mimeType switch
        {
            "video/webm" => ".webm",
            "video/quicktime" => ".mov",
            _ => ".mp4"
        };
        var directory = Path.Combine(Root, organizationId.ToString("N"), "clips");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{Guid.NewGuid():N}{extension}");
        try
        {
            await using var input = file.OpenReadStream();
            var header = new byte[12];
            var headerLength = await input.ReadAsync(header, cancellationToken);
            var validHeader = mimeType == "video/webm"
                ? headerLength >= 4 && header.AsSpan(0, 4).SequenceEqual(WebmHeader)
                : headerLength >= 8 && header.AsSpan(4, 4).SequenceEqual("ftyp"u8);
            if (!validHeader)
            {
                throw new CounterStrikeUploadException("file", "Die Datei besitzt keinen gültigen Video-Header.");
            }
            input.Position = 0;
            await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
            await input.CopyToAsync(output, cancellationToken);
            return new StoredCounterStrikeClip(path, SafeFileName(file.FileName), mimeType, output.Length);
        }
        catch
        {
            if (File.Exists(path)) File.Delete(path);
            throw;
        }
    }

    public Stream OpenRead(string path) => new FileStream(Resolve(path), FileMode.Open, FileAccess.Read, FileShare.Read);

    public void Delete(string path)
    {
        var resolved = Resolve(path);
        if (File.Exists(resolved)) File.Delete(resolved);
    }

    private string Root => Path.GetFullPath(options.Value.StorageRoot);

    private string Resolve(string path)
    {
        var resolved = Path.GetFullPath(path);
        var root = Root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!resolved.StartsWith(root, StringComparison.Ordinal))
            throw new InvalidOperationException("Ungültiger Clip-Speicherpfad.");
        return resolved;
    }

    private static string SafeFileName(string value)
    {
        var name = new string(Path.GetFileName(value).Where(character => !char.IsControl(character)).ToArray()).Trim();
        return name.Length == 0 ? "clip.mp4" : name[..Math.Min(name.Length, 255)];
    }
}
