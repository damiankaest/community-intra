namespace CommunityIntranet.Modules.ThemePacks.Configuration;

public sealed record ThemePackConfiguration(
    string Key,
    string Name,
    string Description,
    string Version,
    string Author,
    ThemeVisuals Visuals,
    ThemeTerminology Terminology,
    IReadOnlyList<string> SuggestedTitles,
    IReadOnlyList<SuggestedDepartment> SuggestedDepartments,
    IReadOnlyList<string> IncidentCategories,
    IReadOnlyList<AwardTemplate> AwardTemplates,
    IReadOnlyList<string> StatusMessages,
    ThemeMessages Messages);

public sealed record ThemeVisuals(
    string PrimaryColor,
    string SecondaryColor,
    string AccentColor,
    string BackgroundColor,
    string SurfaceColor,
    string TextColor,
    string MutedColor,
    string DangerColor,
    string WarningColor,
    string SuccessColor,
    string LogoIcon,
    string Style);

public sealed record ThemeTerminology(
    string Organization,
    string Member,
    string Members,
    string Department,
    string Project,
    string Task,
    string Incident,
    string Award,
    string ActivityFeed);

public sealed record SuggestedDepartment(string Name, string Icon);

public sealed record AwardTemplate(string Name, string DescriptionTemplate);

public sealed record ThemeMessages(
    string Welcome,
    string EmptyProjects,
    string EmptyTasks,
    string EmptyIncidents,
    string EmptyActivityFeed);
