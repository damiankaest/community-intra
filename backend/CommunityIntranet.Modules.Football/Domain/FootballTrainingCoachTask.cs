using CommunityIntranet.BuildingBlocks.Tenancy;

namespace CommunityIntranet.Modules.Football.Domain;

public sealed class FootballTrainingCoachTask : IOrganizationScoped
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid SessionId { get; set; }
    public Guid TrainingBlockId { get; set; }
    public Guid MemberId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Task { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid UpdatedByMemberId { get; set; }
}
