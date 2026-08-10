using System.Threading.Channels;
using CommunityIntranet.Modules.CounterStrike.Domain;
using CommunityIntranet.Modules.CounterStrike.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CommunityIntranet.Modules.CounterStrike.Services;

public interface ICounterStrikeDemoQueue
{
    ValueTask QueueAsync(Guid matchId, CancellationToken cancellationToken);
}

public sealed class CounterStrikeDemoQueue : ICounterStrikeDemoQueue
{
    private readonly Channel<Guid> _channel;

    public CounterStrikeDemoQueue(IOptions<CounterStrikeOptions> options)
    {
        _channel = Channel.CreateBounded<Guid>(new BoundedChannelOptions(
            Math.Clamp(options.Value.QueueCapacity, 4, 128))
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public ValueTask QueueAsync(Guid matchId, CancellationToken cancellationToken) =>
        _channel.Writer.WriteAsync(matchId, cancellationToken);

    internal IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}

public sealed partial class CounterStrikeDemoWorker(
    CounterStrikeDemoQueue queue,
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<CounterStrikeDemoWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var matchId in await GetInterruptedImportsAsync(stoppingToken))
        {
            await ProcessAsync(matchId, stoppingToken);
        }
        await foreach (var matchId in queue.ReadAllAsync(stoppingToken))
        {
            await ProcessAsync(matchId, stoppingToken);
        }
    }

    private async Task<Guid[]> GetInterruptedImportsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ICounterStrikeDbContext>();
        return await dbContext.CounterStrikeMatches.AsNoTracking()
            .Where(match => match.Status == CounterStrikeDemoStatus.Uploaded
                || match.Status == CounterStrikeDemoStatus.Processing)
            .Select(match => match.Id)
            .ToArrayAsync(cancellationToken);
    }

    private async Task ProcessAsync(Guid matchId, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ICounterStrikeDbContext>();
        var match = await dbContext.CounterStrikeMatches.SingleOrDefaultAsync(
            item => item.Id == matchId,
            cancellationToken);
        if (match is null || match.Status == CounterStrikeDemoStatus.Completed)
        {
            return;
        }

        match.Status = CounterStrikeDemoStatus.Processing;
        match.ProcessingStartedAt = timeProvider.GetUtcNow();
        match.AttemptCount++;
        match.FailureCode = null;
        match.FailureMessage = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var storage = scope.ServiceProvider.GetRequiredService<ICounterStrikeDemoStorage>();
            var analyzer = scope.ServiceProvider.GetRequiredService<ICounterStrikeDemoAnalyzer>();
            var importer = scope.ServiceProvider.GetRequiredService<CounterStrikeMatchImporter>();
            var result = await analyzer.AnalyzeAsync(
                match.DemoStoragePath,
                storage.GetArtifactPath(match.OrganizationId, match.Id),
                cancellationToken);
            await importer.ImportAsync(match, result, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            var failureCode = exception is CounterStrikeAnalyzerException analyzerException
                ? analyzerException.Code
                : "import_failed";
            await MarkFailedAsync(matchId, failureCode, SafeMessage(exception));
            LogImportFailed(logger, matchId, failureCode, exception);
        }
    }

    private async Task MarkFailedAsync(Guid matchId, string failureCode, string failureMessage)
    {
        await using var failureScope = scopeFactory.CreateAsyncScope();
        var failureDbContext = failureScope.ServiceProvider.GetRequiredService<ICounterStrikeDbContext>();
        var failedMatch = await failureDbContext.CounterStrikeMatches.SingleOrDefaultAsync(
            item => item.Id == matchId,
            CancellationToken.None);
        if (failedMatch is null)
        {
            return;
        }

        failedMatch.Status = CounterStrikeDemoStatus.Failed;
        failedMatch.FailureCode = failureCode;
        failedMatch.FailureMessage = failureMessage;
        await failureDbContext.SaveChangesAsync(CancellationToken.None);
    }

    private static string SafeMessage(Exception exception)
    {
        var message = exception is CounterStrikeAnalyzerException or CounterStrikeUploadException
            ? exception.Message
            : "Die Demo konnte nicht verarbeitet werden. Bitte versuche es erneut.";
        return message[..Math.Min(message.Length, 500)];
    }

    [LoggerMessage(EventId = 4120, Level = LogLevel.Error,
        Message = "CS2 demo import {MatchId} failed with {FailureCode}")]
    private static partial void LogImportFailed(
        ILogger logger,
        Guid matchId,
        string failureCode,
        Exception exception);
}
