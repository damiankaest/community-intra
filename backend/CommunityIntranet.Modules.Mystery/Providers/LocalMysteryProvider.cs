using System.Security.Cryptography;
using CommunityIntranet.Modules.Mystery.Domain;

namespace CommunityIntranet.Modules.Mystery.Providers;

public sealed class LocalMysteryProvider : IMysteryLlmProvider
{
    private static readonly SuspectSeed[] SuspectSeeds =
    [
        new(
            "mara",
            "Mara Seidel",
            "Kuratorin",
            "Beherrscht jedes Detail der Sammlung und wirkt ungewöhnlich kontrolliert.",
            "eine heimliche Manipulation der Inventarlisten",
            "roter Restaurierungslack"),
        new(
            "leon",
            "Leon Voss",
            "Neffe des Opfers",
            "Hat finanzielle Sorgen und versucht, sie mit lautem Auftreten zu überspielen.",
            "den verdeckten Verkauf eines wertvollen Sammlungsstücks",
            "eine seltene blaue Mantelfaser"),
        new(
            "ines",
            "Dr. Ines Brandt",
            "Restauratorin",
            "Kennt die technischen Schwachstellen der Exponate und beobachtet sehr genau.",
            "eine nicht genehmigte Restaurierung mit schweren Folgeschäden",
            "silbernen Polierstaub")
    ];

    private static readonly VictimSeed[] VictimSeeds =
    [
        new("Emil Voss", "Sammler und Gastgeber"),
        new("Helena Falk", "Stifterin des privaten Archivs"),
        new("Konrad Winter", "Besitzer einer seltenen Musiksammlung")
    ];

