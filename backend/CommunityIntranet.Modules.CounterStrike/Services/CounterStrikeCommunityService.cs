using System.Globalization;
using CommunityIntranet.Modules.CounterStrike.Domain;
using CommunityIntranet.Modules.CounterStrike.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CommunityIntranet.Modules.CounterStrike.Services;

public sealed class CounterStrikeCommunityService(
    ICounterStrikeDbContext dbContext,
    CounterStrikeAwardService awardService,
    IOptions<CounterStrikeOptions> options,
    TimeProvider timeProvider)
{
    private static readonly (string Name, string Position, string Target)[] MirageDrills =
    [
        ("Window Smoke", "T-Spawn", "Window"),
        ("Jungle Smoke", "T-Spawn", "Jungle"),
        ("Stairs Smoke", "T-Spawn", "Stairs"),
        ("CT Smoke", "A Ramp", "CT"),
        ("A Flash", "A Ramp", "Über Default")
    ];

    public async Task<CounterStrikeSeason> EnsureInitializedAsync(
        Guid organizationId,
        Guid userId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var settings = await dbContext.CounterStrikeCommunitySettings
            .SingleOrDefaultAsync(item => item.OrganizationId == organizationId, cancellationToken);
        CounterStrikeSeason? season = null;
        if (settings?.ActiveSeasonId is not null)
        {
            season = await dbContext.CounterStrikeSeasons.SingleOrDefaultAsync(
                item => item.Id == settings.ActiveSeasonId
                    && item.OrganizationId == organizationId
                    && item.IsActive,
                cancellationToken);
        }

        if (settings is null)
        {
            settings = new CounterStrikeCommunitySettings
            {
                OrganizationId = organizationId,
                DemoMaximumMegabytes = options.Value.MaximumDemoMegabytes,
                CreatedAt = now,
                UpdatedAt = now
            };
            dbContext.CounterStrikeCommunitySettings.Add(settings);
        }

        if (season is null)
        {
            season = new CounterStrikeSeason
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                Name = $"Season {now.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("de-DE"))}",
                StartsAt = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero),
                IsActive = true,
                CreatedAt = now
            };
            dbContext.CounterStrikeSeasons.Add(season);
            settings.ActiveSeasonId = season.Id;
            settings.UpdatedAt = now;
        }

        if (!await dbContext.CounterStrikeTrainingExercises.AnyAsync(
                item => item.OrganizationId == organizationId && item.TrainingPlanId == null,
                cancellationToken))
        {
            var order = 0;
            foreach (var drill in MirageDrills)
            {
                dbContext.CounterStrikeTrainingExercises.Add(new CounterStrikeTrainingExercise
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Kind = CounterStrikeTrainingKind.Utility,
                    Name = drill.Name,
                    Description = "Line-up ansehen, dreimal sauber werfen und im nächsten Match bewusst einsetzen.",
                    DurationMinutes = 3,
                    MapName = "Mirage",
                    Position = drill.Position,
                    Target = drill.Target,
                    SortOrder = order++
                });
            }
        }

        if (!await dbContext.CounterStrikeWeeklyChallenges.AnyAsync(
                item => item.OrganizationId == organizationId && item.EndsAt >= now,
                cancellationToken))
        {
            var daysSinceMonday = ((int)now.DayOfWeek + 6) % 7;
            var start = new DateTimeOffset(
                now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero)
                .AddDays(-daysSinceMonday);
            dbContext.CounterStrikeWeeklyChallenges.Add(new CounterStrikeWeeklyChallenge
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                SeasonId = season.Id,
                Name = "Warm-up statt Ausrede",
                Description = "Absolviere diese Woche mindestens 10 Minuten Browser-Training.",
                MetricKey = "training-minutes",
                TargetValue = 10,
                StartsAt = start,
                EndsAt = start.AddDays(7).AddTicks(-1)
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        if (options.Value.SeedDemoData
            && !await dbContext.CounterStrikeMatches.AnyAsync(
                item => item.OrganizationId == organizationId,
                cancellationToken))
        {
            await SeedDemoDataAsync(organizationId, season.Id, userId, memberId, cancellationToken);
        }

        return season;
    }

    private async Task SeedDemoDataAsync(
        Guid organizationId,
        Guid seasonId,
        Guid userId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var samples = new[]
        {
            ("Ancient", 13, 7, 18, 13, 5, 84d, 1.21d),
            ("Mirage", 11, 13, 16, 17, 7, 72d, 1.02d),
            ("Inferno", 13, 9, 22, 14, 8, 91d, 1.34d),
            ("Nuke", 13, 5, 20, 10, 4, 96d, 1.43d),
            ("Anubis", 9, 13, 14, 18, 6, 68d, 0.91d)
        };

        for (var index = 0; index < samples.Length; index++)
        {
            var sample = samples[index];
            var matchId = Guid.NewGuid();
            dbContext.CounterStrikeMatches.Add(new CounterStrikeMatch
            {
                Id = matchId,
                OrganizationId = organizationId,
                SeasonId = seasonId,
                UploadedByUserId = userId,
                UploadedByMemberId = memberId,
                DemoChecksum = $"development-{organizationId:N}-{index}",
                OriginalFileName = $"demo-{sample.Item1.ToLowerInvariant()}.dem",
                DemoStoragePath = "development-seed",
                Status = CounterStrikeDemoStatus.Completed,
                MapName = sample.Item1,
                PlayedAt = now.AddDays(-(index + 1) * 2),
                TeamAName = "CouchClash",
                TeamBName = "Matchmaking",
                TeamAScore = sample.Item2,
                TeamBScore = sample.Item3,
                CommunityTeam = "A",
                UploadedAt = now.AddDays(-(index + 1) * 2),
                CompletedAt = now.AddDays(-(index + 1) * 2)
            });
            dbContext.CounterStrikeMatchPlayers.Add(new CounterStrikeMatchPlayer
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                MatchId = matchId,
                UserId = userId,
                SteamId64 = "development",
                DisplayName = "Damian",
                TeamName = "CouchClash",
                Kills = sample.Item4,
                Deaths = sample.Item5,
                Assists = sample.Item6,
                Adr = sample.Item7,
                Kast = 72 + index,
                HeadshotPercent = 42 + index,
                UtilityDamage = 14 + index * 3,
                FirstKills = 3 + index % 2,
                FirstDeaths = 2 + index % 3,
                TradeKills = 2 + index,
                HltvRating = sample.Item8,
                ThreeKills = index % 2,
                FourKills = index == 2 ? 1 : 0,
                Aces = index == 3 ? 1 : 0,
                ClutchesWon = index is 0 or 3 ? 1 : 0
            });
            if (index is 2 or 3)
            {
                var type = index == 3 ? "Ace" : "4K";
                dbContext.CounterStrikeHighlights.Add(new CounterStrikeHighlight
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    SeasonId = seasonId,
                    MatchId = matchId,
                    UserId = userId,
                    SteamId64 = "development",
                    PlayerName = "Damian",
                    RoundNumber = index == 3 ? 12 : 8,
                    Type = type,
                    Title = $"Damian erzielt {type}",
                    Score = index == 3 ? 100 : 86,
                    StartTick = 10000 + index * 1000,
                    EndTick = 10400 + index * 1000,
                    CreatedAt = now.AddDays(-(index + 1) * 2)
                });
            }
        }

        dbContext.CounterStrikePlayerStats.Add(new CounterStrikePlayerStats
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            SeasonId = seasonId,
            UserId = userId,
            Matches = 5,
            Wins = 3,
            Kills = samples.Sum(sample => sample.Item4),
            Deaths = samples.Sum(sample => sample.Item5),
            Assists = samples.Sum(sample => sample.Item6),
            Adr = samples.Average(sample => sample.Item7),
            Kast = 74,
            HeadshotPercent = 44,
            HltvRating = samples.Average(sample => sample.Item8),
            UtilityDamage = 92,
            FirstKills = 17,
            FirstDeaths = 15,
            TradeKills = 20,
            ThreeKills = 2,
            FourKills = 1,
            Aces = 1,
            ClutchesWon = 2,
            Role = CounterStrikePlayerRole.Rifler,
            UpdatedAt = now
        });
        for (var index = 0; index < 3; index++)
        {
            var completedAt = now.AddDays(-index);
            var sessionId = Guid.NewGuid();
            dbContext.CounterStrikeTrainingSessions.Add(new CounterStrikeTrainingSession
            {
                Id = sessionId,
                OrganizationId = organizationId,
                UserId = userId,
                StartedAt = completedAt.AddMinutes(-5),
                CompletedAt = completedAt,
                DurationSeconds = 300
            });
            dbContext.CounterStrikeTrainingResults.Add(new CounterStrikeTrainingResult
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                UserId = userId,
                TrainingSessionId = sessionId,
                Kind = index == 0 ? CounterStrikeTrainingKind.Flick : CounterStrikeTrainingKind.Reaction,
                Hits = 42 + index * 3,
                Misses = 8 - index,
                Accuracy = 84 + index * 3,
                ReactionTimeMs = 310 - index * 18,
                FlickTimeMs = 340 - index * 16,
                Repetitions = 50,
                CompletedAt = completedAt
            });
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        await awardService.RecalculateAsync(organizationId, seasonId, cancellationToken);
    }
}
