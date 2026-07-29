namespace CommunityIntranet.Modules.Organizations.Services;

public interface IOrganizationOwnerProvisioner
{
    Guid AddOwner(Guid organizationId, Guid userId, string? visibleTitle);

    void AddDepartments(
        Guid organizationId,
        IReadOnlyList<OrganizationDepartmentTemplate> departments);
}

public sealed record OrganizationDepartmentTemplate(string Name, string Icon);
