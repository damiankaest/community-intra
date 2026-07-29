using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using CommunityIntranet.BuildingBlocks.ActivityFeed;
using CommunityIntranet.BuildingBlocks.Authorization;
using CommunityIntranet.BuildingBlocks.LiveOperations;
using CommunityIntranet.BuildingBlocks.Tenancy;
using CommunityIntranet.Modules.FactoryInsights.Contracts;
using CommunityIntranet.Modules.FactoryInsights.Domain;
using CommunityIntranet.Modules.FactoryInsights.Persistence;
using CommunityIntranet.Modules.FactoryInsights.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.FactoryInsights.Endpoints;

public static class FactoryInsightsEndpoints
{
    private const long MaximumSaveBytes = 200L * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapFactoryInsightsEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup(
                "/api/organizations/{organizationId:guid}/factory-insights")
            .WithTags("Factory Insights")
            .RequireAuthorization();
        group.MapGet("", GetOverviewAsync);
        group.MapPost("/factories", CreateFactoryAsync);
        group.MapDelete("/factories/{factoryId:guid}", DeleteFactoryAsync);
        group.MapPost("/imports/manual", ImportManualAsync)
            .WithMetadata(new RequestSizeLimitAttribute(
                MaximumSaveBytes + 1_048_576))
            .DisableAntiforgery();
        group.MapPost("/imports/server", ImportServerAsync);
        return endpoints;
    }

    private static async Task<IResult> GetOverviewAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        IFactoryInsightsDbContext dbContext,
        IOrganizationAccessService accessService,
        ISaveFileAnalyzer analyzer,
        ILiveOperationsReader liveOperationsReader,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(
            organizationId,
            principal,
            accessService,
            cancellationToken);
        if (access.Result is not null)
        {
            return access.Result;
        }

        var factories = await dbContext.FactorySites
            .AsNoTracking()
            .Where(factory => factory.OrganizationId == organizationId)
            .OrderBy(factory => factory.Name)
            .ToArrayAsync(cancellationToken);
        var snapshots = await dbContext.SaveSnapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.OrganizationId == organizationId)
            .OrderByDescending(snapshot => snapshot.ImportedAt)
            .Take(10)
            .ToArrayAsync(cancellationToken);
        var latestAnalysis = snapshots.FirstOrDefault() is { } latest
            ? DeserializeAnalysis(latest.AnalysisJson)
            : null;
        var parserAvailable = await analyzer.IsAvailableAsync(cancellationToken);
        var serverStatus = await liveOperationsReader.GetServerStatusAsync(
            organizationId,
            forceRefresh: false,
            cancellationToken);
        return Results.Ok(new FactoryInsightsOverviewResponse(
            factories.Select(factory =>
                ToResponse(factory, MatchArea(factory, latestAnalysis)))
                .ToArray(),
            snapshots.FirstOrDefault() is { } newest
                ? ToResponse(newest, latestAnalysis)
                : null,
            snapshots.Select(snapshot => ToResponse(snapshot, null)).ToArray(),
            parserAvailable,
            serverStatus.State,
            serverStatus.Message));
    }

    private static async Task<IResult> CreateFactoryAsync(
        Guid organizationId,
        SaveFactoryRequest request,
        ClaimsPrincipal principal,
        IFactoryInsightsDbContext dbContext,
        IOrganizationAccessService accessService,
        IActivityWriter activityWriter,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(
            organizationId,
            principal,
            accessService,
            cancellationToken);
        if (access.Result is not null)
        {
            return access.Result;
        }

        if (!access.Membership!.PermissionRole.CanCreateContent())
        {
            return Results.Forbid();
        }

        var validation = ValidateFactory(request);
        if (validation is not null)
        {
            return validation;
        }

        var now = timeProvider.GetUtcNow();
        var factory = new FactorySite
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = request.Name!.Trim(),
            Description = Normalize(request.Description),
            CenterX = request.CenterX,
            CenterY = request.CenterY,
            RadiusMeters = request.RadiusMeters,
            CreatedAt = now,
            UpdatedAt = now,
            ConcurrencyToken = Guid.NewGuid()
        };
        dbContext.FactorySites.Add(factory);
        activityWriter.Add(new ActivityDraft(
            organizationId,
            "factory.created",
            access.Membership.MemberId,
            "factory",
            factory.Id,
            new Dictionary<string, string?> { ["factoryName"] = factory.Name }));
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Created(
            $"/api/organizations/{organizationId}/factory-insights/factories/{factory.Id}",
            ToResponse(factory, null));
    }

    private static async Task<IResult> DeleteFactoryAsync(
        Guid organizationId,
        Guid factoryId,
        ClaimsPrincipal principal,
        IFactoryInsightsDbContext dbContext,
        IOrganizationAccessService accessService,
        IActivityWriter activityWriter,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(
            organizationId,
            principal,
            accessService,
            cancellationToken);
        if (access.Result is not null)
        {
            return access.Result;
        }

        if (!access.Membership!.PermissionRole.CanManageContent())
        {
            return Results.Forbid();
        }

        var factory = await dbContext.FactorySites.SingleOrDefaultAsync(
            item =>
                item.OrganizationId == organizationId && item.Id == factoryId,
            cancellationToken);
        if (factory is null)
        {
            return Results.NotFound();
        }

        dbContext.FactorySites.Remove(factory);
        activityWriter.Add(new ActivityDraft(
            organizationId,
            "factory.deleted",
            access.Membership.MemberId,
            "factory",
            factory.Id,
            new Dictionary<string, string?> { ["factoryName"] = factory.Name }));
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ImportManualAsync(
        Guid organizationId,
        [FromForm] IFormFile file,
        ClaimsPrincipal principal,
        IFactoryInsightsDbContext dbContext,
        IOrganizationAccessService accessService,
        ISaveFileAnalyzer analyzer,
        IActivityWriter activityWriter,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(
            organizationId,
            principal,
            accessService,
            cancellationToken);
        if (access.Result is not null)
        {
            return access.Result;
        }

        if (!access.Membership!.PermissionRole.CanCreateContent())
        {
            return Results.Forbid();
        }

        if (!file.FileName.EndsWith(".sav", StringComparison.OrdinalIgnoreCase)
            || file.Length is <= 0 or > MaximumSaveBytes)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["file"] =
                [
                    "Wähle eine nicht leere Satisfactory-.sav-Datei bis 200 MB."
                ]
            });
        }

        await using var stream = file.OpenReadStream();
        using var destination = new MemoryStream(
            file.Length <= int.MaxValue ? (int)file.Length : 0);
        await stream.CopyToAsync(destination, cancellationToken);
        return await ImportAsync(
            organizationId,
            access.Membership.MemberId,
            SaveImportSource.ManualUpload,
            Path.GetFileName(file.FileName),
            destination.ToArray(),
            dbContext,
            analyzer,
            activityWriter,
            timeProvider,
            cancellationToken);
    }

    private static async Task<IResult> ImportServerAsync(
        Guid organizationId,
        ServerSaveImportRequest request,
        ClaimsPrincipal principal,
        IFactoryInsightsDbContext dbContext,
        IOrganizationAccessService accessService,
        ISatisfactorySaveProvider saveProvider,
        ISaveFileAnalyzer analyzer,
        IActivityWriter activityWriter,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(
            organizationId,
            principal,
            accessService,
            cancellationToken);
        if (access.Result is not null)
        {
            return access.Result;
        }

        if (!access.Membership!.PermissionRole.CanManageOrganization())
        {
            return Results.Forbid();
        }

        var downloaded = await saveProvider.DownloadAsync(
            organizationId,
            request.SaveName,
            cancellationToken);
        if (downloaded.State != ServerSaveFetchState.Downloaded
            || downloaded.Content is null
            || downloaded.FileName is null)
        {
            return downloaded.State switch
            {
                ServerSaveFetchState.AuthenticationFailed =>
                    Results.Json(
                        new
                        {
                            title = "API-Token reicht nicht aus",
                            detail = downloaded.Message
                        },
                        statusCode: StatusCodes.Status401Unauthorized),
                ServerSaveFetchState.NotFound => Results.NotFound(new
                {
                    title = "Kein Spielstand gefunden",
                    detail = downloaded.Message
                }),
                ServerSaveFetchState.NotConfigured
                    or ServerSaveFetchState.Disabled
                    or ServerSaveFetchState.ConfigurationError =>
                    Results.Conflict(new
                    {
                        title = "Serverimport nicht bereit",
                        detail = downloaded.Message
                    }),
                _ => Results.Json(
                    new
                    {
                        title = "Gameserver nicht erreichbar",
                        detail = downloaded.Message
                    },
                    statusCode: StatusCodes.Status503ServiceUnavailable)
            };
        }

        return await ImportAsync(
            organizationId,
            access.Membership.MemberId,
            SaveImportSource.ServerApi,
            downloaded.FileName,
            downloaded.Content,
            dbContext,
            analyzer,
            activityWriter,
            timeProvider,
            cancellationToken);
    }

    private static async Task<IResult> ImportAsync(
        Guid organizationId,
        Guid memberId,
        SaveImportSource source,
        string fileName,
        byte[] content,
        IFactoryInsightsDbContext dbContext,
        ISaveFileAnalyzer analyzer,
        IActivityWriter activityWriter,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var hash = Convert.ToHexString(SHA256.HashData(content))
            .ToLowerInvariant();
        var existing = await dbContext.SaveSnapshots
            .AsNoTracking()
            .SingleOrDefaultAsync(
                snapshot =>
                    snapshot.OrganizationId == organizationId
                    && snapshot.ContentSha256 == hash,
                cancellationToken);
        if (existing is not null)
        {
            return Results.Ok(ToResponse(
                existing,
                DeserializeAnalysis(existing.AnalysisJson)));
        }

        SaveAnalysis analysis;
        try
        {
            analysis = await analyzer.AnalyzeAsync(
                content,
                fileName,
                cancellationToken);
        }
        catch (InvalidDataException exception)
        {
            return Results.UnprocessableEntity(new
            {
                title = "Save konnte nicht analysiert werden",
                detail = exception.Message
            });
        }
        catch (HttpRequestException)
        {
            return Results.Json(
                new
                {
                    title = "Save-Parser nicht erreichbar",
                    detail = "Der interne Analysedienst antwortet gerade nicht."
                },
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var now = timeProvider.GetUtcNow();
        var snapshot = new SaveSnapshot
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ImportedByMemberId = memberId,
            Source = source,
            OriginalFileName = fileName,
            ContentSha256 = hash,
            FileSizeBytes = content.LongLength,
            SaveName = Normalize(analysis.SaveName),
            SessionName = Normalize(analysis.SessionName),
            MapName = Normalize(analysis.MapName),
            SaveVersion = analysis.SaveVersion,
            BuildVersion = analysis.BuildVersion,
            PlayDurationSeconds = analysis.PlayDurationSeconds,
            SavedAt = analysis.SavedAt,
            IsModdedSave = analysis.IsModdedSave,
            ParserVersion = analysis.ParserVersion,
            AnalysisJson = JsonSerializer.Serialize(analysis, JsonOptions),
            ImportedAt = now
        };
        dbContext.SaveSnapshots.Add(snapshot);
        activityWriter.Add(new ActivityDraft(
            organizationId,
            "save_snapshot.imported",
            memberId,
            "save_snapshot",
            snapshot.Id,
            new Dictionary<string, string?>
            {
                ["fileName"] = snapshot.OriginalFileName,
                ["source"] = snapshot.Source.ToString(),
                ["buildables"] = analysis.Totals.Buildables.ToString(
                    System.Globalization.CultureInfo.InvariantCulture)
            }));
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Created(
            $"/api/organizations/{organizationId}/factory-insights/snapshots/{snapshot.Id}",
            ToResponse(snapshot, analysis));
    }

    private static IResult? ValidateFactory(SaveFactoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)
            || request.Name.Trim().Length > 120)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["name"] = ["Ein Name mit maximal 120 Zeichen ist erforderlich."]
            });
        }

        if (request.Description?.Trim().Length > 500)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["description"] = ["Die Beschreibung darf maximal 500 Zeichen haben."]
            });
        }

        var coordinateCount = new[]
        {
            request.CenterX,
            request.CenterY,
            request.RadiusMeters
        }.Count(value => value.HasValue);
        if (coordinateCount is > 0 and < 3
            || request.RadiusMeters is <= 0 or > 10000)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["location"] =
                [
                    "Position und Radius müssen gemeinsam angegeben werden; der Radius darf höchstens 10 km betragen."
                ]
            });
        }

        return null;
    }

    private static FactorySiteResponse ToResponse(
        FactorySite factory,
        DetectedFactoryArea? area) =>
        new(
            factory.Id,
            factory.Name,
            factory.Description,
            factory.CenterX,
            factory.CenterY,
            factory.RadiusMeters,
            area?.MachineCount,
            area?.BuildableCount,
            factory.UpdatedAt,
            factory.ConcurrencyToken);

    private static SaveSnapshotResponse ToResponse(
        SaveSnapshot snapshot,
        SaveAnalysis? analysis) =>
        new(
            snapshot.Id,
            snapshot.Source,
            snapshot.OriginalFileName,
            snapshot.ContentSha256,
            snapshot.FileSizeBytes,
            snapshot.SaveName,
            snapshot.SessionName,
            snapshot.MapName,
            snapshot.SaveVersion,
            snapshot.BuildVersion,
            snapshot.PlayDurationSeconds,
            snapshot.SavedAt,
            snapshot.IsModdedSave,
            snapshot.ParserVersion,
            snapshot.ImportedAt,
            analysis);

    private static DetectedFactoryArea? MatchArea(
        FactorySite factory,
        SaveAnalysis? analysis)
    {
        if (factory.CenterX is null
            || factory.CenterY is null
            || factory.RadiusMeters is null
            || analysis is null)
        {
            return null;
        }

        return analysis.DetectedAreas
            .Select(area => new
            {
                Area = area,
                Distance = Math.Sqrt(
                    Math.Pow(area.CenterX - factory.CenterX.Value, 2)
                    + Math.Pow(area.CenterY - factory.CenterY.Value, 2))
            })
            .Where(match =>
                match.Distance <= Math.Max(
                    factory.RadiusMeters.Value,
                    match.Area.RadiusMeters) * 100)
            .OrderBy(match => match.Distance)
            .Select(match => match.Area)
            .FirstOrDefault();
    }

    private static SaveAnalysis? DeserializeAnalysis(string value)
    {
        try
        {
            return JsonSerializer.Deserialize<SaveAnalysis>(value, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static async Task<AccessResult> GetAccessAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        IOrganizationAccessService accessService,
        CancellationToken cancellationToken)
    {
        var rawUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        if (!Guid.TryParse(rawUserId, out var userId))
        {
            return new AccessResult(null, Results.Unauthorized());
        }

        var membership = await accessService.GetActiveMembershipAsync(
            organizationId,
            userId,
            cancellationToken);
        return membership is null
            ? new AccessResult(null, Results.NotFound())
            : new AccessResult(membership, null);
    }

    private sealed record AccessResult(
        OrganizationMembership? Membership,
        IResult? Result);
}
