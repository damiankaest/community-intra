namespace CommunityIntranet.Modules.Tasks.Domain;

public sealed class TaskAttachment
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid TaskId { get; set; }

    public Guid UploadedByMemberId { get; set; }

    public required string FileName { get; set; }

    public required string MediaType { get; set; }

    public long Size { get; set; }

    public required byte[] Content { get; set; }

    public byte[]? ThumbnailContent { get; set; }

    public string? ThumbnailMediaType { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