    public Task<MysteryCaseGenerationResult> GenerateCaseAsync(
        MysteryGameConfiguration configuration,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var culpritIndex = RandomNumberGenerator.GetInt32(SuspectSeeds.Length);
        var culprit = SuspectSeeds[culpritIndex];
        var clearedSuspect = SuspectSeeds[(culpritIndex + 1) % SuspectSeeds.Length];
        var redHerringSuspect = SuspectSeeds[(culpritIndex + 2) % SuspectSeeds.Length];
        var victim = VictimSeeds[RandomNumberGenerator.GetInt32(VictimSeeds.Length)];
        var codeDigits = Enumerable.Range(0, 4)
            .Select(_ => RandomNumberGenerator.GetInt32(1, 10))
            .ToArray();
        var puzzleCode = string.Concat(codeDigits);
        var spacedPuzzleCode = string.Join(" ", codeDigits);
        var caseMarker = Convert.ToHexString(RandomNumberGenerator.GetBytes(3));

        var location = configuration.Locations.FirstOrDefault(x => x.AvailableFromProgress <= 0.75);
        var item = configuration.AvailableItems.FirstOrDefault();
        var locationNarrative = location is null
            ? "Der letzte verschlossene Bereich des Hauses wird gemeinsam untersucht."
            : $"Der Game Master schickt euch jetzt unerwartet zu: {location.Description}.";
        var taskPrompt = item is null
            ? "Ordnet die drei bisher gefundenen Zeitangaben auf einem Blatt Papier."
            : $"Nehmt den vorbereiteten Gegenstand „{item}“ und legt damit die drei Zeitangaben in die richtige Reihenfolge.";
        var suspectNames = string.Join(", ", SuspectSeeds.Select(x => x.Name));
        var motive = $"{culprit.Name} wollte verhindern, dass {victim.Name} {culprit.HiddenConflict} öffentlich macht.";

        var suspects = SuspectSeeds.Select(seed => new MysteryCharacterDefinition
        {
            Id = seed.Id,
            Name = seed.Name,
            Role = seed.Role,
            PublicDescription = seed.PublicDescription,
            Secret = seed.Id == culprit.Id
                ? $"Fürchtet die Aufdeckung von {seed.HiddenConflict} und hat die Tat vorbereitet."
                : seed.Id == clearedSuspect.Id
                    ? "Verschweigt ein privates Problem, wird später aber durch eine unabhängige Aufzeichnung entlastet."
                    : "Hat einen nachvollziehbaren Konflikt mit dem Opfer, aber keine Verbindung zur Tatspur."
        }).ToArray();

        var mysteryCase = new MysteryCaseDefinition
        {
            Title = $"Der letzte Takt – Fall {caseMarker}",
            Opening = $"Ein Gewitter trennt das Haus vom Rest der Welt. Als das Licht zurückkehrt, liegt {victim.Name} leblos neben einem alten Grammophon. Niemand hat das Gebäude verlassen.",
            Victim = $"{victim.Name}, {victim.Role}",
            CulpritId = culprit.Id,
            Motive = motive,
            Timeline = $"Zwischen 21:32 und 21:41 manipulierte {culprit.Name} Tatort und Standuhr. Das scheinbare Alibi beruhte auf einer Uhr, die sieben Minuten vorging.",
            Suspects = suspects,
            Evidence =
            [
                new MysteryEvidenceDefinition
                {
                    Id = "clock",
                    Title = "Die verstimmte Standuhr",
                    Description = "Die Standuhr geht exakt sieben Minuten vor. Am Stellrad befindet sich eine frische, zunächst nicht zuzuordnende Spur.",
                    IsRedHerring = false
                },
                new MysteryEvidenceDefinition
                {
                    Id = "ledger",
                    Title = "Die Zeichenfolge im Sammlungsbuch",
                    Description = $"Vier kaum sichtbare Markierungen tragen von oben nach unten die Ziffern {spacedPuzzleCode}.",
                    IsRedHerring = false
                },
                new MysteryEvidenceDefinition
                {
                    Id = "false-motive",
                    Title = $"Der Konflikt von {redHerringSuspect.Name}",
                    Description = $"Ein Dokument liefert {redHerringSuspect.Name} ein mögliches Motiv, beweist aber keine Anwesenheit am Tatort.",
                    IsRedHerring = true
                },
                new MysteryEvidenceDefinition
                {
                    Id = "trace",
                    Title = "Die Spur am Mechanismus",
                    Description = $"Die Laborprüfung ordnet die Spur eindeutig zu: {culprit.TraceDescription}. Dasselbe Material findet sich an einem persönlichen Gegenstand von {culprit.Name}.",
                    IsRedHerring = false
                },
                new MysteryEvidenceDefinition
                {
                    Id = "recording",
                    Title = "Die unabhängige Aufzeichnung",
                    Description = $"Eine durchgehende Aufnahme belegt, dass {clearedSuspect.Name} zwischen 21:29 und 21:38 nicht am Tatort war.",
                    IsRedHerring = false
                }
            ],
            Puzzles =
            [
                new MysteryPuzzleDefinition
                {
                    Id = "ledger-code",
                    Prompt = "Welche vierstellige Zahl ergibt sich aus den markierten Ziffern im Sammlungsbuch?",
                    InputType = "code",
                    Solution = puzzleCode,
                    AcceptedAnswers = [puzzleCode, spacedPuzzleCode],
                    Hints =
                    [
                        "Nicht die Preise, sondern die kleinen Markierungen sind wichtig.",
                        "Lest die vier markierten Ziffern von oben nach unten.",
                        $"Der gesuchte Code lautet {puzzleCode}."
                    ]
                }
            ],
            Scenes =
            [
                new MysterySceneDefinition
                {
                    Id = "salon",
                    Chapter = 1,
                    Kind = MysterySceneKind.Story,
                    Title = "Stille nach dem Donner",
                    Narrative = $"{suspectNames} stehen im Salon. Die Standuhr zeigt 21:48, obwohl ein Handy 21:41 anzeigt. Neben dem Opfer dreht sich eine Platte ohne Ton weiter.",
                    Prompt = "Lest die Szene laut vor und sammelt erste Verdachtsmomente.",
                    EvidenceIds = ["clock"],
                    CharacterIds = SuspectSeeds.Select(x => x.Id).ToArray(),
                    StoryFlags = ["clock-discrepancy-known"],
                    Hints =
                    [
                        "Vergleicht alle sichtbaren Zeitangaben.",
                        "Die Standuhr und das Handy können nicht beide stimmen.",
                        "Die Standuhr geht sieben Minuten vor – fragt euch, wem das ein Alibi verschafft."
                    ]
                },
                new MysterySceneDefinition
                {
                    Id = "archive",
                    Chapter = 1,
                    Kind = MysterySceneKind.Puzzle,
                    Title = "Das verschlossene Archiv",
                    Narrative = "Im Sammlungsbuch sind vier Stellen mit kaum sichtbaren Bleistiftpunkten markiert. Ein Zahlenschloss schützt die letzte Notiz des Opfers.",
                    EvidenceIds = ["ledger"],
                    CharacterIds = [],
                    PuzzleId = "ledger-code",
                    Hints =
                    [
                        "Sucht nach wiederholten Markierungen.",
                        "Die markierten Ziffern bilden den Code.",
                        $"Gebt {puzzleCode} ein."
                    ]
                },
                new MysterySceneDefinition
                {
                    Id = "interviews",
                    Chapter = 2,
                    Kind = MysterySceneKind.Decision,
                    Title = "Drei Aussagen, ein Widerspruch",
                    Narrative = $"{redHerringSuspect.Name} legt ein belastendes Dokument vor. {culprit.Name} bestreitet jede Berührung der Standuhr. {clearedSuspect.Name} verweist auf eine überprüfbare Aufzeichnung.",
                    Prompt = "Welche Spur prüft ihr zuerst? Die Entscheidung wird gespeichert, verrät aber nicht automatisch die richtige Richtung.",
                    EvidenceIds = ["false-motive"],
                    CharacterIds = [],
                    Choices =
                    [
                        new MysteryChoiceDefinition
                        {
                            Id = "check-recording",
                            Label = "Die Aufzeichnung prüfen",
                            Consequence = "Der Zeitstempel wird später besonders genau geprüft.",
                            StoryFlags = ["prioritized-recording"]
                        },
                        new MysteryChoiceDefinition
                        {
                            Id = "check-trace",
                            Label = "Die Materialspur untersuchen",
                            Consequence = "Die Spur am Mechanismus rückt in den Fokus.",
                            StoryFlags = ["prioritized-trace"]
                        },
                        new MysteryChoiceDefinition
                        {
                            Id = "check-document",
                            Label = "Das belastende Dokument prüfen",
                            Consequence = "Motiv und Tatgelegenheit werden getrennt bewertet.",
                            StoryFlags = ["prioritized-document"]
                        }
                    ],
                    Hints =
                    [
                        "Eine überprüfbare Aussage ist wertvoller als ein Bauchgefühl.",
                        "Sowohl Aufzeichnung als auch Materialspur lassen sich objektiv prüfen.",
                        "Keine Wahl blockiert euch; entscheidet, welche Spur ihr zuerst absichert."
                    ]
                },
                new MysterySceneDefinition
                {
                    Id = "real-task",
                    Chapter = 2,
                    Kind = location is null ? MysterySceneKind.RealTask : MysterySceneKind.LocationChange,
                    Title = "Sieben fehlende Minuten",
                    Narrative = locationNarrative,
                    Prompt = taskPrompt,
                    EvidenceIds = ["trace"],
                    CharacterIds = [],
                    LocationId = location?.Id,
                    StoryFlags = ["timeline-reconstructed"],
                    Hints =
                    [
                        "Ordnet zuerst nur sichere Zeitangaben.",
                        "Zieht von der Standuhr sieben Minuten ab.",
                        "Die Tat geschah, bevor die Gruppe laut falscher Uhr wieder im Salon war."
                    ]
                },
                new MysterySceneDefinition
                {
                    Id = "last-proof",
                    Chapter = 3,
                    Kind = MysterySceneKind.Evidence,
                    Title = "Der letzte Takt",
                    Narrative = $"Die Aufzeichnung entlastet {clearedSuspect.Name}. Die Materialanalyse verbindet Standuhr und Grammophon mit einem Gegenstand von {culprit.Name}. Nun müsst ihr Täter, Motiv und Ablauf benennen.",
                    EvidenceIds = ["recording"],
                    CharacterIds = [],
                    StoryFlags = ["finale-unlocked"],
                    Hints =
                    [
                        "Trennt Motive von überprüften Tatgelegenheiten.",
                        "Eine Person ist durch eine unabhängige Aufzeichnung entlastet.",
                        $"Verbindet die Zeitmanipulation und {culprit.TraceDescription} mit derselben Person."
                    ]
                }
            ],
            Resolution = $"{culprit.Name} wollte {culprit.HiddenConflict} verbergen und manipulierte während des Stromausfalls Grammophon und Standuhr. Die sieben Minuten Zeitabweichung, {culprit.TraceDescription} und die unabhängige Aufzeichnung schließen die übrigen Verdächtigen aus."
        };

        return Task.FromResult(new MysteryCaseGenerationResult(
            mysteryCase,
            "Lokaler prozeduraler Game Master",
            "Kein KI-API-Key konfiguriert. Der geheime Fall wurde beim Start serverseitig aus zufälligen Bausteinen erzeugt; für vollständig frei erzählte Fälle den serverseitigen KI-Key setzen."));
    }

    public Task<string> AnswerPlayerQuestionAsync(
        MysteryCaseDefinition mysteryCase,
        MysteryGameState state,
        string question,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var evidence = mysteryCase.Evidence
            .Where(x => state.FoundEvidenceIds.Contains(x.Id))
            .Select(x => x.Title)
            .ToArray();
        var answer = evidence.Length == 0
            ? "Noch ist nichts sicher. Trennt Beobachtungen von Vermutungen und untersucht zuerst die aktuelle Szene."
            : $"Ich kann eure Theorie nicht bestätigen. Prüft die bereits sichtbaren Spuren gemeinsam: {string.Join(", ", evidence)}.";
        return Task.FromResult(answer);
    }

    private sealed record SuspectSeed(
        string Id,
        string Name,
        string Role,
        string PublicDescription,
        string HiddenConflict,
        string TraceDescription);

    private sealed record VictimSeed(string Name, string Role);
}
