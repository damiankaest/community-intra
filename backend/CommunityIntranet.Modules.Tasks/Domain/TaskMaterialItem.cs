namespace CommunityIntranet.Modules.Tasks.Domain;

public sealed class TaskMaterialItem
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid TaskId { get; set; }

    public required string Name { get; set; }

    public required string Quantity { get; set; }

    public string? Notes { get; set; }

    public bool IsPrepared { get; set; }

    public Guid? PreparedByMemberId { get; set; }

    public DateTimeOffset? PreparedAt { get; set; }

    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Guid ConcurrencyToken { get; set; }
}
