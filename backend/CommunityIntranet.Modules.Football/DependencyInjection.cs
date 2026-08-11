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
        services.AddScoped<IFootballTrainingPlanner, FootballTrainingPlanner>();

        if (configuration.GetValue("Database:ApplyMigrations", false))
        {
            services.AddHostedService<FootballMigrationHostedService>();
        }

        return services;
    }
}
