namespace CommunityIntranet.Modules.Projects.Services;

public interface IProjectLookup
{
    Task<bool> ExistsAsync(
        Guid organizationId,
        Guid projectId,
        CancellationToken cancellationToken);
}
