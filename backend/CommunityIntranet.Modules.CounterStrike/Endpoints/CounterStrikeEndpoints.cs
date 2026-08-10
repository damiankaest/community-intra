using System.Security.Claims;
using CommunityIntranet.BuildingBlocks.Authorization;
using CommunityIntranet.BuildingBlocks.Tenancy;
using CommunityIntranet.Modules.CounterStrike.Domain;
using CommunityIntranet.Modules.CounterStrike.Persistence;
using CommunityIntranet.Modules.CounterStrike.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.CounterStrike.Endpoints;

public static class CounterStrikeEndpoints
{
    private static readonly HashSet<string> AllowedReactions = ["🔥", "😂", "💀", "🤡"];

    public static IEndpointRouteBuilder MapCounterStrikeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/organizations/{organizationId:guid}/counter-strike")
            .WithTags("Counter Strike")
            .RequireAuthorization();

        group.MapGet("/dashboard", GetDashboardAsync);
        group.MapGet("/play", GetPlayAsync);
        group.MapPut("/play", UpdatePlayAsync);
        group.MapGet("/sync", GetSyncStatusAsync);
        group.MapGet("/matches", ListMatchesAsync);
        group.MapPost("/matches", UploadMatchAsync)
            .DisableAntiforgery()
            .RequireRateLimiting("counter-strike-upload");
        group.MapGet("/matches/{matchId:guid}", GetMatchAsync);
        group.MapPost("/matches/{matchId:guid}/retry", RetryMatchAsync)
            .RequireRateLimiting("counter-strike-upload");
        group.MapGet("/seasons/current", GetSeasonAsync);
        group.MapPost("/seasons", CreateSeasonAsync);
        group.MapPost("/seasons/{seasonId:guid}/close", CloseSeasonAsync);
        group.MapGet("/leaderboards", GetLeaderboardsAsync);
        group.MapGet("/recap", GetRecapAsync);
        group.MapGet("/highlights", ListHighlightsAsync);
        group.MapPost("/highlights/{highlightId:guid}/reactions", ToggleReactionAsync);
        group.MapGet("/clips", ListClipsAsync);
        group.MapPost("/clips", UploadClipAsync).DisableAntiforgery().RequireRateLimiting("counter-strike-upload");
        group.MapGet("/clips/{clipId:guid}/content", GetClipContentAsync);
        group.MapDelete("/clips/{clipId:guid}", DeleteClipAsync);
        group.MapGet("/squad", GetSquadListAsync);
        group.MapGet("/squad/overview", GetSquadOverviewAsync);
        group.MapPut("/squad/settings", UpdateSquadSettingsAsync);
        group.MapPut("/squad/{userId:guid}/status", UpdateRosterStatusAsync);
        group.MapGet("/squad/{userId:guid}", GetPlayerProfileAsync);
        group.MapPut("/squad/me/role", UpdateRoleAsync);
        group.MapGet("/training", GetTrainingAsync);
        group.MapGet("/training/history", GetTrainingHistoryAsync);
        group.MapGet("/training/utility", GetUtilityTrainingAsync);
        group.MapPost("/training/results", SaveTrainingResultAsync);
        group.MapGet("/challenges", GetChallengesAsync);
        return endpoints;
    }

    private static async Task<IResult> GetDashboardAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        ICounterStrikeDbContext dbContext,
        IOrganizationAccessService accessService,
        CounterStrikeCommunityService communityService,
        IEnumerable<ITrainingRecommendationRule> trainingRules,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(organizationId, principal, accessService, cancellationToken);
        if (access.Result is not null)
        {
            return access.Result;
        }

        var season = await communityService.EnsureInitializedAsync(
            organizationId, access.UserId, access.Membership!.MemberId, cancellationToken);
        var matches = await dbContext.CounterStrikeMatches.AsNoTracking()
            .Where(match => match.OrganizationId == organizationId
                && match.SeasonId == season.Id
                && match.Status == CounterStrikeDemoStatus.Completed)
            .OrderByDescending(match => match.PlayedAt)
            .ToArrayAsync(cancellationToken);
        var wins = matches.Count(IsCommunityWin);
        var losses = matches.Length - wins;
        var streak = CalculateStreak(matches);
        var personalStats = await dbContext.CounterStrikePlayerStats.AsNoTracking()
            .SingleOrDefaultAsync(stats => stats.OrganizationId == organizationId
                && stats.SeasonId == season.Id
                && stats.UserId == access.UserId, cancellationToken);
        var recommendation = personalStats is null
            ? new CounterStrikeTrainingRecommendation(
                "baseline", "Baseline setzen", "Starte mit einem kurzen Flick-Drill.",
                CounterStrikeTrainingKind.Flick, 50, "aim?mode=flick")
            : trainingRules.Select(rule => rule.Evaluate(personalStats))
                .Where(item => item is not null)
                .OrderByDescending(item => item!.Priority)
                .FirstOrDefault() ?? new CounterStrikeTrainingRecommendation(
                    "maintain", "Form halten", "Deine Werte sind stabil – fünf Minuten Target Switching halten dich warm.",
                    CounterStrikeTrainingKind.TargetSwitching, 30, "aim?mode=switching");
        var leaders = await QueryLeadersAsync(dbContext, organizationId, season.Id, cancellationToken);
        var highlights = await dbContext.CounterStrikeHighlights.AsNoTracking()
            .Where(highlight => highlight.OrganizationId == organizationId && highlight.SeasonId == season.Id)
            .OrderByDescending(highlight => highlight.CreatedAt)
            .Take(4)
            .Select(highlight => new
            {
                highlight.Id,
                highlight.PlayerName,
                highlight.Type,
                highlight.Title,
                highlight.Score,
                highlight.MatchId,
                highlight.RoundNumber
            })
            .ToArrayAsync(cancellationToken);
        var awards = await QueryAwardsAsync(dbContext, organizationId, season.Id, cancellationToken);
        var play = await QueryPlayAsync(dbContext, organizationId, access.UserId, cancellationToken);

        return Results.Ok(new
        {
            season = new { season.Id, season.Name, season.StartsAt, season.EndsAt },
            summary = new
            {
                matches = matches.Length,
                wins,
                losses,
                winRate = matches.Length == 0 ? 0 : wins * 100d / matches.Length,
                streak = streak.Count,
                streakType = streak.IsWin ? "W" : "L"
            },
            lastMatch = matches.FirstOrDefault() is { } latest ? MatchSummary(latest) : null,
            play,
            leaders,
            awards = awards.Take(3),
            highlights,
            recommendation
        });
    }

    private static async Task<IResult> GetPlayAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        ICounterStrikeDbContext dbContext,
        IOrganizationAccessService accessService,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(organizationId, principal, accessService, cancellationToken);
        return access.Result ?? Results.Ok(await QueryPlayAsync(dbContext, organizationId, access.UserId, cancellationToken));
    }

    private static async Task<IResult> UpdatePlayAsync(
        Guid organizationId,
        UpdatePlayRequest request,
        ClaimsPrincipal principal,
        ICounterStrikeDbContext dbContext,
        IOrganizationAccessService accessService,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(organizationId, principal, accessService, cancellationToken);
        if (access.Result is not null)
        {
            return access.Result;
        }
        if (!Enum.IsDefined(request.Availability))
        {
            return Validation("availability", "Dieser Spielstatus ist ungültig.");
        }

        var now = timeProvider.GetUtcNow();
        var session = await dbContext.CounterStrikeGameSessions
            .Where(item => item.OrganizationId == organizationId && item.SessionDate.Date == now.Date && !item.IsClosed)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (session is null)
        {
            session = new CounterStrikeGameSession
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                CreatedByUserId = access.UserId,
                SessionDate = now,
                PlannedStart = request.PlannedStart,
                CreatedAt = now
            };
            dbContext.CounterStrikeGameSessions.Add(session);
        }
        else if (request.PlannedStart is not null)
        {
            session.PlannedStart = request.PlannedStart;
        }

        var participant = await dbContext.CounterStrikeGameSessionParticipants
            .SingleOrDefaultAsync(item => item.GameSessionId == session.Id && item.UserId == access.UserId, cancellationToken);
        if (participant is null)
        {
            participant = new CounterStrikeGameSessionParticipant
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                GameSessionId = session.Id,
                UserId = access.UserId
            };
            dbContext.CounterStrikeGameSessionParticipants.Add(participant);
        }

        participant.Availability = request.Availability;
        participant.AvailableFrom = request.AvailableFrom;
        participant.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(await QueryPlayAsync(dbContext, organizationId, access.UserId, cancellationToken));
    }

    private static async Task<IResult> UploadMatchAsync(
        Guid organizationId,
        HttpContext httpContext,
        ClaimsPrincipal principal,
        ICounterStrikeDbContext dbContext,
        IOrganizationAccessService accessService,
        CounterStrikeCommunityService communityService,
        ICounterStrikeDemoStorage storage,
        ICounterStrikeDemoPipeline pipeline,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var requestSize = httpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (requestSize is { IsReadOnly: false })
        {
            requestSize.MaxRequestBodySize =
                Math.Clamp(storage.MaximumDemoMegabytes, 16, 2048) * 1024L * 1024L + 1024L * 1024L;
        }
        var access = await GetAccessAsync(organizationId, principal, accessService, cancellationToken);
        if (access.Result is not null)
        {
            return access.Result;
        }

        try
        {
            if (!httpContext.Request.HasFormContentType)
            {
                return Validation("file", "Bitte wähle eine CS2-Demo aus.");
            }
            var form = await httpContext.Request.ReadFormAsync(cancellationToken);
            var demo = form.Files.GetFile("demo");
            if (demo is null)
            {
                return Validation("file", "Bitte wähle eine CS2-Demo aus.");
            }
            var season = await communityService.EnsureInitializedAsync(
                organizationId, access.UserId, access.Membership!.MemberId, cancellationToken);
            var stored = await storage.SaveAsync(organizationId, demo, cancellationToken);
            var duplicate = await dbContext.CounterStrikeMatches.AsNoTracking()
                .SingleOrDefaultAsync(match => match.OrganizationId == organizationId
                    && match.DemoChecksum == stored.Checksum, cancellationToken);
            if (duplicate is not null)
            {
                return Results.Conflict(new
                {
                    message = "Diese Demo wurde bereits hochgeladen.",
                    matchId = duplicate.Id,
                    duplicate.Status
                });
            }

            var match = new CounterStrikeMatch
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                SeasonId = season.Id,
                UploadedByUserId = access.UserId,
                UploadedByMemberId = access.Membership.MemberId,
                DemoChecksum = stored.Checksum,
                OriginalFileName = stored.OriginalFileName,
                DemoStoragePath = stored.Path,
                Status = CounterStrikeDemoStatus.Uploaded,
                UploadedAt = timeProvider.GetUtcNow()
            };
            dbContext.CounterStrikeMatches.Add(match);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                var concurrentDuplicate = await dbContext.CounterStrikeMatches.AsNoTracking()
                    .SingleOrDefaultAsync(item => item.OrganizationId == organizationId
                        && item.DemoChecksum == stored.Checksum, cancellationToken);
                if (concurrentDuplicate is null)
                {
                    throw;
                }
                return Results.Conflict(new
                {
                    message = "Diese Demo wurde bereits hochgeladen.",
                    matchId = concurrentDuplicate.Id,
                    concurrentDuplicate.Status
                });
            }
            await pipeline.QueueAsync(match.Id, cancellationToken);
            return Results.Accepted(
                $"/api/organizations/{organizationId}/counter-strike/matches/{match.Id}",
                MatchSummary(match));
        }
        catch (CounterStrikeUploadException exception)
        {
            return Validation(exception.Key, exception.Message);
        }
    }

    private static async Task<IResult> GetSyncStatusAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        ICounterStrikeDbContext dbContext,
        IOrganizationAccessService accessService,
        ICounterStrikeDemoStorage storage,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(organizationId, principal, accessService, cancellationToken);
        if (access.Result is not null)
        {
            return access.Result;
        }

        var steam = await dbContext.SteamIdentities.AsNoTracking()
            .SingleOrDefaultAsync(identity => identity.UserId == access.UserId, cancellationToken);
        var imports = await dbContext.CounterStrikeMatches.AsNoTracking()
            .Where(match => match.OrganizationId == organizationId)
            .GroupBy(_ => 1)
            .Select(group => new CounterStrikeImportCountsResponse(
                group.Count(),
                group.Count(match => match.Status == CounterStrikeDemoStatus.Uploaded),
                group.Count(match => match.Status == CounterStrikeDemoStatus.Processing),
                group.Count(match => match.Status == CounterStrikeDemoStatus.Completed),
                group.Count(match => match.Status == CounterStrikeDemoStatus.Failed),
                group.Max(match => (DateTimeOffset?)match.UploadedAt),
                group.Max(match => match.CompletedAt)))
            .SingleOrDefaultAsync(cancellationToken);

        return Results.Ok(new CounterStrikeSyncStatusResponse(
            "demo-upload",
            false,
            storage.MaximumDemoMegabytes,
            steam is null
                ? new CounterStrikeSteamConnectionResponse(false, null, null, null, null)
                : new CounterStrikeSteamConnectionResponse(
                    true,
                    steam.SteamId64,
                    steam.DisplayName,
                    steam.AvatarUrl,
                    steam.LinkedAt),
            imports ?? new CounterStrikeImportCountsResponse(0, 0, 0, 0, 0, null, null)));
    }

    private static async Task<IResult> RetryMatchAsync(
        Guid organizationId,
        Guid matchId,
        ClaimsPrincipal principal,
        ICounterStrikeDbContext dbContext,
        IOrganizationAccessService accessService,
        ICounterStrikeDemoPipeline pipeline,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(organizationId, principal, accessService, cancellationToken);
        if (access.Result is not null)
        {
            return access.Result;
        }

        var match = await dbContext.CounterStrikeMatches.SingleOrDefaultAsync(
            item => item.Id == matchId && item.OrganizationId == organizationId,
            cancellationToken);
        if (match is null)
        {
            return Results.NotFound();
        }
        if (match.Status != CounterStrikeDemoStatus.Failed)
        {
            return Results.Conflict(new { message = "Nur fehlgeschlagene Imports können erneut gestartet werden." });
        }

        match.Status = CounterStrikeDemoStatus.Uploaded;
        match.FailureCode = null;
        match.FailureMessage = null;
        await dbContext.SaveChangesAsync(cancellationToken);
        await pipeline.QueueAsync(match.Id, cancellationToken);
        return Results.Accepted(value: MatchSummary(match));
    }

    private static async Task<IResult> ListMatchesAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        ICounterStrikeDbContext dbContext,
        IOrganizationAccessService accessService,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(organizationId, principal, accessService, cancellationToken);
        if (access.Result is not null)
        {
            return access.Result;
        }

        var matches = await dbContext.CounterStrikeMatches.AsNoTracking()
            .Where(match => match.OrganizationId == organizationId)
            .OrderByDescending(match => match.PlayedAt ?? match.UploadedAt)
            .Take(100)
            .ToArrayAsync(cancellationToken);
        return Results.Ok(matches.Select(MatchSummary));
    }

    private static async Task<IResult> GetMatchAsync(
        Guid organizationId,
        Guid matchId,
        ClaimsPrincipal principal,
        ICounterStrikeDbContext dbContext,
        IOrganizationAccessService accessService,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(organizationId, principal, accessService, cancellationToken);
        if (access.Result is not null)
        {
            return access.Result;
        }

        var match = await dbContext.CounterStrikeMatches.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == matchId && item.OrganizationId == organizationId, cancellationToken);
        if (match is null)
        {
            return Results.NotFound();
        }

        var players = await dbContext.CounterStrikeMatchPlayers.AsNoTracking()
            .Where(player => player.OrganizationId == organizationId && player.MatchId == matchId)
            .OrderByDescending(player => player.HltvRating)
            .ToArrayAsync(cancellationToken);
        var rounds = await dbContext.CounterStrikeRounds.AsNoTracking()
            .Where(round => round.OrganizationId == organizationId && round.MatchId == matchId)
            .OrderBy(round => round.Number)
            .ToArrayAsync(cancellationToken);
        var highlights = await dbContext.CounterStrikeHighlights.AsNoTracking()
            .Where(highlight => highlight.OrganizationId == organizationId && highlight.MatchId == matchId)
            .OrderByDescending(highlight => highlight.Score)
            .ToArrayAsync(cancellationToken);
        return Results.Ok(new
        {
            match = MatchSummary(match),
            players,
            rounds,
            highlights,
            story = highlights.Select(highlight => highlight.Title).Take(6)
        });
    }

    private static async Task<IResult> GetSeasonAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        ICounterStrikeDbContext dbContext,
        IOrganizationAccessService accessService,
        CounterStrikeCommunityService communityService,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(organizationId, principal, accessService, cancellationToken);
        if (access.Result is not null)
        {
            return access.Result;
        }

        var season = await communityService.EnsureInitializedAsync(
            organizationId, access.UserId, access.Membership!.MemberId, cancellationToken);
        var matches = await dbContext.CounterStrikeMatches.AsNoTracking()
            .Where(match => match.OrganizationId == organizationId && match.SeasonId == season.Id && match.Status == CounterStrikeDemoStatus.Completed)
            .ToArrayAsync(cancellationToken);
        var wins = matches.Count(IsCommunityWin);
        return Results.Ok(new
        {
            season.Id,
            season.Name,
            season.StartsAt,
            season.EndsAt,
            season.IsActive,
            matches = matches.Length,
            wins,
            losses = matches.Length - wins,
            winRate = matches.Length == 0 ? 0 : wins * 100d / matches.Length
        });
    }

    private static async Task<IResult> CreateSeasonAsync(
        Guid organizationId,
        CreateSeasonRequest request,
        ClaimsPrincipal principal,
        ICounterStrikeDbContext dbContext,
        IOrganizationAccessService accessService,
        CounterStrikeCommunityService communityService,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(organizationId, principal, accessService, cancellationToken);
        if (access.Result is not null)
        {
            return access.Result;
        }
        if (!access.Membership!.PermissionRole.CanManageOrganization())
        {
            return Results.Forbid();
        }
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 120)
        {
            return Validation("name", "Bitte gib einen Season-Namen mit maximal 120 Zeichen an.");
        }

        await communityService.EnsureInitializedAsync(
            organizationId, access.UserId, access.Membership.MemberId, cancellationToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.CounterStrikeSeasons
            .Where(season => season.OrganizationId == organizationId && season.IsActive)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(season => season.IsActive, false)
                .SetProperty(season => season.EndsAt, timeProvider.GetUtcNow()), cancellationToken);
        var season = new CounterStrikeSeason
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = request.Name.Trim(),
            StartsAt = request.StartsAt ?? timeProvider.GetUtcNow(),
            IsActive = true,
            CreatedAt = timeProvider.GetUtcNow()
        };
        dbContext.CounterStrikeSeasons.Add(season);
        var settings = await dbContext.CounterStrikeCommunitySettings.SingleAsync(
            item => item.OrganizationId == organizationId, cancellationToken);
        settings.ActiveSeasonId = season.Id;
        settings.UpdatedAt = timeProvider.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Results.Created($"/api/organizations/{organizationId}/counter-strike/seasons/{season.Id}", season);
    }

    private static async Task<IResult> CloseSeasonAsync(
        Guid organizationId,
        Guid seasonId,
        ClaimsPrincipal principal,
        ICounterStrikeDbContext dbContext,
        IOrganizationAccessService accessService,
        CounterStrikeAwardService awardService,
        CounterStrikeCommunityService communityService,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(organizationId, principal, accessService, cancellationToken);
        if (access.Result is not null)
        {
            return access.Result;
        }
        if (!access.Membership!.PermissionRole.CanManageOrganization())
        {
            return Results.Forbid();
        }

        var season = await dbContext.CounterStrikeSeasons.SingleOrDefaultAsync(
            item => item.Id == seasonId && item.OrganizationId == organizationId, cancellationToken);
        if (season is null)
        {
            return Results.NotFound();
        }
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        season.IsActive = false;
        season.EndsAt ??= timeProvider.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken);
        await awardService.RecalculateAsync(organizationId, seasonId, cancellationToken);
        var nextSeason = await communityService.EnsureInitializedAsync(
            organizationId, access.UserId, access.Membership.MemberId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Results.Ok(new { closedSeason = season, nextSeason });
    }

    private static async Task<IResult> GetLeaderboardsAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        ICounterStrikeDbContext dbContext,
        IOrganizationAccessService accessService,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(organizationId, principal, accessService, cancellationToken);
        if (access.Result is not null)
        {
            return access.Result;
        }
        var settings = await dbContext.CounterStrikeCommunitySettings.AsNoTracking()
            .SingleOrDefaultAsync(item => item.OrganizationId == organizationId, cancellationToken);
        return settings?.ActiveSeasonId is null
            ? Results.Ok(new { performance = Array.Empty<object>(), impact = Array.Empty<object>(), clutch = Array.Empty<object>(), multiKills = Array.Empty<object>() })
            : Results.Ok(await QueryLeadersAsync(dbContext, organizationId, settings.ActiveSeasonId.Value, cancellationToken));
    }

    private static async Task<IResult> GetRecapAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        ICounterStrikeDbContext dbContext,
        IOrganizationAccessService accessService,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(organizationId, principal, accessService, cancellationToken);
        if (access.Result is not null)
        {
            return access.Result;
        }
        var settings = await dbContext.CounterStrikeCommunitySettings.AsNoTracking()
            .SingleOrDefaultAsync(item => item.OrganizationId == organizationId, cancellationToken);
        if (settings?.ActiveSeasonId is null)
        {
            return Results.NotFound();
        }

        var season = await dbContext.CounterStrikeSeasons.AsNoTracking().SingleAsync(item => item.Id == settings.ActiveSeasonId, cancellationToken);
        var matches = await dbContext.CounterStrikeMatches.AsNoTracking()
            .Where(match => match.OrganizationId == organizationId && match.SeasonId == season.Id && match.Status == CounterStrikeDemoStatus.Completed)
            .OrderBy(match => match.PlayedAt)
            .ToArrayAsync(cancellationToken);
        var mapStats = matches.GroupBy(match => match.MapName ?? "Unknown")
            .Select(group => new { map = group.Key, matches = group.Count(), wins = group.Count(IsCommunityWin), winRate = group.Count(IsCommunityWin) * 100d / group.Count() })
            .OrderByDescending(item => item.winRate)
            .ToArray();
        var highlights = await dbContext.CounterStrikeHighlights.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId && item.SeasonId == season.Id)
            .OrderByDescending(item => item.Score)
            .Take(8)
            .ToArrayAsync(cancellationToken);
        var awards = await QueryAwardsAsync(dbContext, organizationId, season.Id, cancellationToken);
        var wins = matches.Count(IsCommunityWin);
        return Results.Ok(new
        {
            season = new { season.Id, season.Name, season.StartsAt, season.EndsAt },
            summary = new { matches = matches.Length, wins, losses = matches.Length - wins, winRate = matches.Length == 0 ? 0 : wins * 100d / matches.Length },
            bestMap = mapStats.FirstOrDefault(),
            worstMap = mapStats.LastOrDefault(),
            maps = mapStats,
            highlights,
            awards,
            winStreak = LongestWinStreak(matches),
            aces = highlights.Count(item => item.Type == "Ace"),
            clutches = highlights.Count(item => item.Type.StartsWith("1v", StringComparison.Ordinal))
        });
    }

    private static async Task<IResult> ListHighlightsAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        ICounterStrikeDbContext dbContext,
        IOrganizationAccessService accessService,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(organizationId, principal, accessService, cancellationToken);
        if (access.Result is not null)
        {
            return access.Result;
        }
        var highlights = await dbContext.CounterStrikeHighlights.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId)
            .OrderByDescending(item => item.CreatedAt)
            .Take(100)
            .ToArrayAsync(cancellationToken);
        var ids = highlights.Select(item => item.Id).ToArray();
        var reactions = await dbContext.CounterStrikeHighlightReactions.AsNoTracking()
            .Where(item => ids.Contains(item.HighlightId))
            .GroupBy(item => new { item.HighlightId, item.Reaction })
            .Select(group => new { group.Key.HighlightId, group.Key.Reaction, Count = group.Count() })
            .ToArrayAsync(cancellationToken);
        return Results.Ok(highlights.Select(highlight => new
        {
            highlight.Id,
            highlight.MatchId,
            highlight.PlayerName,
            highlight.RoundNumber,
            highlight.Type,
            highlight.Title,
            highlight.Score,
            highlight.StartTick,
            highlight.EndTick,
            highlight.VideoStoragePath,
            highlight.CreatedAt,
            reactions = reactions.Where(item => item.HighlightId == highlight.Id)
        }));
    }

    private static async Task<IResult> ToggleReactionAsync(
        Guid organizationId,
        Guid highlightId,
        ToggleReactionRequest request,
        ClaimsPrincipal principal,
        ICounterStrikeDbContext dbContext,
        IOrganizationAccessService accessService,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(organizationId, principal, accessService, cancellationToken);
        if (access.Result is not null)
        {
            return access.Result;
        }
        if (!AllowedReactions.Contains(request.Reaction))
        {
            return Validation("reaction", "Diese Reaction wird nicht unterstützt.");
        }
        if (!await dbContext.CounterStrikeHighlights.AnyAsync(
                item => item.Id == highlightId && item.OrganizationId == organizationId, cancellationToken))
        {
            return Results.NotFound();
        }

        var existing = await dbContext.CounterStrikeHighlightReactions.SingleOrDefaultAsync(
            item => item.HighlightId == highlightId && item.UserId == access.UserId && item.Reaction == request.Reaction,
            cancellationToken);
        if (existing is null)
        {
            dbContext.CounterStrikeHighlightReactions.Add(new CounterStrikeHighlightReaction
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                HighlightId = highlightId,
                UserId = access.UserId,
                Reaction = request.Reaction,
                CreatedAt = timeProvider.GetUtcNow()
            });
        }
        else
        {
            dbContext.CounterStrikeHighlightReactions.Remove(existing);
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GetSquadListAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        ICounterStrikeDbContext dbContext,
        IOrganizationAccessService accessService,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(organizationId, principal, accessService, cancellationToken);
        if (access.Result is not null)
        {
            return access.Result;
        }
        var settings = await dbContext.CounterStrikeCommunitySettings.AsNoTracking()
            .SingleOrDefaultAsync(item => item.OrganizationId == organizationId, cancellationToken);
        var seasonId = settings?.ActiveSeasonId;
        var members = await (
            from member in dbContext.OrganizationMembers.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on member.UserId equals user.Id
            join steam in dbContext.SteamIdentities.AsNoTracking() on user.Id equals steam.UserId into steamLinks
            from steam in steamLinks.DefaultIfEmpty()
            join stats in dbContext.CounterStrikePlayerStats.AsNoTracking().Where(item => item.SeasonId == seasonId)
                on user.Id equals stats.UserId into playerStats
            from stats in playerStats.DefaultIfEmpty()
            where member.OrganizationId == organizationId && member.IsActive
            orderby stats == null ? 0 : stats.HltvRating descending
            select new
            {
                user.Id,
                user.DisplayName,
                user.AvatarUrl,
                steamId64 = steam == null ? null : steam.SteamId64,
                steamName = steam == null ? null : steam.DisplayName,
                steamAvatarUrl = steam == null ? null : steam.AvatarUrl,
                role = stats == null ? CounterStrikePlayerRole.Unset : stats.Role,
                stats = stats == null ? null : new
                {
                    stats.Matches,
                    stats.Wins,
                    losses = stats.Matches - stats.Wins,
                    kd = stats.Deaths == 0 ? stats.Kills : (double)stats.Kills / stats.Deaths,
                    stats.Adr,
                    stats.Kast,
                    stats.HeadshotPercent,
                    stats.HltvRating,
                    stats.Aces,
                    stats.ClutchesWon
                }
            }).ToArrayAsync(cancellationToken);
        return Results.Ok(members);
    }

    private static async Task<IResult> GetSquadOverviewAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        ICounterStrikeDbContext dbContext,
        IOrganizationAccessService accessService,
        CounterStrikeCommunityService communityService,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(organizationId, principal, accessService, cancellationToken);
        if (access.Result is not null)
        {
            return access.Result;
        }
        var season = await communityService.EnsureInitializedAsync(
            organizationId,
            access.UserId,
            access.Membership!.MemberId,
            cancellationToken);
        var memberRows = await (
            from member in dbContext.OrganizationMembers.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on member.UserId equals user.Id
            join steam in dbContext.SteamIdentities.AsNoTracking() on user.Id equals steam.UserId into steamLinks
            from steam in steamLinks.DefaultIfEmpty()
            where member.OrganizationId == organizationId && member.IsActive
            select new
            {
                user.Id,
                user.DisplayName,
                user.AvatarUrl,
                steamId64 = steam == null ? null : steam.SteamId64,
                steamName = steam == null ? null : steam.DisplayName,
                steamAvatarUrl = steam == null ? null : steam.AvatarUrl
            }).ToArrayAsync(cancellationToken);
        var playerStats = await dbContext.CounterStrikePlayerStats.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId && item.SeasonId == season.Id)
            .ToDictionaryAsync(item => item.UserId, cancellationToken);
        var rosterStatuses = await dbContext.CounterStrikeRosterMembers.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId)
            .ToDictionaryAsync(item => item.UserId, item => item.Status, cancellationToken);
        var players = memberRows
            .Select(member =>
            {
                playerStats.TryGetValue(member.Id, out var stats);
                return new
                {
                    member.Id,
                    member.DisplayName,
                    member.AvatarUrl,
                    member.steamId64,
                    member.steamName,
                    member.steamAvatarUrl,
                    rosterStatus = rosterStatuses.GetValueOrDefault(member.Id, CounterStrikeRosterStatus.Active),
                    role = stats?.Role ?? CounterStrikePlayerRole.Unset,
                    stats = stats is null ? null : new
                    {
                        stats.Matches,
                        stats.Wins,
                        losses = stats.Matches - stats.Wins,
                        kd = stats.Deaths == 0 ? stats.Kills : (double)stats.Kills / stats.Deaths,
                        stats.Adr,
                        stats.Kast,
                        stats.HeadshotPercent,
                        stats.HltvRating,
                        stats.Aces,
                        stats.ClutchesWon
                    }
                };
            })
            .OrderByDescending(player => player.stats?.HltvRating ?? 0)
            .ToArray();
        var matches = await dbContext.CounterStrikeMatches.AsNoTracking()
            .Where(match => match.OrganizationId == organizationId
                && match.SeasonId == season.Id
                && match.Status == CounterStrikeDemoStatus.Completed)
            .ToArrayAsync(cancellationToken);
        var matchIds = matches.Select(match => match.Id).ToArray();
        var matchPlayers = await dbContext.CounterStrikeMatchPlayers.AsNoTracking()
            .Where(player => player.OrganizationId == organizationId
                && matchIds.Contains(player.MatchId))
            .ToArrayAsync(cancellationToken);
        var memberUserIds = memberRows.Select(member => member.Id).ToHashSet();
        var settings = await dbContext.CounterStrikeCommunitySettings.AsNoTracking()
            .SingleAsync(item => item.OrganizationId == organizationId, cancellationToken);
        var activePlayers = players.Where(player => player.rosterStatus == CounterStrikeRosterStatus.Active).ToArray();
        var steamConnected = activePlayers.Count(player => player.steamId64 is not null);
        var rolesAssigned = activePlayers.Count(player => player.role != CounterStrikePlayerRole.Unset);
        var completedDemos = matches.Length;
        var steps = new[]
        {
            !string.IsNullOrWhiteSpace(settings.SquadName) && !string.IsNullOrWhiteSpace(settings.SquadTag),
            activePlayers.Length >= 5,
            activePlayers.Length >= 5 && steamConnected >= 5,
            activePlayers.Length >= 5 && rolesAssigned >= 5,
            completedDemos > 0
        };

        return Results.Ok(new
        {
            settings = new { settings.SquadName, settings.SquadTag },
            readiness = new
            {
                totalMembers = players.Length,
                activePlayers = activePlayers.Length,
                substitutes = players.Count(player => player.rosterStatus == CounterStrikeRosterStatus.Substitute),
                inactivePlayers = players.Count(player => player.rosterStatus == CounterStrikeRosterStatus.Inactive),
                steamConnected,
                rolesAssigned,
                completedDemos,
                completedSteps = steps.Count(done => done),
                totalSteps = steps.Length
            },
            players,
            summary = new
            {
                playerRecord = CounterStrikeSquadStatistics.BuildPlayerRecord(
                    playerStats.Values.Where(stats => memberUserIds.Contains(stats.UserId))),
                fullSquadRecord = CounterStrikeSquadStatistics.BuildFullSquadRecord(
                    matches,
                    matchPlayers,
                    memberUserIds)
            }
        });
    }

    private static async Task<IResult> UpdateSquadSettingsAsync(
        Guid organizationId, UpdateSquadSettingsRequest request, ClaimsPrincipal principal,
        ICounterStrikeDbContext dbContext, IOrganizationAccessService accessService,
        CounterStrikeCommunityService communityService, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(organizationId, principal, accessService, cancellationToken);
        if (access.Result is not null) return access.Result;
        if (!access.Membership!.PermissionRole.CanManageOrganization()) return Results.Forbid();
        var name = request.SquadName?.Trim();
        var tag = request.SquadTag?.Trim().ToUpperInvariant();
        if (name is null || name.Length is < 2 or > 120) return Validation("squadName", "Der Squadname braucht 2 bis 120 Zeichen.");
        if (tag is null || tag.Length is < 2 or > 12 || tag.Any(character => !char.IsLetterOrDigit(character) && character != '-'))
            return Validation("squadTag", "Das Kürzel braucht 2 bis 12 Buchstaben, Zahlen oder Bindestriche.");
        await communityService.EnsureInitializedAsync(organizationId, access.UserId, access.Membership.MemberId, cancellationToken);
        var settings = await dbContext.CounterStrikeCommunitySettings.SingleAsync(item => item.OrganizationId == organizationId, cancellationToken);
        settings.SquadName = name;
        settings.SquadTag = tag;
        settings.UpdatedAt = timeProvider.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { settings.SquadName, settings.SquadTag });
    }

    private static async Task<IResult> UpdateRosterStatusAsync(
        Guid organizationId, Guid userId, UpdateRosterStatusRequest request, ClaimsPrincipal principal,
        ICounterStrikeDbContext dbContext, IOrganizationAccessService accessService,
        TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(organizationId, principal, accessService, cancellationToken);
        if (access.Result is not null) return access.Result;
        if (!access.Membership!.PermissionRole.CanManageOrganization()) return Results.Forbid();
        if (!Enum.IsDefined(request.Status)) return Validation("status", "Dieser Kaderstatus ist ungültig.");
        var isMember = await dbContext.OrganizationMembers.AsNoTracking()
            .AnyAsync(item => item.OrganizationId == organizationId && item.UserId == userId && item.IsActive, cancellationToken);
        if (!isMember) return Results.NotFound();
        var roster = await dbContext.CounterStrikeRosterMembers.SingleOrDefaultAsync(
            item => item.OrganizationId == organizationId && item.UserId == userId, cancellationToken);
        if (roster is null)
        {
            roster = new CounterStrikeRosterMember { OrganizationId = organizationId, UserId = userId };
            dbContext.CounterStrikeRosterMembers.Add(roster);
        }
        roster.Status = request.Status;
        roster.UpdatedAt = timeProvider.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { roster.Status });
    }

    private static async Task<IResult> UpdateRoleAsync(
        Guid organizationId,
        UpdateRoleRequest request,
        ClaimsPrincipal principal,
        ICounterStrikeDbContext dbContext,
        IOrganizationAccessService accessService,
        CounterStrikeCommunityService communityService,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(organizationId, principal, accessService, cancellationToken);
        if (access.Result is not null)
        {
            return access.Result;
        }
        if (!Enum.IsDefined(request.Role))
        {
            return Validation("role", "Diese Squad-Rolle ist ungültig.");
        }
        var season = await communityService.EnsureInitializedAsync(
            organizationId, access.UserId, access.Membership!.MemberId, cancellationToken);
        var stats = await dbContext.CounterStrikePlayerStats.SingleOrDefaultAsync(
            item => item.OrganizationId == organizationId && item.SeasonId == season.Id && item.UserId == access.UserId,
            cancellationToken);
        if (stats is null)
        {
            stats = new CounterStrikePlayerStats
            {
                Id = Guid.NewGuid(), OrganizationId = organizationId, SeasonId = season.Id,
                UserId = access.UserId, Role = request.Role, UpdatedAt = timeProvider.GetUtcNow()
            };
            dbContext.CounterStrikePlayerStats.Add(stats);
        }
        else
        {
            stats.Role = request.Role;
            stats.UpdatedAt = timeProvider.GetUtcNow();
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { stats.Role });
    }

    private static async Task<IResult> GetPlayerProfileAsync(
        Guid organizationId,
        Guid userId,
        ClaimsPrincipal principal,
        ICounterStrikeDbContext dbContext,
        IOrganizationAccessService accessService,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(organizationId, principal, accessService, cancellationToken);
        if (access.Result is not null)
        {
            return access.Result;
        }

        var player = await (
            from member in dbContext.OrganizationMembers.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on member.UserId equals user.Id
            where member.OrganizationId == organizationId && member.IsActive && user.Id == userId
            select new { user.Id, user.DisplayName, user.AvatarUrl })
            .SingleOrDefaultAsync(cancellationToken);
        if (player is null)
        {
            return Results.NotFound();
        }

        var settings = await dbContext.CounterStrikeCommunitySettings.AsNoTracking()
            .SingleOrDefaultAsync(item => item.OrganizationId == organizationId, cancellationToken);
        var seasonId = settings?.ActiveSeasonId;
        var steam = await dbContext.SteamIdentities.AsNoTracking()
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        var stats = seasonId is null
            ? null
            : await dbContext.CounterStrikePlayerStats.AsNoTracking()
                .SingleOrDefaultAsync(item => item.OrganizationId == organizationId
                    && item.SeasonId == seasonId && item.UserId == userId, cancellationToken);
        var matchRows = await (
            from matchPlayer in dbContext.CounterStrikeMatchPlayers.AsNoTracking()
            join match in dbContext.CounterStrikeMatches.AsNoTracking() on matchPlayer.MatchId equals match.Id
            where matchPlayer.OrganizationId == organizationId
                && matchPlayer.UserId == userId
                && match.Status == CounterStrikeDemoStatus.Completed
                && (seasonId == null || match.SeasonId == seasonId)
            orderby match.PlayedAt descending
            select new { match, matchPlayer })
            .Take(20)
            .ToArrayAsync(cancellationToken);
        var favoriteMap = matchRows
            .Where(item => !string.IsNullOrWhiteSpace(item.match.MapName))
            .GroupBy(item => item.match.MapName!)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Select(group => group.Key)
            .FirstOrDefault();
        var awards = seasonId is null
            ? Array.Empty<CounterStrikeAwardResponse>()
            : await QueryAwardsForUserAsync(
                dbContext, organizationId, seasonId.Value, userId, cancellationToken);
        var highlights = await dbContext.CounterStrikeHighlights.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId && item.UserId == userId)
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => item.CreatedAt)
            .Take(8)
            .ToArrayAsync(cancellationToken);
        var training = await dbContext.CounterStrikeTrainingResults.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId && item.UserId == userId)
            .OrderByDescending(item => item.CompletedAt)
            .Take(20)
            .ToArrayAsync(cancellationToken);

        return Results.Ok(new
        {
            player,
            steam = steam is null ? null : new
            {
                steam.SteamId64,
                steam.DisplayName,
                steam.AvatarUrl,
                steam.LinkedAt
            },
            role = stats?.Role ?? CounterStrikePlayerRole.Unset,
            favoriteMap,
            stats,
            trends = new
            {
                last5 = PlayerTrend(matchRows.Take(5).Select(item => item.matchPlayer)),
                last20 = PlayerTrend(matchRows.Select(item => item.matchPlayer))
            },
            awards,
            highlights,
            training = new
            {
                sessions = training.Length,
                averageAccuracy = training.Length == 0 ? 0 : training.Average(item => item.Accuracy),
                recent = training.Take(6)
            }
        });
    }

    private static async Task<IResult> GetTrainingAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        ICounterStrikeDbContext dbContext,
        IOrganizationAccessService accessService,
        CounterStrikeCommunityService communityService,
        IEnumerable<ITrainingRecommendationRule> rules,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(organizationId, principal, accessService, cancellationToken);
        if (access.Result is not null)
        {
            return access.Result;
        }
        var season = await communityService.EnsureInitializedAsync(organizationId, access.UserId, access.Membership!.MemberId, cancellationToken);
        var stats = await dbContext.CounterStrikePlayerStats.AsNoTracking().SingleOrDefaultAsync(
            item => item.OrganizationId == organizationId && item.SeasonId == season.Id && item.UserId == access.UserId,
            cancellationToken);
        var recommendations = stats is null
            ? new[] { new CounterStrikeTrainingRecommendation("baseline", "Baseline setzen", "Starte mit fünf Minuten Flicks.", CounterStrikeTrainingKind.Flick, 50, "aim?mode=flick") }
            : rules.Select(rule => rule.Evaluate(stats)).Where(item => item is not null).Cast<CounterStrikeTrainingRecommendation>().OrderByDescending(item => item.Priority).ToArray();
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        var plan = await dbContext.CounterStrikeTrainingPlans.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId && item.UserId == access.UserId && item.PlanDate == today)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        CounterStrikeTrainingExercise[] exercises = [];
        if (plan is null)
        {
            var recommendation = recommendations.FirstOrDefault();
            plan = new CounterStrikeTrainingPlan
            {
                Id = Guid.NewGuid(), OrganizationId = organizationId, UserId = access.UserId,
                PlanDate = today, PlannedMinutes = 15,
                RecommendationReason = recommendation?.Reason ?? "Kurzer Mix für eine stabile Basis.",
                CreatedAt = timeProvider.GetUtcNow()
            };
            exercises =
            [
                NewPlanExercise(organizationId, plan.Id, CounterStrikeTrainingKind.Flick, "Flick Warm-up", 5, 0),
                NewPlanExercise(organizationId, plan.Id, CounterStrikeTrainingKind.TargetSwitching, "Target Switching", 5, 1),
                NewPlanExercise(organizationId, plan.Id, recommendation?.Kind == CounterStrikeTrainingKind.Utility ? CounterStrikeTrainingKind.Utility : CounterStrikeTrainingKind.Reaction, recommendation?.Kind == CounterStrikeTrainingKind.Utility ? "Mirage Utility" : "Reaction Drill", 5, 2)
            ];
            dbContext.CounterStrikeTrainingPlans.Add(plan);
            dbContext.CounterStrikeTrainingExercises.AddRange(exercises);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            exercises = await dbContext.CounterStrikeTrainingExercises.AsNoTracking()
                .Where(item => item.TrainingPlanId == plan.Id)
                .OrderBy(item => item.SortOrder)
                .ToArrayAsync(cancellationToken);
        }
        var history = await dbContext.CounterStrikeTrainingResults.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId && item.UserId == access.UserId)
            .OrderByDescending(item => item.CompletedAt).Take(12).ToArrayAsync(cancellationToken);
        return Results.Ok(new { recommendations, plan, exercises, history });
    }

    private static async Task<IResult> SaveTrainingResultAsync(
        Guid organizationId,
        SaveTrainingResultRequest request,
        ClaimsPrincipal principal,
        ICounterStrikeDbContext dbContext,
        IOrganizationAccessService accessService,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(organizationId, principal, accessService, cancellationToken);
        if (access.Result is not null)
        {
            return access.Result;
        }
        if (!Enum.IsDefined(request.Kind)
            || request.DurationSeconds is < 1 or > 7200
            || request.Hits < 0
            || request.Misses < 0)
        {
            return Validation("result", "Das Trainingsergebnis ist ungültig.");
        }
        var now = timeProvider.GetUtcNow();
        var session = new CounterStrikeTrainingSession
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, UserId = access.UserId,
            TrainingPlanId = request.TrainingPlanId, StartedAt = now.AddSeconds(-request.DurationSeconds),
            CompletedAt = now, DurationSeconds = request.DurationSeconds
        };
        var result = new CounterStrikeTrainingResult
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, UserId = access.UserId,
            TrainingSessionId = session.Id, TrainingExerciseId = request.TrainingExerciseId,
            Kind = request.Kind, Hits = request.Hits, Misses = request.Misses,
            Accuracy = request.Hits + request.Misses == 0 ? 0 : request.Hits * 100d / (request.Hits + request.Misses),
            ReactionTimeMs = Math.Max(0, request.ReactionTimeMs), FlickTimeMs = Math.Max(0, request.FlickTimeMs),
            TrackingPercent = Math.Clamp(request.TrackingPercent, 0, 100), Repetitions = Math.Max(0, request.Repetitions),
            CompletedAt = now
        };
        dbContext.CounterStrikeTrainingSessions.Add(session);
        dbContext.CounterStrikeTrainingResults.Add(result);
        var challenge = await dbContext.CounterStrikeWeeklyChallenges.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId && item.MetricKey == "training-minutes" && item.StartsAt <= now && item.EndsAt >= now)
            .FirstOrDefaultAsync(cancellationToken);
        if (challenge is not null)
        {
            var progress = await dbContext.CounterStrikeWeeklyChallengeProgress.SingleOrDefaultAsync(
                item => item.ChallengeId == challenge.Id && item.UserId == access.UserId, cancellationToken);
            if (progress is null)
            {
                progress = new CounterStrikeWeeklyChallengeProgress
                {
                    Id = Guid.NewGuid(), OrganizationId = organizationId, ChallengeId = challenge.Id, UserId = access.UserId
                };
                dbContext.CounterStrikeWeeklyChallengeProgress.Add(progress);
            }
            progress.Value += request.DurationSeconds / 60d;
            progress.UpdatedAt = now;
            if (progress.Value >= challenge.TargetValue)
            {
                progress.CompletedAt ??= now;
            }
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/organizations/{organizationId}/counter-strike/training/results/{result.Id}", result);
    }

    private static async Task<IResult> GetTrainingHistoryAsync(
        Guid organizationId, ClaimsPrincipal principal, ICounterStrikeDbContext dbContext,
        IOrganizationAccessService accessService, CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(organizationId, principal, accessService, cancellationToken);
        return access.Result ?? Results.Ok(await dbContext.CounterStrikeTrainingResults.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId && item.UserId == access.UserId)
            .OrderByDescending(item => item.CompletedAt).Take(100).ToArrayAsync(cancellationToken));
    }

    private static async Task<IResult> GetUtilityTrainingAsync(
        Guid organizationId, ClaimsPrincipal principal, ICounterStrikeDbContext dbContext,
        IOrganizationAccessService accessService, CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(organizationId, principal, accessService, cancellationToken);
        return access.Result ?? Results.Ok(await dbContext.CounterStrikeTrainingExercises.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId && item.Kind == CounterStrikeTrainingKind.Utility && item.TrainingPlanId == null)
            .OrderBy(item => item.MapName).ThenBy(item => item.SortOrder).ToArrayAsync(cancellationToken));
    }

    private static async Task<IResult> GetChallengesAsync(
        Guid organizationId, ClaimsPrincipal principal, ICounterStrikeDbContext dbContext,
        IOrganizationAccessService accessService, CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(organizationId, principal, accessService, cancellationToken);
        if (access.Result is not null)
        {
            return access.Result;
        }
        var now = DateTimeOffset.UtcNow;
        var challenges = await dbContext.CounterStrikeWeeklyChallenges.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId && item.EndsAt >= now)
            .OrderBy(item => item.EndsAt).ToArrayAsync(cancellationToken);
        var ids = challenges.Select(item => item.Id).ToArray();
        var progress = await dbContext.CounterStrikeWeeklyChallengeProgress.AsNoTracking()
            .Where(item => ids.Contains(item.ChallengeId)).ToArrayAsync(cancellationToken);
        return Results.Ok(challenges.Select(challenge => new
        {
            challenge.Id, challenge.Name, challenge.Description, challenge.TargetValue,
            challenge.StartsAt, challenge.EndsAt,
            squad = progress.Where(item => item.ChallengeId == challenge.Id).Select(item => new { item.UserId, item.Value, item.CompletedAt }),
            mine = progress.FirstOrDefault(item => item.ChallengeId == challenge.Id && item.UserId == access.UserId)
        }));
    }

    private static async Task<object> QueryPlayAsync(ICounterStrikeDbContext dbContext, Guid organizationId, Guid userId, CancellationToken cancellationToken)
    {
        var today = DateTimeOffset.UtcNow.Date;
        var session = await dbContext.CounterStrikeGameSessions.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId && item.SessionDate.Date == today && !item.IsClosed)
            .OrderByDescending(item => item.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        if (session is null)
        {
            return new
            {
                sessionId = (Guid?)null,
                plannedStart = (TimeOnly?)null,
                yes = 0,
                maybe = 0,
                missing = 5,
                substitutes = 0,
                fullStack = false,
                mine = (CounterStrikeAvailability?)null,
                participants = Array.Empty<object>()
            };
        }
        var participants = await (
            from participant in dbContext.CounterStrikeGameSessionParticipants.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on participant.UserId equals user.Id
            where participant.GameSessionId == session.Id
            orderby participant.Availability
            select new { participant.UserId, user.DisplayName, user.AvatarUrl, participant.Availability, participant.AvailableFrom })
            .ToArrayAsync(cancellationToken);
        var yes = participants.Count(item => item.Availability == CounterStrikeAvailability.Yes);
        var readiness = CounterStrikeSquadStatistics.BuildReadiness(yes);
        return new
        {
            sessionId = (Guid?)session.Id, session.PlannedStart, yes,
            maybe = participants.Count(item => item.Availability == CounterStrikeAvailability.Maybe),
            readiness.Missing, readiness.Substitutes, readiness.FullStack,
            mine = participants.FirstOrDefault(item => item.UserId == userId)?.Availability,
            participants
        };
    }

    private static async Task<object> QueryLeadersAsync(ICounterStrikeDbContext dbContext, Guid organizationId, Guid seasonId, CancellationToken cancellationToken)
    {
        var stats = await (
            from item in dbContext.CounterStrikePlayerStats.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on item.UserId equals user.Id
            where item.OrganizationId == organizationId && item.SeasonId == seasonId && item.Matches > 0
            select new
            {
                item.UserId, user.DisplayName, user.AvatarUrl, item.Matches,
                kd = item.Deaths == 0 ? item.Kills : (double)item.Kills / item.Deaths,
                item.Adr, item.Kast, item.HeadshotPercent, item.HltvRating,
                item.FirstKills, entryDifference = item.FirstKills - item.FirstDeaths,
                item.TradeKills, item.UtilityDamage, item.ClutchesWon,
                item.ThreeKills, item.FourKills, item.Aces
            }).ToArrayAsync(cancellationToken);
        return new
        {
            performance = stats.OrderByDescending(item => item.HltvRating).Take(10),
            impact = stats.OrderByDescending(item => item.entryDifference).ThenByDescending(item => item.TradeKills).Take(10),
            clutch = stats.OrderByDescending(item => item.ClutchesWon).Take(10),
            multiKills = stats.OrderByDescending(item => item.Aces).ThenByDescending(item => item.FourKills).Take(10)
        };
    }

    private static async Task<CounterStrikeAwardResponse[]> QueryAwardsAsync(ICounterStrikeDbContext dbContext, Guid organizationId, Guid seasonId, CancellationToken cancellationToken) =>
        await (
            from award in dbContext.CounterStrikeAwards.AsNoTracking()
            join assignment in dbContext.CounterStrikeAwardAssignments.AsNoTracking() on award.Id equals assignment.AwardId
            join user in dbContext.Users.AsNoTracking() on assignment.UserId equals user.Id
            where award.OrganizationId == organizationId && award.SeasonId == seasonId
            orderby award.Name
            select new CounterStrikeAwardResponse(
                award.Id, award.Name, award.Description, award.Icon, user.DisplayName, assignment.Value))
            .ToArrayAsync(cancellationToken);

    private static async Task<CounterStrikeAwardResponse[]> QueryAwardsForUserAsync(
        ICounterStrikeDbContext dbContext,
        Guid organizationId,
        Guid seasonId,
        Guid userId,
        CancellationToken cancellationToken) =>
        await (
            from award in dbContext.CounterStrikeAwards.AsNoTracking()
            join assignment in dbContext.CounterStrikeAwardAssignments.AsNoTracking() on award.Id equals assignment.AwardId
            join user in dbContext.Users.AsNoTracking() on assignment.UserId equals user.Id
            where award.OrganizationId == organizationId
                && award.SeasonId == seasonId
                && assignment.UserId == userId
            orderby award.Name
            select new CounterStrikeAwardResponse(
                award.Id, award.Name, award.Description, award.Icon, user.DisplayName, assignment.Value))
            .ToArrayAsync(cancellationToken);

    private static object PlayerTrend(IEnumerable<CounterStrikeMatchPlayer> source)
    {
        var rows = source.ToArray();
        return new
        {
            matches = rows.Length,
            kd = rows.Sum(item => item.Deaths) == 0
                ? rows.Sum(item => item.Kills)
                : (double)rows.Sum(item => item.Kills) / rows.Sum(item => item.Deaths),
            adr = rows.Length == 0 ? 0 : rows.Average(item => item.Adr),
            kast = rows.Length == 0 ? 0 : rows.Average(item => item.Kast),
            rating = rows.Length == 0 ? 0 : rows.Average(item => item.HltvRating)
        };
    }

    private static object MatchSummary(CounterStrikeMatch match) => new
    {
        match.Id, match.SeasonId, match.Status, match.MapName, match.PlayedAt,
        match.TeamAName, match.TeamBName, match.TeamAScore, match.TeamBScore,
        match.CommunityTeam, win = match.Status == CounterStrikeDemoStatus.Completed ? IsCommunityWin(match) : (bool?)null,
        match.OriginalFileName, match.UploadedAt, match.CompletedAt, match.FailureCode, match.FailureMessage, match.AttemptCount
    };

    private static bool IsCommunityWin(CounterStrikeMatch match) => match.CommunityTeam == "A"
        ? match.TeamAScore > match.TeamBScore
        : match.CommunityTeam == "B" && match.TeamBScore > match.TeamAScore;

    private static (int Count, bool IsWin) CalculateStreak(IReadOnlyList<CounterStrikeMatch> matches)
    {
        if (matches.Count == 0)
        {
            return (0, true);
        }
        var win = IsCommunityWin(matches[0]);
        var count = matches.TakeWhile(match => IsCommunityWin(match) == win).Count();
        return (count, win);
    }

    private static int LongestWinStreak(IEnumerable<CounterStrikeMatch> matches)
    {
        var longest = 0;
        var current = 0;
        foreach (var match in matches)
        {
            current = IsCommunityWin(match) ? current + 1 : 0;
            longest = Math.Max(longest, current);
        }
        return longest;
    }

    private static CounterStrikeTrainingExercise NewPlanExercise(Guid organizationId, Guid planId, CounterStrikeTrainingKind kind, string name, int minutes, int order) => new()
    {
        Id = Guid.NewGuid(), OrganizationId = organizationId, TrainingPlanId = planId, Kind = kind,
        Name = name, Description = "Konzentriert, sauber und ohne unnötige Wiederholungen.", DurationMinutes = minutes, SortOrder = order
    };

    private static async Task<IResult> ListClipsAsync(
        Guid organizationId, ClaimsPrincipal principal, ICounterStrikeDbContext dbContext,
        IOrganizationAccessService accessService, CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(organizationId, principal, accessService, cancellationToken);
        if (access.Result is not null) return access.Result;
        var canManage = access.Membership!.PermissionRole.CanManageOrganization();
        var rows = await (
            from clip in dbContext.CounterStrikeClips.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on clip.UploadedByUserId equals user.Id
            where clip.OrganizationId == organizationId
            orderby clip.CreatedAt descending
            select new
            {
                clip.Id, clip.Title, clip.Description, clip.OriginalFileName, clip.MimeType,
                clip.SizeBytes, clip.CreatedAt, uploader = user.DisplayName, user.AvatarUrl,
                clip.UploadedByUserId
            }).Take(100).ToArrayAsync(cancellationToken);
        var clips = rows.Select(clip => new
        {
            clip.Id, clip.Title, clip.Description, clip.OriginalFileName, clip.MimeType,
            clip.SizeBytes, clip.CreatedAt, clip.uploader, clip.AvatarUrl,
            contentUrl = $"/api/organizations/{organizationId}/counter-strike/clips/{clip.Id}/content",
            canDelete = canManage || clip.UploadedByUserId == access.UserId
        });
        return Results.Ok(clips);
    }

    private static async Task<IResult> UploadClipAsync(
        Guid organizationId, HttpRequest request, ClaimsPrincipal principal, ICounterStrikeDbContext dbContext,
        IOrganizationAccessService accessService, ICounterStrikeClipStorage storage,
        TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(organizationId, principal, accessService, cancellationToken);
        if (access.Result is not null) return access.Result;
        if (!request.HasFormContentType) return Validation("file", "Bitte lade einen Clip als Formulardatei hoch.");
        var form = await request.ReadFormAsync(cancellationToken);
        var title = form["title"].ToString().Trim();
        var description = form["description"].ToString().Trim();
        var startRaw = form["startSeconds"].ToString();
        var endRaw = form["endSeconds"].ToString();
        var quality = form["quality"].ToString();
        var file = form.Files.GetFile("file");
        if (title.Length is < 2 or > 120) return Validation("title", "Der Titel braucht 2 bis 120 Zeichen.");
        if (description.Length > 500) return Validation("description", "Die Beschreibung darf höchstens 500 Zeichen lang sein.");
        if (file is null) return Validation("file", "Bitte wähle einen Videoclip aus.");
        if (!double.TryParse(startRaw, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var startSeconds))
            return Validation("trim", "Der Startpunkt ist ungültig.");
        double? endSeconds = null;
        if (!string.IsNullOrWhiteSpace(endRaw))
        {
            if (!double.TryParse(endRaw, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var parsedEnd))
                return Validation("trim", "Der Endpunkt ist ungültig.");
            endSeconds = parsedEnd;
        }
        StoredCounterStrikeClip stored;
        try { stored = await storage.SaveAsync(organizationId, file, startSeconds, endSeconds, quality, cancellationToken); }
        catch (CounterStrikeUploadException exception) { return Validation(exception.Key, exception.Message); }
        var clip = new CounterStrikeClip
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, UploadedByUserId = access.UserId,
            UploadedByMemberId = access.Membership!.MemberId, Title = title,
            Description = description.Length == 0 ? null : description, OriginalFileName = stored.OriginalFileName,
            StoragePath = stored.Path, MimeType = stored.MimeType, SizeBytes = stored.Size,
            CreatedAt = timeProvider.GetUtcNow()
        };
        dbContext.CounterStrikeClips.Add(clip);
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch { storage.Delete(stored.Path); throw; }
        return Results.Created($"/api/organizations/{organizationId}/counter-strike/clips/{clip.Id}", new { clip.Id });
    }

    private static async Task<IResult> GetClipContentAsync(
        Guid organizationId, Guid clipId, ClaimsPrincipal principal, ICounterStrikeDbContext dbContext,
        IOrganizationAccessService accessService, ICounterStrikeClipStorage storage, CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(organizationId, principal, accessService, cancellationToken);
        if (access.Result is not null) return access.Result;
        var clip = await dbContext.CounterStrikeClips.AsNoTracking().SingleOrDefaultAsync(
            item => item.OrganizationId == organizationId && item.Id == clipId, cancellationToken);
        if (clip is null) return Results.NotFound();
        try { return Results.File(storage.OpenRead(clip.StoragePath), clip.MimeType, enableRangeProcessing: true); }
        catch (FileNotFoundException) { return Results.NotFound(); }
    }

    private static async Task<IResult> DeleteClipAsync(
        Guid organizationId, Guid clipId, ClaimsPrincipal principal, ICounterStrikeDbContext dbContext,
        IOrganizationAccessService accessService, ICounterStrikeClipStorage storage, CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(organizationId, principal, accessService, cancellationToken);
        if (access.Result is not null) return access.Result;
        var clip = await dbContext.CounterStrikeClips.SingleOrDefaultAsync(
            item => item.OrganizationId == organizationId && item.Id == clipId, cancellationToken);
        if (clip is null) return Results.NotFound();
        if (clip.UploadedByUserId != access.UserId && !access.Membership!.PermissionRole.CanManageOrganization())
            return Results.Forbid();
        dbContext.CounterStrikeClips.Remove(clip);
        await dbContext.SaveChangesAsync(cancellationToken);
        storage.Delete(clip.StoragePath);
        return Results.NoContent();
    }

    private static IResult Validation(string key, string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { [key] = [message] });

    private static async Task<AccessResult> GetAccessAsync(
        Guid organizationId, ClaimsPrincipal principal, IOrganizationAccessService accessService, CancellationToken cancellationToken)
    {
        var rawUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        if (!Guid.TryParse(rawUserId, out var userId))
        {
            return new AccessResult(Guid.Empty, null, Results.Unauthorized());
        }
        var membership = await accessService.GetActiveMembershipAsync(organizationId, userId, cancellationToken);
        return membership is null
            ? new AccessResult(userId, null, Results.NotFound())
            : new AccessResult(userId, membership, null);
    }

    private sealed record AccessResult(Guid UserId, OrganizationMembership? Membership, IResult? Result);
    private sealed record CounterStrikeAwardResponse(
        Guid Id,
        string Name,
        string Description,
        string Icon,
        string DisplayName,
        double Value);
}

