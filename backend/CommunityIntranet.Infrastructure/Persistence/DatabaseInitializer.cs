using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CommunityIntranet.Infrastructure.Persistence;

public sealed class DatabaseInitializer(
    CommunityIntranetDbContext dbContext,
    ILogger<DatabaseInitializer> logger)
{
    public async Task ApplyMigrationsAsync(CancellationToken cancellationToken)
    {
        var pendingMigrations = await dbContext.Database
            .GetPendingMigrationsAsync(cancellationToken);

        if (!pendingMigrations.Any())
        {
            logger.LogInformation("No pending database migrations");
            return;
        }

        logger.LogInformation(
            "Applying {MigrationCount} database migration(s)",
            pendingMigrations.Count());

        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
