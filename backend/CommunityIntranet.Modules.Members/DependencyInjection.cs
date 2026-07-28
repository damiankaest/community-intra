using CommunityIntranet.BuildingBlocks.Tenancy;
using CommunityIntranet.Modules.Members.Services;
using CommunityIntranet.Modules.Organizations.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityIntranet.Modules.Members;

public static class DependencyInjection
{
    public static IServiceCollection AddMembersModule(
        this IServiceCollection services)
    {
        services.AddScoped<OrganizationAccessService>();
        services.AddScoped<IOrganizationAccessService>(
            provider => provider.GetRequiredService<OrganizationAccessService>());
        services.AddScoped<IOrganizationOwnerProvisioner>(
            provider => provider.GetRequiredService<OrganizationAccessService>());

        return services;
    }
}
