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
            "Spricht leise, merkt sich jedes Detail und trägt einen abgegriffenen roten Bleistift bei sich.",
            "eine heimliche Manipulation der Inventarlisten",
            "roter Restaurierungslack",
            "behauptet, während des Stromausfalls allein im Archiv gewesen zu sein"),
        new(
            "leon",
            "Leon Voss",
            "Neffe des Opfers",
            "Wirkt fahrig, hält sein Telefon fest umklammert und weicht Fragen nach Geld aus.",
            "den verdeckten Verkauf eines wertvollen Sammlungsstücks",
            "eine seltene blaue Mantelfaser",
            "will zur Tatzeit einen langen Videoanruf geführt haben"),
        new(
            "ines",
            "Dr. Ines Brandt",
            "Restauratorin",
            "Beobachtet erst den Tatort und dann die Menschen; an ihren Händen haftet silbriger Staub.",
            "eine nicht genehmigte Restaurierung mit schweren Folgeschäden",
            "silbernen Polierstaub",
            "sagt, im Werkraum ein beschädigtes Exponat gesichert zu haben")
    ];

    private static readonly VictimSeed[] VictimSeeds =
    [
        new("Emil Voss", "Sammler und Gastgeber"),
        new("Helena Falk", "Stifterin des privaten Archivs"),
        new("Konrad Winter", "Besitzer einer seltenen Musiksammlung")
    ];

    private static readonly string[] OrdinalWords =
    [
        "nullte", "erste", "zweite", "dritte", "vierte",
        "fünfte", "sechste", "siebte", "achte", "neunte"
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
        var introductionOffset = RandomNumberGenerator.GetInt32(SuspectSeeds.Length);
        var introducedFirst = SuspectSeeds[introductionOffset];
        var introducedSecond = SuspectSeeds[(introductionOffset + 1) % SuspectSeeds.Length];
        var introducedThird = SuspectSeeds[(introductionOffset + 2) % SuspectSeeds.Length];
        var victim = VictimSeeds[RandomNumberGenerator.GetInt32(VictimSeeds.Length)];
        var caseMarker = Convert.ToHexString(RandomNumberGenerator.GetBytes(3));

        var codeDigits = Enumerable.Range(0, 4)
            .Select(_ => RandomNumberGenerator.GetInt32(1, 10))
            .ToArray();
        var catalogueCode = string.Concat(codeDigits);
        var catalogueEntries = string.Join(
            "\n",
            codeDigits.Select((digit, index) =>
                $"{(char)('A' + index)} · Erwerb {1886 + index * 29} · „Die {OrdinalWords[digit]} Variation“"));

        var clockOffset = RandomNumberGenerator.GetInt32(5, 10);
        var mechanismLead = RandomNumberGenerator.GetInt32(3, 6);
        const int displayMinute = 41;
        var restoredMinute = displayMinute - clockOffset;
        var manipulationMinute = restoredMinute - mechanismLead;
        var timelineCode = $"21{manipulationMinute:00}";
        var encodedRoom = EncodeCaesar("SALON", clockOffset);

        var location = configuration.Locations.FirstOrDefault(x => x.AvailableFromProgress <= 0.75);
        var item = configuration.AvailableItems.FirstOrDefault();
        var locationNarrative = location is null
            ? "Hinter einer schmalen Holztür liegt ein kaum benutzter Nebenraum. Der Geruch nach kaltem Wachs ist hier stärker als im Salon. Auf dem Boden führt eine einzelne Schleifspur zu einem abgedeckten Tisch."
            : $"Unter einer Tür liegt ein Umschlag mit eurem Spielcode. Darin steht nur: „Geht gemeinsam zu {location.Description}. Öffnet dort erst die nächste Anweisung.“";
        var taskPrompt = item is null
            ? "Legt drei Zettel für sichere Uhrzeiten aus. Ordnet nur Fakten ein; Vermutungen bleiben daneben liegen."
            : $"Nehmt „{item}“ und markiert damit auf drei Zetteln: sichere Zeit, behauptete Zeit und noch ungeklärte Zeit.";
        var motive = $"{culprit.Name} wollte verhindern, dass {victim.Name} {culprit.HiddenConflict} öffentlich macht.";

        var suspects = SuspectSeeds.Select(seed => new MysteryCharacterDefinition
        {
            Id = seed.Id,
            Name = seed.Name,
            Role = seed.Role,
            PublicDescription = seed.PublicDescription,
            Secret = seed.Id == culprit.Id
                ? $"Fürchtet die Aufdeckung von {seed.HiddenConflict}, hat Tatort und Uhr manipuliert und stützt das Alibi auf die falsche Zeit."
                : seed.Id == clearedSuspect.Id
                    ? "Verschweigt ein privates Problem, wird später aber durch eine unabhängige Aufzeichnung entlastet."
                    : "Hat einen ernsten Konflikt mit dem Opfer, aber keine Verbindung zur entscheidenden Tatspur."
        }).ToArray();

        var evidence = new List<MysteryEvidenceDefinition>
        {
            new()
            {
                Id = "clock",
                Title = "Die stehen gebliebene Uhr",
                Description = $"Die Standuhr blieb beim Wiederkehren des Lichts auf 21:41 stehen. Ein Abgleich mit zwei Telefonen zeigt später: Sie ging bereits vorher genau {clockOffset} Minuten vor.",
                IsRedHerring = false
            },
            new()
            {
                Id = "ledger",
                Title = "Vier markierte Katalogeinträge",
                Description = "Im Katalog tragen vier Einträge die Buchstaben A bis D. Jahre und Werktitel wurden ins Fallarchiv übertragen; daneben steht: „Das Alter täuscht.“",
                IsRedHerring = false
            },
            new()
            {
                Id = "false-motive",
                Title = $"Der verschwiegene Konflikt von {redHerringSuspect.Name}",
                Description = $"Ein zerrissener Vertragsentwurf zeigt, dass {redHerringSuspect.Name} am Abend heftig mit dem Opfer gestritten hatte. Er erklärt ein Motiv, aber noch keine Tatgelegenheit.",
                IsRedHerring = true
            },
            new()
            {
                Id = "trace-sample",
                Title = "Die Spur unter dem Plattenarm",
                Description = "Unter dem Metall sitzt ein winziger fremder Rückstand. Farbe und Material sind ohne Vergleichsprobe noch nicht zuzuordnen.",
                IsRedHerring = false
            },
            new()
            {
                Id = "mechanism",
                Title = "Das blockierte Federwerk",
                Description = $"Die Spannung des Federwerks reicht für exakt {mechanismLead} Minuten. Die Blockade muss daher genau so lange vor dem Wiederkehren des Lichts eingesetzt worden sein.",
                IsRedHerring = false
            },
            new()
            {
                Id = "recording",
                Title = "Die unabhängige Aufzeichnung",
                Description = $"Eine lückenlose Aufnahme belegt, dass {clearedSuspect.Name} im entscheidenden Zeitfenster nicht am Grammophon gewesen sein kann.",
                IsRedHerring = false
            },
            new()
            {
                Id = "trace-result",
                Title = "Der Materialvergleich",
                Description = $"Der Rückstand von Uhr und Plattenarm ist identisch mit {culprit.TraceDescription} an einem persönlichen Arbeitsgegenstand von {culprit.Name}.",
                IsRedHerring = false
            }
        };

        if (configuration.Difficulty == MysteryDifficulty.Hard)
        {
            evidence.Add(new MysteryEvidenceDefinition
            {
                Id = "cipher-note",
                Title = "Die verschobene Ortsangabe",
                Description = $"Auf der Rückseite einer Quittung steht nur „{encodedRoom}“. Darunter: „Die falsche Uhr sagt, wie weit ich jeden Buchstaben verschoben habe.“",
                IsRedHerring = false
            });
        }

        var puzzles = new List<MysteryPuzzleDefinition>
        {
            new()
            {
                Id = "catalogue-code",
                Prompt = $"Auf dem Tisch liegen vier abgeschriebene Einträge:\n\n{catalogueEntries}\n\nRandnotiz: „Das Alter täuscht. Lies A bis D und nimm den Rang im Titel.“ Welcher vierstellige Code öffnet die Kassette?",
                InputType = "code",
                Solution = catalogueCode,
                AcceptedAnswers = [catalogueCode, string.Join(" ", codeDigits)],
                Hints =
                [
                    "Die Erwerbsjahre sind absichtlich auffällig, aber für den Code nicht nötig.",
                    "Übersetzt bei A bis D jeweils die Ordnungszahl im Werktitel in eine Ziffer.",
                    $"Die vier Titel stehen für {string.Join(" – ", codeDigits)}."
                ]
            }
        };

        if (configuration.Difficulty is MysteryDifficulty.Medium or MysteryDifficulty.Hard)
        {
            puzzles.Add(new MysteryPuzzleDefinition
            {
                Id = "timeline-code",
                Prompt = $"Rekonstruiert den Zeitpunkt der Manipulation. Beim Wiederkehren des Lichts zeigte die Standuhr 21:41, ging aber {clockOffset} Minuten vor. Das blockierte Federwerk war zu diesem Zeitpunkt bereits genau {mechanismLead} Minuten gelaufen. Zu welcher echten Uhrzeit wurde es blockiert? Gebt vier Ziffern im Format HHMM ein.",
                InputType = "code",
                Solution = timelineCode,
                AcceptedAnswers = [timelineCode, $"21:{manipulationMinute:00}"],
                Hints =
                [
                    "Korrigiert zuerst die falsche Standuhr. Erst danach betrachtet ihr das Federwerk.",
                    $"21:41 minus {clockOffset} Minuten ergibt den echten Zeitpunkt, an dem das Licht zurückkam. Davon fehlen noch {mechanismLead} Minuten.",
                    $"Die Manipulation geschah um 21:{manipulationMinute:00}."
                ]
            });
        }

        if (configuration.Difficulty == MysteryDifficulty.Hard)
        {
            puzzles.Add(new MysteryPuzzleDefinition
            {
                Id = "room-cipher",
                Prompt = $"Entschlüsselt die Ortsangabe „{encodedRoom}“. Jeder Buchstabe wurde im Alphabet um genau so viele Stellen nach vorn verschoben, wie die Standuhr vorging. Welches Wort war ursprünglich notiert?",
                InputType = "text",
                Solution = "SALON",
                AcceptedAnswers = ["SALON", "DER SALON"],
                Hints =
                [
                    "Verwendet die bekannte Abweichung der Standuhr als Verschiebung.",
                    $"Geht bei jedem Buchstaben {clockOffset} Stellen im Alphabet zurück.",
                    "Das gesuchte Wort bezeichnet den Raum, in dem der Abend begann."
                ]
            });
        }

        var scenes = new List<MysterySceneDefinition>
        {
            new()
            {
                Id = "discovery",
                Chapter = 1,
                Kind = MysterySceneKind.Story,
                Title = "Als die Musik verstummt",
                Narrative = $"Zuerst hört ihr nur die Nadel, die immer wieder über dieselbe leere Rille springt. Dann zündet {introducedFirst.Name} eine einzelne Kerze an. {introducedFirst.Role} – mehr wisst ihr in diesem Moment nicht. {introducedFirst.Name} kniet neben dem Grammophon, ohne es zu berühren, und sagt: „Das Licht war höchstens eine Minute aus.“\n\nDraußen drückt der Sturm gegen die Scheiben. Im Raum riecht es nach Wachs und nassem Holz. Niemand ruft sofort die Polizei; das Festnetz ist tot, die Zufahrt überflutet. Ihr habt Zeit, aber ihr seid auf euch gestellt.",
                Prompt = "Lest nur diese Szene laut. Beschreibt anschließend reihum genau eine Beobachtung – noch keine Tätertheorie.",
                EvidenceIds = [],
                CharacterIds = [introducedFirst.Id],
                StoryFlags = ["body-discovered"],
                Hints =
                [
                    "Achtet zunächst auf Geräusche, Gerüche und die erste Zeitangabe.",
                    "Die Aussage über die Dauer des Stromausfalls ist noch nicht überprüft.",
                    "Merkt euch, wer als erste Person am Grammophon war, ohne daraus schon Schuld abzuleiten."
                ]
            },
            new()
            {
                Id = "clock-room",
                Chapter = 1,
                Kind = MysterySceneKind.Dialogue,
                Title = "Sieben Schritte bis zur Tür",
                Narrative = $"Während ihr den Raum sichert, kommt {introducedSecond.Name} aus dem dunklen Flur. {introducedSecond.Role}. {introducedSecond.Name} bleibt an der Tür stehen und {introducedSecond.Alibi}. Erst jetzt fällt auf, dass die große Standuhr nicht tickt. Ihr Zifferblatt zeigt 21:41, doch auf den Telefonen ist es früher.\n\nAuf die Frage nach dem Opfer folgt keine direkte Antwort. Stattdessen deutet {introducedSecond.Name} auf die Uhr: „Wenn die stimmt, kann meine Aussage nicht stimmen.“",
                Prompt = "Welche Aussage ist hier überprüfbar? Sichert erst die Uhrzeit und notiert dann offene Fragen.",
                EvidenceIds = ["clock"],
                CharacterIds = [introducedSecond.Id],
                StoryFlags = ["clock-discrepancy-known"],
                Hints =
                [
                    "Vergleicht Uhr, Telefone und die behauptete Dauer des Ausfalls.",
                    "Eine falsche Uhr macht nicht automatisch eine Person schuldig, aber sie verändert jedes Alibi.",
                    $"Die Standuhr geht {clockOffset} Minuten vor. Rechnet Aussagen später immer in echte Zeit um."
                ]
            },
            new()
            {
                Id = "catalogue",
                Chapter = 1,
                Kind = MysterySceneKind.Puzzle,
                Title = "Die Kassette ohne Schlüssel",
                Narrative = "In einer Schublade liegt eine flache Messingkassette. Das Schloss besitzt vier Zahlenräder. Daneben liegt kein fertiger Code, sondern ein Blatt mit vier Katalogeinträgen und einer hastig geschriebenen Randnotiz. Die Schrift stammt vom Opfer. Offenbar wollte es, dass jemand die Kassette noch an diesem Abend öffnet.",
                Prompt = "Löst das Rätsel gemeinsam. Die vollständige Aufgabenstellung bleibt anschließend im Reiter „Rätsel“ des Fallarchivs.",
                EvidenceIds = ["ledger"],
                CharacterIds = [],
                PuzzleId = "catalogue-code",
                Hints =
                [
                    "Trennt dekorative Angaben von Informationen, die die Randnotiz ausdrücklich nennt.",
                    "A bis D geben die Reihenfolge vor; die Werktitel liefern die Ziffern.",
                    $"Der Code lautet {catalogueCode}."
                ]
            },
            new()
            {
                Id = "third-arrival",
                Chapter = 2,
                Kind = MysterySceneKind.Dialogue,
                Title = "Was im Streit gesagt wurde",
                Narrative = $"Die Kassette enthält keinen Namen, nur einen Vertragsentwurf mit einer herausgerissenen Unterschrift. In diesem Moment betritt {introducedThird.Name} den Raum. {introducedThird.Role}. Ohne den Vertrag ganz zu lesen, erkennt {introducedThird.Name} das Papier und berichtet von einem Streit kurz vor dem Stromausfall.\n\nDer Streit betraf {redHerringSuspect.Name}. Das ist belastend – aber {introducedThird.Name} bittet euch, Motiv und Gelegenheit nicht zu verwechseln.",
                Prompt = "Nehmt euch zwei Minuten: Was beweist der Vertrag wirklich, und was vermutet ihr nur?",
                EvidenceIds = ["false-motive"],
                CharacterIds = [introducedThird.Id],
                StoryFlags = ["all-suspects-known"],
                Hints =
                [
                    "Ein Streit erklärt Gefühle, aber noch keinen Tatablauf.",
                    "Prüft, wer durch den Vertrag belastet wird und wer darüber berichtet.",
                    "Die Spur ist eine falsche Fährte, solange ihr sie nicht mit Zeit und Tatort verbinden könnt."
                ]
            },
            new()
            {
                Id = "choose-investigation",
                Chapter = 2,
                Kind = MysterySceneKind.Decision,
                Title = "Eine Spur, drei Richtungen",
                Narrative = "Unter dem Plattenarm entdeckt ihr einen winzigen Rückstand. Gleichzeitig wartet der Vertragsentwurf auf Prüfung, und eines der behaupteten Alibis ließe sich über eine Aufzeichnung kontrollieren. Ihr könnt alles untersuchen – aber die Reihenfolge entscheidet, welche Vermutung den nächsten Teil des Abends prägt.",
                Prompt = "Entscheidet gemeinsam, welcher überprüfbaren Spur ihr zuerst folgt. Keine Wahl sperrt den Fall dauerhaft.",
                EvidenceIds = ["trace-sample"],
                CharacterIds = [],
                Choices =
                [
                    new MysteryChoiceDefinition
                    {
                        Id = "check-recording",
                        Label = "Die unabhängige Aufzeichnung suchen",
                        Consequence = "Ein Alibi wird zuerst technisch überprüft.",
                        StoryFlags = ["prioritized-recording"]
                    },
                    new MysteryChoiceDefinition
                    {
                        Id = "check-trace",
                        Label = "Den Rückstand am Mechanismus vergleichen",
                        Consequence = "Die materielle Tatspur rückt zuerst in den Fokus.",
                        StoryFlags = ["prioritized-trace"]
                    },
                    new MysteryChoiceDefinition
                    {
                        Id = "check-document",
                        Label = "Herkunft und Zweck des Vertrags klären",
                        Consequence = "Das stärkste scheinbare Motiv wird zuerst geprüft.",
                        StoryFlags = ["prioritized-document"]
                    }
                ],
                Hints =
                [
                    "Eine überprüfbare Spur ist meist wertvoller als ein Eindruck.",
                    "Aufzeichnung und Materialprobe können Aussagen objektiv bestätigen oder widerlegen.",
                    "Keine Wahl ist falsch; sie verändert nur euren Blick auf die folgenden Beweise."
                ]
            },
            new()
            {
                Id = "hidden-room",
                Chapter = 2,
                Kind = location is null ? MysterySceneKind.RealTask : MysterySceneKind.LocationChange,
                Title = "Der Raum hinter der Tür",
                Narrative = locationNarrative,
                Prompt = taskPrompt,
                EvidenceIds = ["mechanism"],
                CharacterIds = [],
                LocationId = location?.Id,
                StoryFlags = ["mechanism-understood"],
                Hints =
                [
                    "Ordnet nur Zeiten ein, die durch Gegenstände oder Aufzeichnungen belegt sind.",
                    "Die Standuhrzeit muss korrigiert werden; das Federwerk liefert ein weiteres Zeitintervall.",
                    $"Das Federwerk lief {mechanismLead} Minuten. Diese Zahl braucht ihr für die Rekonstruktion."
                ]
            }
        };

        if (configuration.Difficulty is MysteryDifficulty.Medium or MysteryDifficulty.Hard)
        {
            scenes.Add(new MysterySceneDefinition
            {
                Id = "timeline",
                Chapter = 3,
                Kind = MysterySceneKind.Puzzle,
                Title = "Die Zeit, die niemand genannt hat",
                Narrative = "Jetzt liegen erstmals zwei unabhängige Zeitspuren nebeneinander: die falsch gehende Standuhr und die Restspannung des Grammophons. Keine davon nennt den Tatzeitpunkt direkt. Zusammen begrenzen sie jedoch exakt den Moment, in dem jemand den Mechanismus vorbereitet haben muss.",
                Prompt = "Rechnet in zwei Schritten. Schreibt beide Zwischenergebnisse auf, bevor ihr den Code eingebt.",
                EvidenceIds = [],
                CharacterIds = [],
                PuzzleId = "timeline-code",
                Hints =
                [
                    "Korrigiert zuerst die Uhr und zieht erst danach die Laufzeit des Federwerks ab.",
                    $"Aus 21:41 werden nach der Uhrkorrektur {clockOffset} Minuten weniger. Danach zieht ihr weitere {mechanismLead} Minuten ab.",
                    $"Gesucht ist 21:{manipulationMinute:00}, also {timelineCode}."
                ]
            });
        }

        if (configuration.Difficulty == MysteryDifficulty.Hard)
        {
            scenes.Add(new MysterySceneDefinition
            {
                Id = "cipher",
                Chapter = 3,
                Kind = MysterySceneKind.Puzzle,
                Title = "Fünf verschobene Buchstaben",
                Narrative = "Die rekonstruierte Uhrzeit führt zu einer gefalteten Quittung. Darauf steht ein scheinbar sinnloses Wort. Der Verfasser hat dieselbe falsche Zeit benutzt, die bereits mehrere Aussagen verzerrt hat – diesmal nicht als Alibi, sondern als Schlüssel.",
                Prompt = "Verwendet die bekannte Abweichung der Standuhr als Caesar-Verschiebung und entschlüsselt den Fundort.",
                EvidenceIds = ["cipher-note"],
                CharacterIds = [],
                PuzzleId = "room-cipher",
                Hints =
                [
                    "Die Uhrabweichung ist zugleich die Anzahl der Buchstabenstellen.",
                    $"Verschiebt jeden Buchstaben von {encodedRoom} um {clockOffset} Stellen zurück.",
                    "Das Lösungswort ist SALON."
                ]
            });
        }

        scenes.Add(new MysterySceneDefinition
        {
            Id = "last-proof",
            Chapter = 3,
            Kind = MysterySceneKind.Evidence,
            Title = "Was nach dem Abzug übrig bleibt",
            Narrative = $"Die Aufzeichnung entlastet {clearedSuspect.Name} für das entscheidende Zeitfenster. Der Materialvergleich ist ebenso eindeutig: Derselbe Rückstand verbindet Standuhr, Plattenarm und einen persönlichen Arbeitsgegenstand von {culprit.Name}.\n\nNoch sagt der Game Master nicht, ob eure Schlussfolgerung stimmt. Legt alle Spuren nebeneinander: Wer hatte ein Motiv? Wer konnte zur rekonstruierten Zeit handeln? Und welche scheinbar starke Spur führte nur in die Irre?",
            Prompt = "Öffnet das Fallarchiv. Formuliert gemeinsam Täter, Motiv und Ablauf, bevor ihr zur finalen Theorie weitergeht.",
            EvidenceIds = ["recording", "trace-result"],
            CharacterIds = [],
            StoryFlags = ["finale-unlocked"],
            Hints =
            [
                "Trennt Motiv, Gelegenheit und materielle Verbindung voneinander.",
                $"{clearedSuspect.Name} ist technisch entlastet; der Vertragskonflikt allein reicht ebenfalls nicht.",
                $"Zeitfenster und Materialspur führen beide zu {culprit.Name}."
            ]
        });

        var mysteryCase = new MysteryCaseDefinition
        {
            Title = $"Der letzte Takt – Fall {caseMarker}",
            Opening = $"Der Abend beginnt nicht mit einem Schrei. {victim.Name}, {victim.Role}, hat euch eingeladen, weil um Mitternacht „eine Wahrheit aus dem Archiv“ öffentlich werden soll. Kurz nach dem ersten Donner fällt das Licht aus. Als eine Kerze brennt, liegt {victim.Name} reglos neben dem Grammophon. Die Haustür ist von innen verriegelt; die Zufahrt steht unter Wasser. Was geschehen ist, muss sich in diesem Haus erklären lassen.",
            Victim = $"{victim.Name}, {victim.Role}",
            CulpritId = culprit.Id,
            Motive = motive,
            Timeline = $"{culprit.Name} blockierte das Grammophon um 21:{manipulationMinute:00}, nutzte den Stromausfall für die Tat und stützte das Alibi auf die um {clockOffset} Minuten vorgehende Standuhr. Als das Licht um 21:{restoredMinute:00} zurückkehrte, zeigte sie 21:41.",
            Suspects = suspects,
            Evidence = evidence.ToArray(),
            Puzzles = puzzles.ToArray(),
            Scenes = scenes.ToArray(),
            Resolution = $"{culprit.Name} wollte {culprit.HiddenConflict} verbergen. Die korrigierte Uhrzeit widerlegt das behauptete Alibi; die Restspannung des Federwerks bestimmt den Vorbereitungszeitpunkt. Schließlich verbindet {culprit.TraceDescription} sowohl Uhr als auch Grammophon mit {culprit.Name}. Die Aufzeichnung entlastet {clearedSuspect.Name}, während der Vertrag gegen {redHerringSuspect.Name} nur ein Motiv ohne Tatgelegenheit lieferte."
        };

        return Task.FromResult(new MysteryCaseGenerationResult(
            mysteryCase,
            "Lokaler prozeduraler Game Master",
            "Kein KI-API-Key konfiguriert. Dieser Fall wurde serverseitig prozedural erzeugt. Mit einem serverseitigen KI-Key entstehen zusätzlich vollständig frei geschriebene Fälle."));
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
            ? "Noch ist nichts sicher. Beschreibt zuerst, was ihr wirklich gesehen oder gehört habt, und trennt es von eurer ersten Vermutung."
            : $"Ich bestätige noch keine Theorie. Für diese Frage dürft ihr derzeit nur mit diesen gesicherten Spuren arbeiten: {string.Join(", ", evidence)}.";
        return Task.FromResult(answer);
    }

    private static string EncodeCaesar(string value, int shift) => new(
        value.Select(character => (char)('A' + (character - 'A' + shift) % 26)).ToArray());

    private sealed record SuspectSeed(
        string Id,
        string Name,
        string Role,
        string PublicDescription,
        string HiddenConflict,
        string TraceDescription,
        string Alibi);

    private sealed record VictimSeed(string Name, string Role);
}
