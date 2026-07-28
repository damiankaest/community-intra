using CommunityIntranet.Modules.Organizations.Contracts;
using CommunityIntranet.Modules.ThemePacks.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CommunityIntranet.Infrastructure.Persistence;

public sealed partial class DatabaseInitializer
{
    private readonly CommunityIntranetDbContext _dbContext;
    private readonly ThemePackSeeder _themePackSeeder;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        CommunityIntranetDbContext dbContext,
        ThemePackSeeder themePackSeeder,
        ILogger<DatabaseInitializer> logger)
    {
        _dbContext = dbContext;
        _themePackSeeder = themePackSeeder;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var pendingMigrations = await _dbContext.Database
            .GetPendingMigrationsAsync(cancellationToken);

        if (pendingMigrations.Any())
        {
            LogApplyingMigrations(pendingMigrations.Count());
            await _dbContext.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            LogNoPendingMigrations();
        }

        await _themePackSeeder.SeedAsync(cancellationToken);

        var genericTheme = await _dbContext.ThemePacks.SingleAsync(
            themePack =>
                themePack.Key == ThemePackSeeds.GenericCorporateKey
                && themePack.Version == "1.0.0",
            cancellationToken);
        var organizations = await _dbContext.Organizations
            .Where(organization =>
                organization.ThemePackId == null
                || organization.EnabledModules.Count == 0)
            .ToListAsync(cancellationToken);

        foreach (var organization in organizations)
        {
            organization.ThemePackId ??= genericTheme.Id;
            if (organization.EnabledModules.Count == 0)
            {
                organization.EnabledModules = [.. OrganizationModuleKeys.Defaults];
            }
        }

        if (organizations.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            LogOrganizationsUpdated(organizations.Count);
        }
    }

    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "No pending database migrations")]
    private partial void LogNoPendingMigrations();

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Applying {MigrationCount} database migration(s)")]
    private partial void LogApplyingMigrations(int migrationCount);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "Assigned Phase 4 defaults to {OrganizationCount} organization(s)")]
    private partial void LogOrganizationsUpdated(int organizationCount);
}
