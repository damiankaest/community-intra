using CommunityIntranet.Modules.FactoryInsights.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http.Features;

namespace CommunityIntranet.Modules.FactoryInsights;

public static class DependencyInjection
{
    public static IServiceCollection AddFactoryInsightsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var baseUrl = configuration["SaveParser:BaseUrl"]
            ?? "http://127.0.0.1:5091";
        services.AddHttpClient<ISaveFileAnalyzer, SaveFileAnalyzer>(client =>
        {
            client.BaseAddress = new Uri(
                baseUrl.TrimEnd('/') + "/",
                UriKind.Absolute);
            client.Timeout = TimeSpan.FromMinutes(5);
        });
        services.Configure<FormOptions>(options =>
            options.MultipartBodyLengthLimit = 201L * 1024 * 1024);
        return services;
    }
}
