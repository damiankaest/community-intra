using System.Text.RegularExpressions;

namespace CommunityIntranet.Modules.ThemePacks.Configuration;

public sealed partial class ThemePackConfigurationValidator
{
    private const int MaximumSerializedBytes = 128 * 1024;
    private const int MaximumListItems = 50;
    private const int MaximumTextLength = 1000;

    private static readonly HashSet<string> AllowedIcons =
        new(StringComparer.Ordinal)
        {
            "blocks",
            "briefcase",
            "building-2",
            "circle-help",
            "clipboard-list",
            "factory",
            "folder-kanban",
            "pickaxe",
            "route",
            "scroll-text",
            "shield-check",
            "triangle-alert",
            "trophy",
            "users",
            "zap"
        };

    private static readonly HashSet<string> AllowedStyles =
        new(StringComparer.Ordinal)
        {
            "clean-corporate",
            "industrial-corporate"
        };

    public ThemePackValidationResult Validate(
        ThemePackConfiguration configuration,
        int? serializedByteCount = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var errors = new List<string>();

        ValidateRequired(
            errors,
            nameof(configuration.Key),
            configuration.Key,
            64,
            KeyRegex());
        ValidateRequired(
            errors,
            nameof(configuration.Name),
            configuration.Name,
            120);
        ValidateRequired(
            errors,
            nameof(configuration.Description),
            configuration.Description,
            MaximumTextLength);
        ValidateRequired(
            errors,
            nameof(configuration.Version),
            configuration.Version,
            32,
            SemVerRegex());
        ValidateRequired(
            errors,
            nameof(configuration.Author),
            configuration.Author,
            120);

        ValidateVisuals(errors, configuration.Visuals);
        ValidateTerminology(errors, configuration.Terminology);
        ValidateStringList(
            errors,
            nameof(configuration.SuggestedTitles),
            configuration.SuggestedTitles,
            100);
        ValidateDepartments(errors, configuration.SuggestedDepartments);
        ValidateStringList(
            errors,
            nameof(configuration.IncidentCategories),
            configuration.IncidentCategories,
            100);
        ValidateAwardTemplates(errors, configuration.AwardTemplates);
        ValidateStringList(
            errors,
            nameof(configuration.StatusMessages),
            configuration.StatusMessages,
            300);
        ValidateMessages(errors, configuration.Messages);

        if (serializedByteCount > MaximumSerializedBytes)
        {
            errors.Add(
                $"Theme pack configuration exceeds {MaximumSerializedBytes} bytes.");
        }

        return new ThemePackValidationResult(errors);
    }

    private static void ValidateVisuals(
        ICollection<string> errors,
        ThemeVisuals? visuals)
    {
        if (visuals is null)
        {
            errors.Add("Visuals is required.");
            return;
        }

        ValidateColor(errors, nameof(visuals.PrimaryColor), visuals.PrimaryColor);
        ValidateColor(errors, nameof(visuals.SecondaryColor), visuals.SecondaryColor);
        ValidateColor(errors, nameof(visuals.AccentColor), visuals.AccentColor);
        ValidateColor(errors, nameof(visuals.BackgroundColor), visuals.BackgroundColor);
        ValidateColor(errors, nameof(visuals.SurfaceColor), visuals.SurfaceColor);
        ValidateColor(errors, nameof(visuals.TextColor), visuals.TextColor);
        ValidateColor(errors, nameof(visuals.MutedColor), visuals.MutedColor);
        ValidateColor(errors, nameof(visuals.DangerColor), visuals.DangerColor);
        ValidateColor(errors, nameof(visuals.WarningColor), visuals.WarningColor);
        ValidateColor(errors, nameof(visuals.SuccessColor), visuals.SuccessColor);

        if (!AllowedIcons.Contains(visuals.LogoIcon))
        {
            errors.Add($"LogoIcon '{visuals.LogoIcon}' is not allowed.");
        }

        if (!AllowedStyles.Contains(visuals.Style))
        {
            errors.Add($"Style '{visuals.Style}' is not allowed.");
        }
    }

    private static void ValidateTerminology(
        ICollection<string> errors,
        ThemeTerminology? terminology)
    {
        if (terminology is null)
        {
            errors.Add("Terminology is required.");
            return;
        }

        ValidateRequired(errors, nameof(terminology.Organization), terminology.Organization, 80);
        ValidateRequired(errors, nameof(terminology.Member), terminology.Member, 80);
        ValidateRequired(errors, nameof(terminology.Members), terminology.Members, 80);
        ValidateRequired(errors, nameof(terminology.Department), terminology.Department, 80);
        ValidateRequired(errors, nameof(terminology.Project), terminology.Project, 80);
        ValidateRequired(errors, nameof(terminology.Task), terminology.Task, 80);
        ValidateRequired(errors, nameof(terminology.Incident), terminology.Incident, 80);
        ValidateRequired(errors, nameof(terminology.Award), terminology.Award, 80);
        ValidateRequired(errors, nameof(terminology.ActivityFeed), terminology.ActivityFeed, 80);
    }

