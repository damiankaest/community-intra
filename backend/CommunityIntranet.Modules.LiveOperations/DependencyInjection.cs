using CommunityIntranet.BuildingBlocks.LiveOperations;
using CommunityIntranet.Modules.LiveOperations.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityIntranet.Modules.LiveOperations;

public static class DependencyInjection
{
    public static IServiceCollection AddLiveOperationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var dataProtection = services
            .AddDataProtection()
            .SetApplicationName("CommunityIntranet");
        var keysPath = configuration["DataProtection:KeysPath"];
        if (!string.IsNullOrWhiteSpace(keysPath))
        {
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        }

        services.AddMemoryCache();
        services.AddSingleton<ISatisfactoryServerClient, SatisfactoryServerClient>();
        services.AddSingleton<IGameServerTokenProtector, GameServerTokenProtector>();
        services.AddScoped<ILiveOperationsReader, LiveOperationsReader>();
        return services;
    }
}
