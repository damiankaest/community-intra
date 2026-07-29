using CommunityIntranet.BuildingBlocks.Tenancy;

namespace CommunityIntranet.Modules.LiveOperations.Domain;

public sealed class GameServerConnection : IOrganizationScoped
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public required string DisplayName { get; set; }

    public required string Host { get; set; }

    public int Port { get; set; }

    public required string ProtectedApiToken { get; set; }

    public string? CertificateFingerprint { get; set; }

    public bool IsEnabled { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Guid ConcurrencyToken { get; set; }
}
