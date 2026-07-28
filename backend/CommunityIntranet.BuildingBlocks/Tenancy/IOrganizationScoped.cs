namespace CommunityIntranet.BuildingBlocks.Tenancy;

public interface IOrganizationScoped
{
    Guid OrganizationId { get; }
}
