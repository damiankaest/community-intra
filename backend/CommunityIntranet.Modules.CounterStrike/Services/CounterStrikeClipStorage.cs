using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CommunityIntranet.Modules.CounterStrike.Services;

public sealed record StoredCounterStrikeClip(string Path, string OriginalFileName, string MimeType, long Size);

public interface ICounterStrikeClipStorage
{
    int MaximumMegabytes { get; }
    Task<StoredCounterStrikeClip> SaveAsync(
        Guid organizationId, IFormFile file, double startSeconds, double? endSeconds,
        string quality, CancellationToken cancellationToken);
    Stream OpenRead(string path);
    void Delete(string path);
}

public sealed class CounterStrikeClipStorage(IOptions<CounterStrikeOptions> options) : ICounterStrikeClipStorage
{
    private const int MaximumSizeMb = 100;
    private const int MaximumSourceSizeMb = 500;
    private static readonly HashSet<string> AllowedTypes =
        ["video/mp4", "video/quicktime", "video/webm"];
    private static readonly byte[] WebmHeader = [0x1a, 0x45, 0xdf, 0xa3];

    public int MaximumMegabytes => MaximumSizeMb;

    public async Task<StoredCounterStrikeClip> SaveAsync(
        Guid organizationId, IFormFile file, double startSeconds, double? endSeconds,
        string quality, CancellationToken cancellationToken)
    {
        var mimeType = file.ContentType.ToLowerInvariant();
        if (!AllowedTypes.Contains(mimeType))
        {
            throw new CounterStrikeUploadException("file", "Erlaubt sind MP4-, WebM- und MOV-Videos.");
        }
        var maximumBytes = MaximumSourceSizeMb * 1024L * 1024L;
        if (file.Length <= 0 || file.Length > maximumBytes)
        {
            throw new CounterStrikeUploadException("file", $"Die Quelldatei darf höchstens {MaximumSourceSizeMb} MB groß sein.");
        }
        if (startSeconds < 0 || endSeconds is <= 0 || endSeconds is not null && endSeconds <= startSeconds)
            throw new CounterStrikeUploadException("trim", "Der gewählte Schnittbereich ist ungültig.");
        if (endSeconds - startSeconds > 600)
            throw new CounterStrikeUploadException("trim", "Ein Clip darf höchstens zehn Minuten lang sein.");
        quality = quality.ToLowerInvariant();
        if (quality is not ("high" or "balanced" or "compact"))
            throw new CounterStrikeUploadException("quality", "Diese Qualitätsstufe ist ungültig.");

        var extension = mimeType switch
        {
            "video/webm" => ".webm",
            "video/quicktime" => ".mov",
            _ => ".mp4"
        };
        var directory = Path.Combine(Root, organizationId.ToString("N"), "clips");
        Directory.CreateDirectory(directory);
        var id = Guid.NewGuid().ToString("N");
        var sourcePath = Path.Combine(directory, $".{id}.source{extension}");
        var path = Path.Combine(directory, $"{id}.mp4");
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
            await using (var output = new FileStream(sourcePath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await input.CopyToAsync(output, cancellationToken);
                await output.FlushAsync(cancellationToken);
            }
            await TranscodeAsync(sourcePath, path, startSeconds, endSeconds, quality, cancellationToken);
            var outputSize = new FileInfo(path).Length;
            if (outputSize > MaximumSizeMb * 1024L * 1024L)
                throw new CounterStrikeUploadException("file", "Der fertige Clip ist noch größer als 100 MB. Bitte kürzer schneiden oder eine kleinere Qualität wählen.");
            return new StoredCounterStrikeClip(path, SafeFileName(file.FileName), "video/mp4", outputSize);
        }
        catch
        {
            if (File.Exists(path)) File.Delete(path);
            throw;
        }
        finally
        {
            if (File.Exists(sourcePath)) File.Delete(sourcePath);
        }
    }

    public Stream OpenRead(string path) => new FileStream(Resolve(path), FileMode.Open, FileAccess.Read, FileShare.Read);

    public void Delete(string path)
    {
        var resolved = Resolve(path);
        if (File.Exists(resolved)) File.Delete(resolved);
    }

    private string Root => Path.GetFullPath(options.Value.StorageRoot);

    private static async Task TranscodeAsync(
        string sourcePath, string targetPath, double startSeconds, double? endSeconds,
        string quality, CancellationToken cancellationToken)
    {
        var (height, crf) = quality switch
        {
            "high" => (1080, 20),
            "compact" => (480, 29),
            _ => (720, 24)
        };
        var startInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("-hide_banner");
        startInfo.ArgumentList.Add("-loglevel");
        startInfo.ArgumentList.Add("error");
        startInfo.ArgumentList.Add("-ss");
        startInfo.ArgumentList.Add(startSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture));
        startInfo.ArgumentList.Add("-i");
        startInfo.ArgumentList.Add(sourcePath);
        if (endSeconds is not null)
        {
            startInfo.ArgumentList.Add("-t");
            startInfo.ArgumentList.Add((endSeconds.Value - startSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        startInfo.ArgumentList.Add("-vf");
        startInfo.ArgumentList.Add($"scale=-2:min({height}\\,ih)");
        startInfo.ArgumentList.Add("-c:v");
        startInfo.ArgumentList.Add("libx264");
        startInfo.ArgumentList.Add("-preset");
        startInfo.ArgumentList.Add("veryfast");
        startInfo.ArgumentList.Add("-crf");
        startInfo.ArgumentList.Add(crf.ToString());
        startInfo.ArgumentList.Add("-c:a");
        startInfo.ArgumentList.Add("aac");
        startInfo.ArgumentList.Add("-b:a");
        startInfo.ArgumentList.Add("128k");
        startInfo.ArgumentList.Add("-movflags");
        startInfo.ArgumentList.Add("+faststart");
        startInfo.ArgumentList.Add("-y");
        startInfo.ArgumentList.Add(targetPath);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("FFmpeg konnte nicht gestartet werden.");
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        try { await process.WaitForExitAsync(cancellationToken); }
        catch { if (!process.HasExited) process.Kill(entireProcessTree: true); throw; }
        var error = await errorTask;
        if (process.ExitCode != 0)
            throw new CounterStrikeUploadException("file", $"Der Clip konnte nicht verarbeitet werden: {error[..Math.Min(error.Length, 240)]}");
    }

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
