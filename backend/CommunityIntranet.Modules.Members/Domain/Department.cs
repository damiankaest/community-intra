namespace CommunityIntranet.Modules.Members.Domain;

public sealed class Department
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public required string Icon { get; set; }

    public bool IsArchived { get; set; }
}
