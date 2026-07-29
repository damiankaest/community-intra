using CommunityIntranet.Modules.AiAssistant.Contracts;
using CommunityIntranet.Modules.Tasks.Domain;

namespace CommunityIntranet.Modules.AiAssistant.Services;

public static class WorkPlanProposalGuard
{
    public static WorkPlanGenerationResult ValidateAndNormalize(
        WorkPlanProposal proposal)
    {
        ArgumentNullException.ThrowIfNull(proposal);

        var title = Normalize(proposal.Title, 160);
        var summary = Normalize(proposal.ExecutiveSummary, 1200);
        var message = Normalize(proposal.ManagementMessage, 1000);
        if (title is null || summary is null || message is null)
        {
            return WorkPlanGenerationResult.Failure(
                "Der KI-Entwurf enthält unvollständige Kerndaten.");
        }

        if (proposal.Tasks is null || proposal.Tasks.Count is < 1 or > 12)
        {
            return WorkPlanGenerationResult.Failure(
                "Der KI-Entwurf muss zwischen einer und zwölf Aufgaben enthalten.");
        }

        if (proposal.Materials is null || proposal.Materials.Count > 24)
        {
            return WorkPlanGenerationResult.Failure(
                "Der KI-Entwurf enthält zu viele Ressourcen.");
        }

        var materials = NormalizeMaterials(proposal.Materials);
        if (materials is null)
        {
            return WorkPlanGenerationResult.Failure(
                "Eine Ressource im KI-Entwurf ist unvollständig.");
        }

        var tasks = new List<WorkPlanTask>(proposal.Tasks.Count);
        foreach (var task in proposal.Tasks)
        {
            var taskTitle = Normalize(task.Title, 200);
            var description = Normalize(task.Description, 2400);
            if (taskTitle is null || description is null
                || !Enum.IsDefined(task.Priority))
            {
                return WorkPlanGenerationResult.Failure(
                    "Eine Aufgabe im KI-Entwurf ist ungültig.");
            }

            var criteria = (task.AcceptanceCriteria ?? [])
                .Select(value => Normalize(value, 300))
                .Where(value => value is not null)
                .Cast<string>()
                .Distinct(StringComparer.Ordinal)
                .Take(8)
                .ToArray();
            if (task.Materials is null || task.Materials.Count > 24)
            {
                return WorkPlanGenerationResult.Failure(
                    "Eine Aufgabe im KI-Entwurf enthält zu viele Materialien.");
            }

            var taskMaterials = NormalizeMaterials(task.Materials);
            if (taskMaterials is null)
            {
                return WorkPlanGenerationResult.Failure(
                    "Ein Material in einer Aufgabe ist unvollständig.");
            }

            tasks.Add(new WorkPlanTask(
                taskTitle,
                description,
                task.Priority,
                criteria,
                taskMaterials));
        }

        return WorkPlanGenerationResult.Success(new WorkPlanProposal(
            title,
            summary,
            message,
            materials,
            tasks));
    }

    private static IReadOnlyList<WorkPlanMaterial>? NormalizeMaterials(
        IReadOnlyList<WorkPlanMaterial> source)
    {
        var materials = new List<WorkPlanMaterial>(source.Count);
        foreach (var material in source)
        {
            var name = Normalize(material.Name, 160);
            var quantity = Normalize(material.Quantity, 80);
            if (name is null || quantity is null)
            {
                return null;
            }

            materials.Add(new WorkPlanMaterial(
                name,
                quantity,
                Normalize(material.Notes, 300)));
        }

        return materials;
    }

    private static string? Normalize(string? value, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maximumLength
            ? normalized
            : normalized[..maximumLength].TrimEnd();
    }
}