    private static void ValidateDepartments(
        ICollection<string> errors,
        IReadOnlyList<SuggestedDepartment>? departments)
    {
        if (!ValidateListCount(errors, nameof(ThemePackConfiguration.SuggestedDepartments), departments))
        {
            return;
        }

        foreach (var department in departments!)
        {
            if (department is null)
            {
                errors.Add("SuggestedDepartments contains a null item.");
                continue;
            }

            ValidateRequired(errors, "SuggestedDepartments.Name", department.Name, 100);
            if (!AllowedIcons.Contains(department.Icon))
            {
                errors.Add($"Department icon '{department.Icon}' is not allowed.");
            }
        }
    }

    private static void ValidateAwardTemplates(
        ICollection<string> errors,
        IReadOnlyList<AwardTemplate>? templates)
    {
        if (!ValidateListCount(errors, nameof(ThemePackConfiguration.AwardTemplates), templates))
        {
            return;
        }

        foreach (var template in templates!)
        {
            if (template is null)
            {
                errors.Add("AwardTemplates contains a null item.");
                continue;
            }

            ValidateRequired(errors, "AwardTemplates.Name", template.Name, 120);
            ValidateRequired(
                errors,
                "AwardTemplates.DescriptionTemplate",
                template.DescriptionTemplate,
                500);
        }
    }

    private static void ValidateMessages(
        ICollection<string> errors,
        ThemeMessages? messages)
    {
        if (messages is null)
        {
            errors.Add("Messages is required.");
            return;
        }

        ValidateRequired(errors, nameof(messages.Welcome), messages.Welcome, MaximumTextLength);
        ValidateRequired(errors, nameof(messages.EmptyProjects), messages.EmptyProjects, MaximumTextLength);
        ValidateRequired(errors, nameof(messages.EmptyTasks), messages.EmptyTasks, MaximumTextLength);
        ValidateRequired(errors, nameof(messages.EmptyIncidents), messages.EmptyIncidents, MaximumTextLength);
        ValidateRequired(errors, nameof(messages.EmptyActivityFeed), messages.EmptyActivityFeed, MaximumTextLength);
    }

    private static void ValidateStringList(
        ICollection<string> errors,
        string field,
        IReadOnlyList<string>? values,
        int maximumItemLength)
    {
        if (!ValidateListCount(errors, field, values))
        {
            return;
        }

        foreach (var value in values!)
        {
            ValidateRequired(errors, field, value, maximumItemLength);
        }
    }

    private static bool ValidateListCount<T>(
        ICollection<string> errors,
        string field,
        IReadOnlyCollection<T>? values)
    {
        if (values is null)
        {
            errors.Add($"{field} is required.");
            return false;
        }

        if (values.Count > MaximumListItems)
        {
            errors.Add($"{field} cannot contain more than {MaximumListItems} items.");
        }

        return true;
    }

    private static void ValidateColor(
        ICollection<string> errors,
        string field,
        string value)
    {
        if (!ColorRegex().IsMatch(value ?? string.Empty))
        {
            errors.Add($"{field} must use #RRGGBB.");
        }
    }

    private static void ValidateRequired(
        ICollection<string> errors,
        string field,
        string? value,
        int maximumLength,
        Regex? pattern = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{field} is required.");
            return;
        }

        if (value.Length > maximumLength)
        {
            errors.Add($"{field} cannot exceed {maximumLength} characters.");
        }

        if (ContainsUnsafeText(value))
        {
            errors.Add($"{field} contains HTML-like markup or control characters.");
        }

        if (pattern is not null && !pattern.IsMatch(value))
        {
            errors.Add($"{field} has an invalid format.");
        }
    }

    private static bool ContainsUnsafeText(string value) =>
        value.Contains('<')
        || value.Contains('>')
        || value.Any(character =>
            char.IsControl(character)
            && character is not '\r' and not '\n' and not '\t');

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex KeyRegex();

    [GeneratedRegex(
        "^(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)(?:-[0-9A-Za-z.-]+)?(?:\\+[0-9A-Za-z.-]+)?$",
        RegexOptions.CultureInvariant)]
    private static partial Regex SemVerRegex();

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex ColorRegex();
}

public sealed record ThemePackValidationResult(IReadOnlyList<string> Errors)
{
    public bool IsValid => Errors.Count == 0;
}
