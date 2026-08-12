using System.Security.Cryptography;
using System.Text.Json;
using CommunityIntranet.Modules.Mystery.Contracts;
using CommunityIntranet.Modules.Mystery.Domain;
using CommunityIntranet.Modules.Mystery.Game;
using CommunityIntranet.Modules.Mystery.Persistence;
using CommunityIntranet.Modules.Mystery.Providers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.Mystery.Endpoints;

public static class MysteryEndpoints
{
    private const string JoinCodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static IEndpointRouteBuilder MapMysteryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/mistery")
            .WithTags("Mystery")
            .RequireRateLimiting("mystery-public");

        group.MapPost("/sessions", CreateSessionAsync)
            .RequireRateLimiting("mystery-generation");
        group.MapGet("/sessions/{sessionId:guid}", GetSessionAsync);
        group.MapGet("/sessions/code/{joinCode}", GetSessionByCodeAsync);
        group.MapPost("/sessions/{sessionId:guid}/advance", AdvanceAsync);
        group.MapPost("/sessions/{sessionId:guid}/puzzle", SubmitPuzzleAsync);
        group.MapPost("/sessions/{sessionId:guid}/decision", SubmitDecisionAsync);
        group.MapPost("/sessions/{sessionId:guid}/hints", RequestHintAsync);
        group.MapPost("/sessions/{sessionId:guid}/questions", AskQuestionAsync)
            .RequireRateLimiting("mystery-questions");
        group.MapPut("/sessions/{sessionId:guid}/notes", UpdateNotesAsync);
        group.MapPost("/sessions/{sessionId:guid}/finale", SubmitFinaleAsync);
        return endpoints;
    }

    private static async Task<IResult> CreateSessionAsync(
        CreateMysterySessionRequest request,
        IMysteryDbContext dbContext,
        IMysteryLlmProvider provider,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var validation = Validate(request);
        if (validation.Count > 0)
        {
            return Results.ValidationProblem(validation);
        }

        var configuration = new MysteryGameConfiguration
        {
            Players = request.Players.Select(x => x.Trim()).ToArray(),
            DurationMinutes = request.DurationMinutes,
            Difficulty = request.Difficulty,
            Genre = request.Genre.Trim(),
            Atmosphere = request.Atmosphere.Trim(),
            Locations = request.Locations?.Select(x => new MysteryLocationOption
            {
                Id = x.Id.Trim().ToUpperInvariant(),
                Description = x.Description.Trim(),
                AvailableFromProgress = x.AvailableFromProgress,
                PreferredUse = x.PreferredUse.Trim()
            }).ToArray() ?? [],
            AvailableItems = request.AvailableItems?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? []
        };

        var generated = await provider.GenerateCaseAsync(configuration, cancellationToken);
        var mysteryCase = MysteryCaseGuard.ValidateAndNormalize(generated.Case, configuration);
        var state = MysteryGameEngine.CreateInitialState(mysteryCase);
        var now = timeProvider.GetUtcNow();
        var session = new MysterySession
        {
            Id = Guid.NewGuid(),
            JoinCode = await CreateUniqueJoinCodeAsync(dbContext, cancellationToken),
            Title = mysteryCase.Title,
            Status = state.Status,
            GameMaster = generated.ProviderName,
            Notice = generated.Notice,
            ConfigurationJson = JsonSerializer.Serialize(configuration, MysteryJson.Options),
            SecretCaseJson = JsonSerializer.Serialize(mysteryCase, MysteryJson.Options),
            GameStateJson = JsonSerializer.Serialize(state, MysteryJson.Options),
            Version = Guid.NewGuid(),
            CreatedAt = now,
            UpdatedAt = now
        };
        dbContext.MysterySessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/mistery/sessions/{session.Id}", MysterySessionMapper.Map(session));
    }

    private static async Task<IResult> GetSessionAsync(
        Guid sessionId,
        IMysteryDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var session = await dbContext.MysterySessions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        return session is null ? Results.NotFound() : Results.Ok(MysterySessionMapper.Map(session));
    }

    private static async Task<IResult> GetSessionByCodeAsync(
        string joinCode,
        IMysteryDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var normalized = joinCode.Trim().ToUpperInvariant();
        var session = await dbContext.MysterySessions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.JoinCode == normalized, cancellationToken);
        return session is null ? Results.NotFound() : Results.Ok(MysterySessionMapper.Map(session));
    }

    private static async Task<IResult> AdvanceAsync(
        Guid sessionId,
        MysteryVersionRequest request,
        IMysteryDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var loaded = await LoadForUpdateAsync(sessionId, request.Version, dbContext, cancellationToken);
        if (loaded.Error is not null) return loaded.Error;
        var result = MysteryGameEngine.Advance(loaded.Case!, loaded.State!);
        if (!result.IsSuccess) return Conflict(result.Error!);
        var saveError = await SaveAsync(loaded.Session!, loaded.State!, dbContext, timeProvider, cancellationToken);
        return saveError ?? Results.Ok(MysterySessionMapper.Map(loaded.Session!));
    }

    private static async Task<IResult> SubmitPuzzleAsync(
        Guid sessionId,
        SubmitMysteryPuzzleRequest request,
        IMysteryDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Answer) || request.Answer.Length > 300)
        {
            return Validation("answer", "Gebt eine Antwort mit maximal 300 Zeichen ein.");
        }

        var loaded = await LoadForUpdateAsync(sessionId, request.Version, dbContext, cancellationToken);
        if (loaded.Error is not null) return loaded.Error;
        var result = MysteryGameEngine.SubmitPuzzle(loaded.Case!, loaded.State!, request.Answer);
        var saveError = await SaveAsync(loaded.Session!, loaded.State!, dbContext, timeProvider, cancellationToken);
        return saveError ?? Results.Ok(new MysteryPuzzleResultResponse(
            result.IsCorrect,
            result.Message,
            MysterySessionMapper.Map(loaded.Session!)));
    }

    private static async Task<IResult> SubmitDecisionAsync(
        Guid sessionId,
        SubmitMysteryDecisionRequest request,
        IMysteryDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var loaded = await LoadForUpdateAsync(sessionId, request.Version, dbContext, cancellationToken);
        if (loaded.Error is not null) return loaded.Error;
        var result = MysteryGameEngine.Choose(loaded.Case!, loaded.State!, request.ChoiceId);
        if (!result.IsSuccess) return Conflict(result.Error!);
        var saveError = await SaveAsync(loaded.Session!, loaded.State!, dbContext, timeProvider, cancellationToken);
        return saveError ?? Results.Ok(MysterySessionMapper.Map(loaded.Session!));
    }

    private static async Task<IResult> RequestHintAsync(
        Guid sessionId,
        RequestMysteryHintRequest request,
        IMysteryDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (request.Level is < 1 or > 3)
        {
            return Validation("level", "Wählt eine Hinweisstufe zwischen 1 und 3.");
        }

        var loaded = await LoadForUpdateAsync(sessionId, request.Version, dbContext, cancellationToken);
        if (loaded.Error is not null) return loaded.Error;
        string hint;
        try
        {
            hint = MysteryGameEngine.GetHint(
                loaded.Case!, loaded.State!, request.Level, timeProvider.GetUtcNow());
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(exception.Message);
        }

        var saveError = await SaveAsync(loaded.Session!, loaded.State!, dbContext, timeProvider, cancellationToken);
        return saveError ?? Results.Ok(new MysteryHintResponse(
            request.Level,
            hint,
            MysterySessionMapper.Map(loaded.Session!)));
    }

    private static async Task<IResult> AskQuestionAsync(
        Guid sessionId,
        AskMysteryQuestionRequest request,
        IMysteryDbContext dbContext,
        IMysteryLlmProvider provider,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question) || request.Question.Length > 500)
        {
            return Validation("question", "Stellt eine Frage mit maximal 500 Zeichen.");
        }

        var loaded = await LoadForUpdateAsync(sessionId, request.Version, dbContext, cancellationToken);
        if (loaded.Error is not null) return loaded.Error;
        var generated = await provider.AnswerPlayerQuestionAsync(
            loaded.Case!, loaded.State!, request.Question.Trim(), cancellationToken);
        var answer = MysterySpoilerGuard.ProtectAnswer(loaded.Case!, loaded.State!, generated);
        loaded.State!.Questions.Add(new MysteryQuestionAnswer
        {
            Question = request.Question.Trim(),
            Answer = answer,
            AskedAt = timeProvider.GetUtcNow()
        });
        if (loaded.State.Questions.Count > 30)
        {
            loaded.State.Questions.RemoveRange(0, loaded.State.Questions.Count - 30);
        }

        var saveError = await SaveAsync(loaded.Session!, loaded.State, dbContext, timeProvider, cancellationToken);
        return saveError ?? Results.Ok(new MysteryQuestionAnswerResponse(
            answer,
            MysterySessionMapper.Map(loaded.Session!)));
    }

    private static async Task<IResult> UpdateNotesAsync(
        Guid sessionId,
        UpdateMysteryNotesRequest request,
        IMysteryDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (request.Notes.Length > 30 || request.Notes.Any(x => x.Length > 500))
        {
            return Validation("notes", "Speichert maximal 30 Notizen mit je 500 Zeichen.");
        }

        var loaded = await LoadForUpdateAsync(sessionId, request.Version, dbContext, cancellationToken);
        if (loaded.Error is not null) return loaded.Error;
        loaded.State!.Notes = request.Notes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToList();
        var saveError = await SaveAsync(loaded.Session!, loaded.State, dbContext, timeProvider, cancellationToken);
        return saveError ?? Results.Ok(MysterySessionMapper.Map(loaded.Session!));
    }

    private static async Task<IResult> SubmitFinaleAsync(
        Guid sessionId,
        SubmitMysteryFinaleRequest request,
        IMysteryDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CulpritId)
            || string.IsNullOrWhiteSpace(request.Motive)
            || request.Motive.Length > 1200
            || request.Sequence?.Length > 1800)
        {
            return Validation("theory", "Nennt Täter und Motiv in einer kompakten Theorie.");
        }

        var loaded = await LoadForUpdateAsync(sessionId, request.Version, dbContext, cancellationToken);
        if (loaded.Error is not null) return loaded.Error;
        var result = MysteryGameEngine.Complete(loaded.Case!, loaded.State!, new MysteryFinalTheory
        {
            CulpritId = request.CulpritId,
            Motive = request.Motive.Trim(),
            Sequence = request.Sequence?.Trim()
        });
        if (!result.IsSuccess) return Conflict(result.Error!);
        var saveError = await SaveAsync(loaded.Session!, loaded.State!, dbContext, timeProvider, cancellationToken);
        return saveError ?? Results.Ok(MysterySessionMapper.Map(loaded.Session!));
    }

    private static Dictionary<string, string[]> Validate(CreateMysterySessionRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (request.Players is not { Length: >= 1 and <= 12 }
            || request.Players.Any(x => string.IsNullOrWhiteSpace(x) || x.Trim().Length > 80))
        {
            errors["players"] = ["Gebt 1 bis 12 Spielernamen mit maximal 80 Zeichen an."];
        }
        else if (request.Players.Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count()
            != request.Players.Length)
        {
            errors["players"] = ["Spielernamen müssen eindeutig sein."];
        }

        if (request.DurationMinutes is < 30 or > 240)
        {
            errors["durationMinutes"] = ["Die Spieldauer muss zwischen 30 und 240 Minuten liegen."];
        }
        if (string.IsNullOrWhiteSpace(request.Genre) || request.Genre.Trim().Length > 100)
        {
            errors["genre"] = ["Gebt ein Genre mit maximal 100 Zeichen an."];
        }
        if (string.IsNullOrWhiteSpace(request.Atmosphere) || request.Atmosphere.Trim().Length > 200)
        {
            errors["atmosphere"] = ["Beschreibt die Atmosphäre mit maximal 200 Zeichen."];
        }
        if (request.Locations is { Length: > 8 }
            || request.Locations?.Any(x =>
                string.IsNullOrWhiteSpace(x.Id)
                || x.Id.Length > 80
                || x.Id.Any(character => !char.IsLetterOrDigit(character) && character is not '-' and not '_')
                || string.IsNullOrWhiteSpace(x.Description)
                || x.Description.Length > 500
                || x.AvailableFromProgress is < 0 or > 1
                || x.PreferredUse.Length > 100) == true)
        {
            errors["locations"] = ["Prüft IDs, Beschreibungen und Freigabefortschritt der realen Orte."];
        }
        else if (request.Locations?.Select(x => x.Id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase).Count() != request.Locations?.Length)
        {
            errors["locations"] = ["Location-IDs müssen eindeutig sein."];
        }
        if (request.AvailableItems is { Length: > 20 }
            || request.AvailableItems?.Any(x => x.Length > 200) == true)
        {
            errors["availableItems"] = ["Gebt maximal 20 Gegenstände mit je 200 Zeichen an."];
        }
        return errors;
    }

    private static async Task<LoadedSession> LoadForUpdateAsync(
        Guid sessionId,
        Guid? expectedVersion,
        IMysteryDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var session = await dbContext.MysterySessions.SingleOrDefaultAsync(
            x => x.Id == sessionId,
            cancellationToken);
        if (session is null) return new LoadedSession(Error: Results.NotFound());
        if (expectedVersion is not null && session.Version != expectedVersion)
        {
            return new LoadedSession(Error: Conflict(
                "Der Spielstand wurde auf einem anderen Gerät geändert. Ladet den aktuellen Stand neu."));
        }

        return new LoadedSession(
            session,
            MysterySessionMapper.Deserialize<MysteryCaseDefinition>(session.SecretCaseJson),
            MysterySessionMapper.Deserialize<MysteryGameState>(session.GameStateJson));
    }

    private static async Task<IResult?> SaveAsync(
        MysterySession session,
        MysteryGameState state,
        IMysteryDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        session.Status = state.Status;
        session.GameStateJson = JsonSerializer.Serialize(state, MysteryJson.Options);
        session.Version = Guid.NewGuid();
        session.UpdatedAt = timeProvider.GetUtcNow();
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("Der Spielstand wurde gleichzeitig geändert. Ladet den aktuellen Stand neu.");
        }
    }

    private static async Task<string> CreateUniqueJoinCodeAsync(
        IMysteryDbContext dbContext,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var characters = new char[6];
            for (var index = 0; index < characters.Length; index++)
            {
                characters[index] = JoinCodeAlphabet[RandomNumberGenerator.GetInt32(JoinCodeAlphabet.Length)];
            }
            var code = new string(characters);
            if (!await dbContext.MysterySessions.AnyAsync(x => x.JoinCode == code, cancellationToken))
            {
                return code;
            }
        }
        throw new InvalidOperationException("Es konnte kein eindeutiger Beitrittscode erzeugt werden.");
    }

    private static IResult Validation(string key, string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { [key] = [message] });

    private static IResult Conflict(string message) =>
        Results.Conflict(new { message });

    private sealed record LoadedSession(
        MysterySession? Session = null,
        MysteryCaseDefinition? Case = null,
        MysteryGameState? State = null,
        IResult? Error = null);
}
