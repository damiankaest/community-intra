using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CommunityIntranet.Modules.CounterStrike.Services;

public sealed partial class CsdaDemoAnalyzer(
    IOptions<CounterStrikeOptions> options,
    ILogger<CsdaDemoAnalyzer> logger) : ICounterStrikeDemoAnalyzer
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    public async Task<CounterStrikeAnalyzerResult> AnalyzeAsync(
        string demoPath,
        string artifactPath,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        Directory.CreateDirectory(Path.GetDirectoryName(artifactPath)!);
        var startInfo = new ProcessStartInfo
        {
            FileName = settings.AnalyzerExecutable,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(artifactPath)!
        };
        startInfo.ArgumentList.Add($"-demo-path={Path.GetFullPath(demoPath)}");
        startInfo.ArgumentList.Add($"-output={Path.GetFullPath(artifactPath)}");
        startInfo.ArgumentList.Add("-format=json");
        startInfo.ArgumentList.Add("-minify");

        using var process = new Process { StartInfo = startInfo };
        var stopwatch = Stopwatch.StartNew();
        if (!process.Start())
        {
            throw new CounterStrikeAnalyzerException("parser_start_failed", "CSDA konnte nicht gestartet werden.");
        }

        var stderrTask = ReadLimitedAsync(process.StandardError, 4000);
        var stdoutTask = ReadLimitedAsync(process.StandardOutput, 2000);

        using var timeout = new CancellationTokenSource(
            TimeSpan.FromSeconds(Math.Clamp(settings.ParserTimeoutSeconds, 15, 900)));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeout.Token);
        try
        {
            await process.WaitForExitAsync(linked.Token);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            TryKill(process);
            throw new CounterStrikeAnalyzerException("parser_timeout", "Die Demo-Analyse hat das Zeitlimit überschritten.");
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }

        var stderr = await stderrTask;
        _ = await stdoutTask;
        if (process.ExitCode != 0)
        {
            LogParserFailure(process.ExitCode, stopwatch.Elapsed.TotalSeconds, stderr);
            throw new CounterStrikeAnalyzerException(
                "parser_failed",
                $"CSDA konnte diese Demo nicht lesen (Exit-Code {process.ExitCode.ToString(CultureInfo.InvariantCulture)})." +
                (string.IsNullOrWhiteSpace(stderr) ? string.Empty : " Details wurden sicher im Server-Log erfasst."));
        }

        await using var jsonStream = File.OpenRead(artifactPath);
        var match = await JsonSerializer.DeserializeAsync<AnalyzerMatchDto>(
            jsonStream,
            SerializerOptions,
            cancellationToken) ?? throw new CounterStrikeAnalyzerException(
                "invalid_parser_output",
                "CSDA hat kein lesbares Match erzeugt.");
        stopwatch.Stop();
        LogParserCompleted(stopwatch.Elapsed.TotalSeconds, match.Players.Count, match.Rounds.Count);
        return new CounterStrikeAnalyzerResult(match, artifactPath, stopwatch.Elapsed);
    }

    private static async Task<string> ReadLimitedAsync(StreamReader reader, int maximumCharacters)
    {
        var buffer = new char[Math.Min(maximumCharacters, 4096)];
        var builder = new StringBuilder();
        int read;
        while ((read = await reader.ReadAsync(buffer.AsMemory())) > 0)
        {
            if (builder.Length >= maximumCharacters)
            {
                continue;
            }

            builder.Append(buffer, 0, Math.Min(read, maximumCharacters - builder.Length));
        }

        return builder.ToString().Trim();
    }

    private static void TryKill(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
        }
    }

    [LoggerMessage(EventId = 4100, Level = LogLevel.Information,
        Message = "CSDA completed in {DurationSeconds:F2}s with {PlayerCount} players and {RoundCount} rounds")]
    private partial void LogParserCompleted(double durationSeconds, int playerCount, int roundCount);

    [LoggerMessage(EventId = 4101, Level = LogLevel.Warning,
        Message = "CSDA failed with exit code {ExitCode} after {DurationSeconds:F2}s: {ParserDiagnostics}")]
    private partial void LogParserFailure(int exitCode, double durationSeconds, string parserDiagnostics);
}

public sealed class CounterStrikeAnalyzerException(string code, string message)
    : Exception(message)
{
    public string Code { get; } = code;
}
