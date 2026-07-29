using CommunityIntranet.BuildingBlocks.Tenancy;

namespace CommunityIntranet.Modules.FactoryInsights.Domain;

public sealed class FactorySite : IOrganizationScoped
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public double? CenterX { get; set; }

    public double? CenterY { get; set; }

    public double? RadiusMeters { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Guid ConcurrencyToken { get; set; }
}
