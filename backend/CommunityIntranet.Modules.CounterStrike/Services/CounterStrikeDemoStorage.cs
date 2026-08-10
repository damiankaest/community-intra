using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CommunityIntranet.Modules.CounterStrike.Services;

public sealed record StoredCounterStrikeDemo(
    string Checksum,
    string Path,
    string OriginalFileName,
    long Size);

public interface ICounterStrikeDemoStorage
{
    int MaximumDemoMegabytes { get; }

    Task<StoredCounterStrikeDemo> SaveAsync(
        Guid organizationId,
        IFormFile file,
        CancellationToken cancellationToken);

    string GetArtifactPath(Guid organizationId, Guid matchId);
}

public sealed class CounterStrikeDemoStorage(IOptions<CounterStrikeOptions> options)
    : ICounterStrikeDemoStorage
{
    private static readonly byte[] SourceOneHeader = "HL2DEMO"u8.ToArray();
    private static readonly byte[] SourceTwoHeader = "PBDEMS2"u8.ToArray();

    public int MaximumDemoMegabytes => options.Value.MaximumDemoMegabytes;

    public async Task<StoredCounterStrikeDemo> SaveAsync(
        Guid organizationId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(Path.GetExtension(file.FileName), ".dem", StringComparison.OrdinalIgnoreCase))
        {
            throw new CounterStrikeUploadException("file", "Bitte wähle eine Counter-Strike-Demo mit der Endung .dem.");
        }

        var maximumBytes = Math.Clamp(options.Value.MaximumDemoMegabytes, 16, 2048) * 1024L * 1024L;
        if (file.Length <= 0 || file.Length > maximumBytes)
        {
            throw new CounterStrikeUploadException(
                "file",
                $"Die Demo darf höchstens {options.Value.MaximumDemoMegabytes} MB groß sein.");
        }

        var directory = Path.Combine(
            Path.GetFullPath(options.Value.StorageRoot),
            organizationId.ToString("N"),
            "demos");
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(directory, $".{Guid.NewGuid():N}.upload");
        var total = 0L;
        var header = new byte[SourceTwoHeader.Length];
        var headerLength = 0;
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        try
        {
            await using var input = file.OpenReadStream();
            await using (var output = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                64 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                var buffer = new byte[64 * 1024];
                int read;
                while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    total += read;
                    if (total > maximumBytes)
                    {
                        throw new CounterStrikeUploadException("file", "Die Demo überschreitet das Upload-Limit.");
                    }

                    if (headerLength < header.Length)
                    {
                        var copyLength = Math.Min(read, header.Length - headerLength);
                        buffer.AsSpan(0, copyLength).CopyTo(header.AsSpan(headerLength));
                        headerLength += copyLength;
                    }

                    hash.AppendData(buffer, 0, read);
                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }
            }

            if (headerLength < SourceOneHeader.Length
                || (!header.AsSpan(0, SourceOneHeader.Length).SequenceEqual(SourceOneHeader)
                    && !header.AsSpan(0, SourceTwoHeader.Length).SequenceEqual(SourceTwoHeader)))
            {
                throw new CounterStrikeUploadException("file", "Die Datei besitzt keinen gültigen CS-Demo-Header.");
            }

            var checksum = Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
            var finalPath = Path.Combine(directory, $"{checksum}.dem");
            if (!File.Exists(finalPath))
            {
                File.Move(temporaryPath, finalPath);
            }
            else
            {
                File.Delete(temporaryPath);
            }

            return new StoredCounterStrikeDemo(
                checksum,
                finalPath,
                SafeFileName(file.FileName),
                total);
        }
        catch
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }

            throw;
        }
    }

    public string GetArtifactPath(Guid organizationId, Guid matchId) =>
        Path.Combine(
            Path.GetFullPath(options.Value.StorageRoot),
            organizationId.ToString("N"),
            "imports",
            $"{matchId:N}.json");

    private static string SafeFileName(string value)
    {
        var fileName = new string(Path.GetFileName(value)
            .Where(character => !char.IsControl(character))
            .ToArray())
            .Trim();
        if (fileName.Length == 0)
        {
            return "match.dem";
        }
        return fileName[..Math.Min(fileName.Length, 255)];
    }
}

public sealed class CounterStrikeUploadException(string key, string message)
    : Exception(message)
{
    public string Key { get; } = key;
}
