using CommunityIntranet.Modules.Projects.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.Projects.Services;

public sealed class ProjectLookup(IProjectDbContext dbContext) : IProjectLookup
{
    public Task<bool> ExistsAsync(
        Guid organizationId,
        Guid projectId,
        CancellationToken cancellationToken) =>
        dbContext.Projects.AnyAsync(
            project =>
                project.OrganizationId == organizationId
                && project.Id == projectId
                && project.Status != Domain.ProjectStatus.Cancelled,
            cancellationToken);
}
