using Microsoft.Extensions.Options;

namespace CommunityIntranet.Modules.Parties.Services;

public sealed class PartyMediaOptions
{
    public const string SectionName = "PartyMedia";
    public string RootPath { get; set; } = "party-media";
}

public interface IPartyMediaStorage
{
    Task<string> SaveAsync(Guid partyId, Stream content, string extension, CancellationToken cancellationToken);
    Task<Stream?> OpenReadAsync(string storagePath, CancellationToken cancellationToken);
    Task DeleteAsync(string storagePath, CancellationToken cancellationToken);
}

public sealed class PartyMediaStorage(IOptions<PartyMediaOptions> options) : IPartyMediaStorage
{
    private readonly string rootPath = Path.GetFullPath(options.Value.RootPath);

    public async Task<string> SaveAsync(
        Guid partyId,
        Stream content,
        string extension,
        CancellationToken cancellationToken)
    {
        var relativePath = Path.Combine(
            partyId.ToString("N"),
            $"{Guid.NewGuid():N}{extension}");
        var fullPath = Resolve(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var output = new FileStream(
            fullPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        await content.CopyToAsync(output, cancellationToken);
        return relativePath.Replace(Path.DirectorySeparatorChar, '/');
    }

    public Task<Stream?> OpenReadAsync(string storagePath, CancellationToken cancellationToken)
    {
        var fullPath = Resolve(storagePath);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken)
    {
        var fullPath = Resolve(storagePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private string Resolve(string storagePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(rootPath, storagePath));
        var prefix = rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? rootPath
            : rootPath + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(prefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invalid party media path.");
        }

        return fullPath;
    }
}
