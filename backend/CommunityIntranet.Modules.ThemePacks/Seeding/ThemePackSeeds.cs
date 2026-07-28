using CommunityIntranet.Modules.ThemePacks.Configuration;

namespace CommunityIntranet.Modules.ThemePacks.Seeding;

public static class ThemePackSeeds
{
    public const string GenericCorporateKey = "generic-corporate";
    public const string SatisfactoryFicsitKey = "satisfactory-ficsit";

    public static IReadOnlyList<ThemePackConfiguration> All { get; } =
        [CreateGenericCorporate(), CreateSatisfactoryFicsit()];

    private static ThemePackConfiguration CreateGenericCorporate() =>
        new(
            GenericCorporateKey,
            "Community Corporate",
            "Ein klares, freundliches Intranet für Vereine, Freundesgruppen und Communities.",
            "1.0.0",
            "Community Intranet",
            new ThemeVisuals(
                "#38BDF8",
                "#1E293B",
                "#818CF8",
                "#0F172A",
                "#172033",
                "#F8FAFC",
                "#94A3B8",
                "#FB7185",
                "#FBBF24",
                "#34D399",
                "building-2",
                "clean-corporate"),
            new ThemeTerminology(
                "Organisation",
                "Mitglied",
                "Mitglieder",
                "Bereich",
                "Projekt",
                "Aufgabe",
                "Meldung",
                "Auszeichnung",
                "Aktivitäten"),
            [
                "Community Lead",
                "Organisationsprofi",
                "Koordinationsleitung",
                "Mitglied mit wichtigem Klemmbrett"
            ],
            [
                new SuggestedDepartment("Organisation", "clipboard-list"),
                new SuggestedDepartment("Projekte", "folder-kanban"),
                new SuggestedDepartment("Community", "users")
            ],
            [
                "Technisches Problem",
                "Organisatorische Rückfrage",
                "Ungeklärter Vorgang"
            ],
            [
                new AwardTemplate(
                    "Mitglied der Woche",
                    "Für besondere Verdienste im Bereich {reason}."),
                new AwardTemplate(
                    "Goldenes Klemmbrett",
                    "Für eine außergewöhnlich gründliche Dokumentation.")
            ],
            [
                "Alle Systeme arbeiten im erwartbaren Rahmen.",
                "Die Community ist erstaunlich gut organisiert."
            ],
            new ThemeMessages(
                "Willkommen in eurer Organisation. Gute Zusammenarbeit darf sogar Spaß machen.",
                "Noch keine Projekte vorhanden. Zeit für den ersten guten Plan.",
                "Keine offenen Aufgaben. Dieser Zustand sollte dokumentiert werden.",
                "Keine Meldungen vorhanden. So darf es gerne bleiben.",
                "Noch keine Aktivitäten. Die Chronik wartet auf ihren ersten Eintrag."));

    private static ThemePackConfiguration CreateSatisfactoryFicsit() =>
        new(
            SatisfactoryFicsitKey,
            "Industrial Pioneer",
            "Eigenständige Industrieoptik und übertrieben seriöse Konzernsprache für Fabrik-Communities.",
            "1.0.0",
            "Community Intranet",
            new ThemeVisuals(
                "#F59E0B",
                "#252A33",
                "#F97316",
                "#0B0D10",
                "#15181E",
                "#F5F7FA",
                "#99A1AD",
                "#FB7185",
                "#FBBF24",
                "#34D399",
                "factory",
                "industrial-corporate"),
            new ThemeTerminology(
                "Niederlassung",
                "Mitarbeiter",
                "Belegschaft",
                "Abteilung",
                "Bauvorhaben",
                "Arbeitsauftrag",
                "Betriebsstörung",
                "Auszeichnung",
                "Konzernchronik"),
            [
                "Pioneer",
                "Senior Conveyor Architect",
                "Power Grid Supervisor",
                "Chief Spaghetti Officer"
            ],
            [
                new SuggestedDepartment("Energieversorgung", "zap"),
                new SuggestedDepartment("Logistik", "route"),
                new SuggestedDepartment("Rohstoffbeschaffung", "pickaxe"),
                new SuggestedDepartment("Fundamentwesen", "blocks"),
                new SuggestedDepartment("Ungeklärte Bauprojekte", "circle-help")
            ],
            [
                "Stromausfall",
                "Produktionsstillstand",
                "Logistikproblem",
                "Fahrzeugverlust",
                "Ungeklärte Infrastruktur"
            ],
            [
                new AwardTemplate(
                    "Mitarbeiter der Woche",
                    "Für besondere Verdienste im Bereich {reason}."),
                new AwardTemplate(
                    "Chief Spaghetti Officer",
                    "Für herausragende Leistungen bei kaum nachvollziehbarer Logistik.")
            ],
            [
                "Effizienzprüfung läuft. Aussagekraft der Kennzahlen unbestätigt.",
                "Die Produktion arbeitet innerhalb großzügig ausgelegter Toleranzen.",
                "Das Management erkennt keine Probleme, nur ungeplante Lernchancen."
            ],
            new ThemeMessages(
                "Willkommen in der Niederlassung. Effizienz wird erwartet, aber nicht zwingend nachgewiesen.",
                "Derzeit sind keine Bauvorhaben dokumentiert. Das Management geht dennoch von unkontrollierter Bautätigkeit aus.",
                "Keine Arbeitsaufträge offen. Bitte prüfen, ob das Aufgabenformular verlegt wurde.",
                "Keine Betriebsstörungen gemeldet. Diese Angabe wird intern angezweifelt.",
                "Die Konzernchronik ist leer. Offiziell ist heute also nichts passiert."));
}
