using CommunityIntranet.Modules.Members.Domain;
using CommunityIntranet.Modules.Organizations.Contracts;
using CommunityIntranet.Modules.ThemePacks.Configuration;
using CommunityIntranet.Modules.ThemePacks.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CommunityIntranet.Infrastructure.Persistence;

public sealed partial class DatabaseInitializer
{
    private readonly CommunityIntranetDbContext _dbContext;
    private readonly ThemePackSeeder _themePackSeeder;
    private readonly ThemePackSerializer _themePackSerializer;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        CommunityIntranetDbContext dbContext,
        ThemePackSeeder themePackSeeder,
        ThemePackSerializer themePackSerializer,
        ILogger<DatabaseInitializer> logger)
    {
        _dbContext = dbContext;
        _themePackSeeder = themePackSeeder;
        _themePackSerializer = themePackSerializer;
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

        await SeedMissingDepartmentsAsync(cancellationToken);
    }

    private async Task SeedMissingDepartmentsAsync(
        CancellationToken cancellationToken)
    {
        var organizationIdsWithDepartments = await _dbContext.Departments
            .Select(department => department.OrganizationId)
            .Distinct()
            .ToArrayAsync(cancellationToken);
        var organizations = await _dbContext.Organizations
            .AsNoTracking()
            .Where(organization =>
                !organization.IsArchived
                && organization.ThemePackId != null
                && !organizationIdsWithDepartments.Contains(organization.Id))
            .ToArrayAsync(cancellationToken);
        if (organizations.Length == 0)
        {
            return;
        }

        var themePackIds = organizations
            .Select(organization => organization.ThemePackId!.Value)
            .Distinct()
            .ToArray();
        var themePacks = await _dbContext.ThemePacks
            .AsNoTracking()
            .Where(themePack => themePackIds.Contains(themePack.Id))
            .ToDictionaryAsync(themePack => themePack.Id, cancellationToken);

        var departmentCount = 0;
        foreach (var organization in organizations)
        {
            if (!themePacks.TryGetValue(
                    organization.ThemePackId!.Value,
                    out var themePack))
            {
                continue;
            }

            var configuration = _themePackSerializer.Deserialize(
                themePack.ConfigurationJson);
            for (var index = 0;
                 index < configuration.SuggestedDepartments.Count;
                 index++)
            {
                var suggestedDepartment =
                    configuration.SuggestedDepartments[index];
                _dbContext.Departments.Add(new Department
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organization.Id,
                    Name = suggestedDepartment.Name,
                    Icon = suggestedDepartment.Icon,
                    SortOrder = index,
                    IsArchived = false
                });
                departmentCount++;
            }
        }

        if (departmentCount > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            LogDepartmentsAdded(departmentCount);
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

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Information,
        Message = "Added {DepartmentCount} Phase 5 department(s)")]
    private partial void LogDepartmentsAdded(int departmentCount);
}
