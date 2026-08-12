using CommunityIntranet.Modules.Mystery.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityIntranet.Modules.Mystery;

public static class DependencyInjection
{
    public static IServiceCollection AddMysteryModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MysteryProviderOptions>(options =>
        {
            options.ApiKey = configuration["Mystery:ApiKey"]
                ?? configuration["AiAssistant:ApiKey"]
                ?? configuration["OPENAI_API_KEY"]
                ?? string.Empty;
            options.Model = configuration["Mystery:Model"]
                ?? configuration["AiAssistant:Model"]
                ?? configuration["AI_MODEL"]
                ?? "gpt-5.6";
            options.FallbackOnGenerationError = configuration.GetValue(
                "Mystery:FallbackOnGenerationError",
                false);

            var endpoint = configuration["Mystery:Endpoint"]
                ?? configuration["AiAssistant:Endpoint"];
            if (Uri.TryCreate(endpoint, UriKind.Absolute, out var parsedEndpoint))
            {
                options.Endpoint = parsedEndpoint;
            }
        });

        services.AddSingleton<LocalMysteryProvider>();
        services.AddHttpClient<OpenAiMysteryProvider>(client =>
            client.Timeout = TimeSpan.FromMinutes(4));
        services.AddScoped<IMysteryLlmProvider>(provider =>
            provider.GetRequiredService<OpenAiMysteryProvider>());
        return services;
    }
}
