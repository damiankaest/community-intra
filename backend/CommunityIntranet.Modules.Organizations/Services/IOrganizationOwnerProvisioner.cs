namespace CommunityIntranet.Modules.Organizations.Services;

public interface IOrganizationOwnerProvisioner
{
    void AddOwner(Guid organizationId, Guid userId, string? visibleTitle);
}
