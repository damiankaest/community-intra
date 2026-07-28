using CommunityIntranet.Modules.Organizations.Validation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityIntranet.Modules.Organizations;

public static class DependencyInjection
{
    public static IServiceCollection AddOrganizationsModule(
        this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateOrganizationRequestValidator>();
        return services;
    }
}
