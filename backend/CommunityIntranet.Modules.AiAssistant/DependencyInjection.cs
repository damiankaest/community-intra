using CommunityIntranet.Modules.AiAssistant.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityIntranet.Modules.AiAssistant;

public static class DependencyInjection
{
    public static IServiceCollection AddAiAssistantModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AiAssistantOptions>(
            configuration.GetSection(AiAssistantOptions.SectionName));
        services.AddHttpClient<IWorkPlanGenerator, OpenAiWorkPlanGenerator>(
            client => client.Timeout = TimeSpan.FromSeconds(45));
        services.AddHttpClient<IWorkspaceChatGenerator, OpenAiWorkspaceChatGenerator>(
            client => client.Timeout = TimeSpan.FromSeconds(90));
        return services;
    }
}
