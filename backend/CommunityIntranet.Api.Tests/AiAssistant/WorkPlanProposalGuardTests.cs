using CommunityIntranet.Modules.AiAssistant.Contracts;
using CommunityIntranet.Modules.AiAssistant.Services;
using CommunityIntranet.Modules.Tasks.Domain;
using Xunit;

namespace CommunityIntranet.Api.Tests.AiAssistant;

public sealed class WorkPlanProposalGuardTests
{
    [Fact]
    public void ValidateAndNormalizeAcceptsUsableProposal()
    {
        var proposal = new WorkPlanProposal(
            " Aluminiumversorgung ",
            " Versorgung aufbauen. ",
            " Bitte zeitnah Synergien erzeugen. ",
            [new WorkPlanMaterial(" Bauxit ", " zu prüfen ", null)],
            [
                new WorkPlanTask(
                    " Raffinerie bauen ",
                    " Produktionslinie errichten. ",
                    WorkTaskPriority.High,
                    ["Aluminiumoxid wird produziert.", "Aluminiumoxid wird produziert."],
                    [new WorkPlanMaterial(" Raffinerie ", " 1 ", " Bauplatz prüfen ")])
            ]);

        var result = WorkPlanProposalGuard.ValidateAndNormalize(proposal);

        Assert.True(result.IsSuccess);
        Assert.Equal("Aluminiumversorgung", result.Proposal!.Title);
        Assert.Single(result.Proposal.Tasks[0].AcceptanceCriteria);
        Assert.Equal("Bauxit", result.Proposal.Materials[0].Name);
        Assert.Equal("Raffinerie", result.Proposal.Tasks[0].Materials[0].Name);
    }

    [Fact]
    public void ValidateAndNormalizeRejectsProposalWithoutTasks()
    {
        var proposal = new WorkPlanProposal(
            "Projekt",
            "Beschreibung",
            "Mitteilung",
            [],
            []);

        var result = WorkPlanProposalGuard.ValidateAndNormalize(proposal);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Proposal);
    }

    [Fact]
    public void ValidateAndNormalizeRejectsMissingGeneratedCollections()
    {
        var proposal = new WorkPlanProposal(
            "Projekt",
            "Beschreibung",
            "Mitteilung",
            null!,
            null!);

        var result = WorkPlanProposalGuard.ValidateAndNormalize(proposal);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Proposal);
    }

    [Fact]
    public void ValidateAndNormalizeLimitsGeneratedContent()
    {
        var proposal = new WorkPlanProposal(
            new string('A', 240),
            new string('B', 1400),
            new string('C', 1200),
            [],
            [
                new WorkPlanTask(
                    new string('D', 260),
                    new string('E', 2600),
                    WorkTaskPriority.Normal,
                    Enumerable.Range(1, 12)
                        .Select(index => $"Kriterium {index}")
                        .ToArray(),
                    [])
            ]);

        var result = WorkPlanProposalGuard.ValidateAndNormalize(proposal);

        Assert.True(result.IsSuccess);
        Assert.Equal(160, result.Proposal!.Title.Length);
        Assert.Equal(200, result.Proposal.Tasks[0].Title.Length);
        Assert.Equal(8, result.Proposal.Tasks[0].AcceptanceCriteria.Count);
    }
}
