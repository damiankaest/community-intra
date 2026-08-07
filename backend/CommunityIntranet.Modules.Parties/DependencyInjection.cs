using CommunityIntranet.Modules.Parties.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityIntranet.Modules.Parties;

public static class DependencyInjection
{
    public static IServiceCollection AddPartiesModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PartyMediaOptions>(configuration.GetSection(PartyMediaOptions.SectionName));
        services.Configure<FormOptions>(options =>
            options.MultipartBodyLengthLimit = PartyMediaValidator.MaximumVideoSize + 2 * 1024 * 1024);
        services.AddSingleton<IPartyMediaStorage, PartyMediaStorage>();
        return services;
    }
}
