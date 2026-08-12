using System.Text.Json;
using CommunityIntranet.Modules.Mystery.Domain;
using CommunityIntranet.Modules.Mystery.Game;
using CommunityIntranet.Modules.Mystery.Providers;
using Xunit;

namespace CommunityIntranet.Api.Tests.Mystery;

public sealed class MysteryGameEngineTests
{
    [Fact]
    public async Task PublicResponseContainsNeitherSecretFieldsNorFutureStoryData()
    {
        var fixture = await CreateFixtureAsync();

        var response = MysterySessionMapper.Map(fixture.Session);
        var json = JsonSerializer.Serialize(response, MysteryJson.Options);

        Assert.DoesNotContain("culpritId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(fixture.Case.Motive, json, StringComparison.Ordinal);
        Assert.DoesNotContain(fixture.Case.Resolution, json, StringComparison.Ordinal);
        Assert.DoesNotContain(fixture.Case.Scenes[1].Title, json, StringComparison.Ordinal);
        Assert.DoesNotContain(fixture.Case.Puzzles[0].Solution, json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SpoilerGuardBlocksDirectCulpritLeakBeforeFinale()
    {
        var fixture = await CreateFixtureAsync();
        var culprit = fixture.Case.Suspects.Single(x => x.Id == fixture.Case.CulpritId);

        var protectedAnswer = MysterySpoilerGuard.ProtectAnswer(
            fixture.Case,
            fixture.State,
            $"Die Täterin ist {culprit.Name}.");

        Assert.DoesNotContain(culprit.Name, protectedAnswer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Auflösung", protectedAnswer, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GameStateSurvivesSerializationAndReload()
    {
        var fixture = await CreateFixtureAsync();
        fixture.State.Notes.Add("Die Uhr geht sieben Minuten vor.");
        fixture.State.StoryFlags.Add("custom-observation");

        var json = JsonSerializer.Serialize(fixture.State, MysteryJson.Options);
        var reloaded = MysterySessionMapper.Deserialize<MysteryGameState>(json);

        Assert.Equal(fixture.State.CurrentSceneIndex, reloaded.CurrentSceneIndex);
        Assert.Contains("clock", reloaded.FoundEvidenceIds);
        Assert.Contains("Die Uhr geht sieben Minuten vor.", reloaded.Notes);
        Assert.Contains("custom-observation", reloaded.StoryFlags);
    }

    [Fact]
    public async Task HintLevelsAreDistinctAndPersisted()
    {
        var fixture = await CreateFixtureAsync();
        var now = DateTimeOffset.UtcNow;

        var first = MysteryGameEngine.GetHint(fixture.Case, fixture.State, 1, now);
        var second = MysteryGameEngine.GetHint(fixture.Case, fixture.State, 2, now);
        var third = MysteryGameEngine.GetHint(fixture.Case, fixture.State, 3, now);

        Assert.Equal(3, fixture.State.UsedHints.Count);
        Assert.NotEqual(first, second);
        Assert.NotEqual(second, third);
        Assert.NotEqual(first, third);
        Assert.Equal(Enumerable.Range(1, 3), fixture.State.UsedHints.Select(x => x.Level));
    }

    [Fact]
    public async Task InvalidPuzzleAnswerDoesNotUnlockProgression()
    {
        var fixture = await CreateFixtureAsync();
        Assert.True(MysteryGameEngine.Advance(fixture.Case, fixture.State).IsSuccess);

        var result = MysteryGameEngine.SubmitPuzzle(
            fixture.Case,
            fixture.State,
            "definitiv-falsch");

        Assert.False(result.IsCorrect);
        Assert.Empty(fixture.State.SolvedPuzzleIds);
        Assert.False(MysteryGameEngine.CanAdvance(fixture.Case, fixture.State));
        Assert.Equal(1, fixture.State.InvalidPuzzleAttempts);
    }

    [Fact]
    public async Task SessionCanReachFinaleAndRevealSolutionOnlyAfterTheory()
    {
        var fixture = await CreateFixtureAsync();

        Assert.True(MysteryGameEngine.Advance(fixture.Case, fixture.State).IsSuccess);
        Assert.True(MysteryGameEngine.SubmitPuzzle(
            fixture.Case,
            fixture.State,
            fixture.Case.Puzzles[0].Solution).IsCorrect);
        Assert.True(MysteryGameEngine.Advance(fixture.Case, fixture.State).IsSuccess);
        Assert.True(MysteryGameEngine.Choose(
            fixture.Case,
            fixture.State,
            fixture.Case.Scenes[2].Choices[0].Id).IsSuccess);
        Assert.True(MysteryGameEngine.Advance(fixture.Case, fixture.State).IsSuccess);
        Assert.True(MysteryGameEngine.Advance(fixture.Case, fixture.State).IsSuccess);
        Assert.True(MysteryGameEngine.Advance(fixture.Case, fixture.State).IsSuccess);
        Assert.Equal(MysteryGameStatus.ReadyForFinale, fixture.State.Status);

        var completion = MysteryGameEngine.Complete(
            fixture.Case,
            fixture.State,
            new MysteryFinalTheory
            {
                CulpritId = fixture.Case.CulpritId,
                Motive = fixture.Case.Motive
            });
        fixture.Session.Status = fixture.State.Status;
        fixture.Session.GameStateJson = JsonSerializer.Serialize(fixture.State, MysteryJson.Options);
        var response = MysterySessionMapper.Map(fixture.Session);

        Assert.True(completion.IsSuccess);
        Assert.Equal(MysteryGameStatus.Completed, fixture.State.Status);
        Assert.NotNull(response.Finale);
        Assert.True(response.Finale.CorrectCulprit);
        Assert.Equal(fixture.Case.Motive, response.Finale.Motive);
    }

    private static async Task<MysteryFixture> CreateFixtureAsync()
    {
        var configuration = new MysteryGameConfiguration
        {
            Players = ["Damian", "Mitspieler"],
            DurationMinutes = 75,
            Difficulty = MysteryDifficulty.Medium,
            Genre = "Whodunit",
            Atmosphere = "Düster",
            Locations = [],
            AvailableItems = ["Papier", "Stift"]
        };
        var generated = await new LocalMysteryProvider().GenerateCaseAsync(
            configuration,
            CancellationToken.None);
        var mysteryCase = MysteryCaseGuard.ValidateAndNormalize(generated.Case, configuration);
        var state = MysteryGameEngine.CreateInitialState(mysteryCase);
        var session = new MysterySession
        {
            Id = Guid.NewGuid(),
            JoinCode = "ABC234",
            Title = mysteryCase.Title,
            Status = state.Status,
            GameMaster = generated.ProviderName,
            ConfigurationJson = JsonSerializer.Serialize(configuration, MysteryJson.Options),
            SecretCaseJson = JsonSerializer.Serialize(mysteryCase, MysteryJson.Options),
            GameStateJson = JsonSerializer.Serialize(state, MysteryJson.Options),
            Version = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        return new MysteryFixture(configuration, mysteryCase, state, session);
    }

    private sealed record MysteryFixture(
        MysteryGameConfiguration Configuration,
        MysteryCaseDefinition Case,
        MysteryGameState State,
        MysterySession Session);
}