public sealed record UpdatePlayRequest(
    CounterStrikeAvailability Availability,
    TimeOnly? AvailableFrom,
    TimeOnly? PlannedStart);

public sealed record ToggleReactionRequest(string Reaction);
public sealed record UpdateRoleRequest(CounterStrikePlayerRole Role);
public sealed record UpdateSquadSettingsRequest(string? SquadName, string? SquadTag);
public sealed record UpdateRosterStatusRequest(CounterStrikeRosterStatus Status);
public sealed record CreateSeasonRequest(string Name, DateTimeOffset? StartsAt);
public sealed record CounterStrikeSyncStatusResponse(
    string Source,
    bool AutomaticSyncAvailable,
    int MaximumDemoMegabytes,
    CounterStrikeSteamConnectionResponse Steam,
    CounterStrikeImportCountsResponse Imports);
public sealed record CounterStrikeSteamConnectionResponse(
    bool Connected,
    string? SteamId64,
    string? DisplayName,
    string? AvatarUrl,
    DateTimeOffset? LinkedAt);
public sealed record CounterStrikeImportCountsResponse(
    int Total,
    int Queued,
    int Processing,
    int Completed,
    int Failed,
    DateTimeOffset? LastImportedAt,
    DateTimeOffset? LastCompletedAt);
public sealed record SaveTrainingResultRequest(
    CounterStrikeTrainingKind Kind,
    int DurationSeconds,
    int Hits,
    int Misses,
    double ReactionTimeMs,
    double FlickTimeMs,
    double TrackingPercent,
    int Repetitions,
    Guid? TrainingPlanId,
    Guid? TrainingExerciseId);
