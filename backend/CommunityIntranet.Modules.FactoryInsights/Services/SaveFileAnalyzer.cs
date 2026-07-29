using System.Net.Http.Headers;
using System.Net.Http.Json;
using CommunityIntranet.Modules.FactoryInsights.Contracts;

namespace CommunityIntranet.Modules.FactoryInsights.Services;

public interface ISaveFileAnalyzer
{
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken);

    Task<SaveAnalysis> AnalyzeAsync(
        ReadOnlyMemory<byte> content,
        string fileName,
        CancellationToken cancellationToken);
}

public sealed class SaveFileAnalyzer(HttpClient httpClient)
    : ISaveFileAnalyzer
{
    public async Task<bool> IsAvailableAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(
                "health",
                cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    public async Task<SaveAnalysis> AnalyzeAsync(
        ReadOnlyMemory<byte> content,
        string fileName,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "analyze");
        request.Headers.TryAddWithoutValidation(
            "X-Save-File-Name",
            SafeFileName(fileName));
        request.Content = new ReadOnlyMemoryContent(content);
        request.Content.Headers.ContentType =
            new MediaTypeHeaderValue("application/octet-stream");
        using var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidDataException(
                response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity
                    ? "Die Datei ist kein lesbarer Satisfactory-Spielstand."
                    : "Der Save-Parser konnte die Datei nicht analysieren.");
        }

        return await response.Content.ReadFromJsonAsync<SaveAnalysis>(
                   cancellationToken)
               ?? throw new InvalidDataException(
                   "Der Save-Parser hat keine Analyse geliefert.");
    }

    private static string SafeFileName(string value) =>
        Path.GetFileName(value).Length > 160
            ? Path.GetFileName(value)[..160]
            : Path.GetFileName(value);
}
