using CommunityIntranet.Modules.Football.Persistence;
using CommunityIntranet.Modules.Football.Planning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityIntranet.Modules.Football;

public static class DependencyInjection
{
    public static IServiceCollection AddFootballModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PostgreSql")
            ?? throw new InvalidOperationException("ConnectionStrings:PostgreSql is required.");

        services.AddDbContext<FootballDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(FootballDbContext).Assembly.FullName)));
        services.AddScoped<IFootballDbContext>(provider => provider.GetRequiredService<FootballDbContext>());

        services.Configure<FootballAiOptions>(options =>
        {
            options.ApiKey = configuration["AiAssistant:ApiKey"]
                ?? configuration["OPENAI_API_KEY"]
                ?? string.Empty;
            options.Model = configuration["AiAssistant:Model"]
                ?? configuration["AI_MODEL"]
                ?? "gpt-5.6";

            var endpoint = configuration["AiAssistant:Endpoint"];
            if (Uri.TryCreate(endpoint, UriKind.Absolute, out var parsedEndpoint))
            {
                options.Endpoint = parsedEndpoint;
            }
        });

        services.AddScoped<FootballTrainingPlanner>();
        services.AddHttpClient<OpenAiFootballTrainingPlanner>(client =>
            client.Timeout = TimeSpan.FromSeconds(45));
        services.AddScoped<IFootballTrainingPlanner>(provider =>
            provider.GetRequiredService<OpenAiFootballTrainingPlanner>());

        if (configuration.GetValue("Database:ApplyMigrations", false))
        {
            services.AddHostedService<FootballMigrationHostedService>();
        }

        return services;
    }
}
