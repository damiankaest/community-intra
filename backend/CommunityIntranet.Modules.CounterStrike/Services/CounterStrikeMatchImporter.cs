using CommunityIntranet.BuildingBlocks.ActivityFeed;
using CommunityIntranet.Modules.CounterStrike.Domain;
using CommunityIntranet.Modules.CounterStrike.Persistence;
using CommunityIntranet.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CommunityIntranet.Modules.CounterStrike.Services;

public sealed partial class CounterStrikeMatchImporter(
    ICounterStrikeDbContext dbContext,
    IIdentityDbContext identityDbContext,
    IEnumerable<IHighlightRule> highlightRules,
    CounterStrikeAwardService awardService,
    IActivityWriter activityWriter,
    TimeProvider timeProvider,
    ILogger<CounterStrikeMatchImporter> logger)
{
    public async Task ImportAsync(
        CounterStrikeMatch match,
        CounterStrikeAnalyzerResult analyzerResult,
        CancellationToken cancellationToken)
    {
        var source = analyzerResult.Match;
        if (source.Players.Count == 0 || source.Rounds.Count == 0)
        {
            throw new CounterStrikeAnalyzerException(
                "empty_match",
                "Die Demo enthält kein vollständiges Match.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var steamIds = source.Players.Values
            .Select(player => player.SteamId.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .ToArray();
        var identities = await identityDbContext.SteamIdentities
            .AsNoTracking()
            .Where(identity => steamIds.Contains(identity.SteamId64))
            .ToArrayAsync(cancellationToken);
        var linkedUsers = SteamIdentityMapper.Map(identities, source.Players.Values);

        CounterStrikeImportProjection.Apply(match, source, linkedUsers);
        match.AnalyzerArtifactPath = analyzerResult.ArtifactPath;

        var clutchWins = source.Clutches
            .Where(clutch => clutch.HasWon)
            .GroupBy(clutch => clutch.ClutcherSteamId)
            .ToDictionary(group => group.Key, group => group.Count());
        foreach (var player in source.Players.Values)
        {
            var steamId = player.SteamId.ToString(System.Globalization.CultureInfo.InvariantCulture);
            dbContext.CounterStrikeMatchPlayers.Add(new CounterStrikeMatchPlayer
            {
                Id = Guid.NewGuid(),
                OrganizationId = match.OrganizationId,
                MatchId = match.Id,
                UserId = linkedUsers.GetValueOrDefault(steamId),
                SteamId64 = steamId,
                DisplayName = Clean(player.Name, 100, "Unknown"),
                TeamName = Clean(player.Team.Name, 120, "Team"),
                Kills = player.KillCount,
                Deaths = player.DeathCount,
                Assists = player.AssistCount,
                Adr = player.AverageDamagePerRound,
                Kast = player.Kast,
                HeadshotPercent = player.HeadshotPercent,
                UtilityDamage = player.UtilityDamage,
                FirstKills = player.FirstKillCount,
                FirstDeaths = player.FirstDeathCount,
                TradeKills = player.TradeKillCount,
                BombPlants = player.BombPlantedCount,
                BombDefuses = player.BombDefusedCount,
                HltvRating = player.HltvRating2,
                ThreeKills = player.ThreeKillCount,
                FourKills = player.FourKillCount,
                Aces = player.FiveKillCount,
                ClutchesWon = clutchWins.GetValueOrDefault(player.SteamId)
            });
        }

        foreach (var round in source.Rounds)
        {
            dbContext.CounterStrikeRounds.Add(new CounterStrikeRound
            {
                Id = Guid.NewGuid(),
                OrganizationId = match.OrganizationId,
                MatchId = match.Id,
                Number = round.Number,
                StartTick = round.StartTick,
                EndTick = round.EndTick,
                WinnerTeam = Clean(round.WinnerName, 120, "Unknown"),
                TeamAScore = round.TeamAScore,
                TeamBScore = round.TeamBScore
            });
        }

        var candidates = highlightRules
            .SelectMany(rule => rule.Evaluate(new CounterStrikeHighlightContext(source)))
            .GroupBy(candidate => new
            {
                candidate.RoundNumber,
                candidate.Type,
                candidate.SteamId64
            })
            .Select(group => group.MaxBy(candidate => candidate.Score)!)
            .ToArray();
        foreach (var candidate in candidates)
        {
            dbContext.CounterStrikeHighlights.Add(new CounterStrikeHighlight
            {
                Id = Guid.NewGuid(),
                OrganizationId = match.OrganizationId,
                SeasonId = match.SeasonId,
                MatchId = match.Id,
                UserId = linkedUsers.GetValueOrDefault(candidate.SteamId64),
                SteamId64 = candidate.SteamId64,
                PlayerName = Clean(candidate.PlayerName, 100, "Unknown"),
                RoundNumber = candidate.RoundNumber,
                Type = Clean(candidate.Type, 60, "Highlight"),
                Title = Clean(candidate.Title, 180, "Highlight"),
                Score = candidate.Score,
                StartTick = candidate.StartTick,
                EndTick = candidate.EndTick,
                CreatedAt = timeProvider.GetUtcNow()
            });
        }

        match.Status = CounterStrikeDemoStatus.Completed;
        match.CompletedAt = timeProvider.GetUtcNow();
        match.FailureCode = null;
        match.FailureMessage = null;
        await dbContext.SaveChangesAsync(cancellationToken);
        await RebuildSeasonStatsAsync(match.OrganizationId, match.SeasonId, cancellationToken);
        await awardService.RecalculateAsync(match.OrganizationId, match.SeasonId, cancellationToken);

        activityWriter.Add(new ActivityDraft(
            match.OrganizationId,
            "counter-strike.match-imported",
            match.UploadedByMemberId,
            "counter-strike-match",
            match.Id,
            new Dictionary<string, string?>
            {
                ["map"] = match.MapName,
                ["score"] = $"{match.TeamAScore}:{match.TeamBScore}",
                ["highlightCount"] = candidates.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)
            }));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        LogImported(
            logger,
            match.Id,
            source.Players.Count,
            source.Rounds.Count,
            candidates.Length,
            linkedUsers.Count);
    }

    private async Task RebuildSeasonStatsAsync(
        Guid organizationId,
        Guid seasonId,
        CancellationToken cancellationToken)
    {
        var matches = await dbContext.CounterStrikeMatches.AsNoTracking()
            .Where(match => match.OrganizationId == organizationId
                && match.SeasonId == seasonId
                && match.Status == CounterStrikeDemoStatus.Completed)
            .ToDictionaryAsync(match => match.Id, cancellationToken);
        var players = await dbContext.CounterStrikeMatchPlayers.AsNoTracking()
            .Where(player => player.OrganizationId == organizationId
                && player.UserId != null
                && matches.Keys.Contains(player.MatchId))
            .ToArrayAsync(cancellationToken);
        var existing = await dbContext.CounterStrikePlayerStats
            .Where(stats => stats.OrganizationId == organizationId && stats.SeasonId == seasonId)
            .ToDictionaryAsync(stats => stats.UserId, cancellationToken);

        var aggregates = CounterStrikeSeasonAggregation.Build(players, matches);
        foreach (var aggregate in aggregates.Values)
        {
            if (!existing.TryGetValue(aggregate.UserId, out var stats))
            {
                stats = new CounterStrikePlayerStats
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    SeasonId = seasonId,
                    UserId = aggregate.UserId
                };
                dbContext.CounterStrikePlayerStats.Add(stats);
            }

            stats.Matches = aggregate.Matches;
            stats.Wins = aggregate.Wins;
            stats.Kills = aggregate.Kills;
            stats.Deaths = aggregate.Deaths;
            stats.Assists = aggregate.Assists;
            stats.Adr = aggregate.Adr;
            stats.Kast = aggregate.Kast;
            stats.HeadshotPercent = aggregate.HeadshotPercent;
            stats.HltvRating = aggregate.HltvRating;
            stats.UtilityDamage = aggregate.UtilityDamage;
            stats.FirstKills = aggregate.FirstKills;
            stats.FirstDeaths = aggregate.FirstDeaths;
            stats.TradeKills = aggregate.TradeKills;
            stats.ThreeKills = aggregate.ThreeKills;
            stats.FourKills = aggregate.FourKills;
            stats.Aces = aggregate.Aces;
            stats.ClutchesWon = aggregate.ClutchesWon;
            stats.UpdatedAt = timeProvider.GetUtcNow();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string Clean(string? value, int maximumLength, string fallback)
    {
        var clean = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return clean[..Math.Min(clean.Length, maximumLength)];
    }

    [LoggerMessage(EventId = 4110, Level = LogLevel.Information,
        Message = "Imported CS2 match {MatchId}: {PlayerCount} players, {RoundCount} rounds, {HighlightCount} highlights, {MappedPlayerCount} linked users")]
    private static partial void LogImported(
        ILogger logger,
        Guid matchId,
        int playerCount,
        int roundCount,
        int highlightCount,
        int mappedPlayerCount);
}

public sealed class CounterStrikeAwardService(
    ICounterStrikeDbContext dbContext,
    IEnumerable<ICounterStrikeAwardRule> rules,
    TimeProvider timeProvider)
{
    public async Task RecalculateAsync(Guid organizationId, Guid seasonId, CancellationToken cancellationToken)
    {
        var stats = await dbContext.CounterStrikePlayerStats.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId && item.SeasonId == seasonId)
            .ToArrayAsync(cancellationToken);
        var candidates = rules.Select(rule => rule.Evaluate(stats)).Where(candidate => candidate is not null).Cast<CounterStrikeAwardCandidate>().ToArray();
        var existingAwards = await dbContext.CounterStrikeAwards
            .Where(award => award.OrganizationId == organizationId && award.SeasonId == seasonId)
            .ToArrayAsync(cancellationToken);
        var existingAwardIds = existingAwards.Select(award => award.Id).ToArray();
        var assignments = await dbContext.CounterStrikeAwardAssignments
            .Where(assignment => existingAwardIds.Contains(assignment.AwardId))
            .ToArrayAsync(cancellationToken);
        dbContext.CounterStrikeAwardAssignments.RemoveRange(assignments);
        dbContext.CounterStrikeAwards.RemoveRange(existingAwards);

        foreach (var candidate in candidates)
        {
            var award = new CounterStrikeAward
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                SeasonId = seasonId,
                Key = candidate.Key,
                Name = candidate.Name,
                Description = candidate.Description,
                Icon = candidate.Icon,
                CreatedAt = timeProvider.GetUtcNow()
            };
            dbContext.CounterStrikeAwards.Add(award);
            dbContext.CounterStrikeAwardAssignments.Add(new CounterStrikeAwardAssignment
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                AwardId = award.Id,
                UserId = candidate.UserId,
                Value = candidate.Value,
                AssignedAt = timeProvider.GetUtcNow()
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
