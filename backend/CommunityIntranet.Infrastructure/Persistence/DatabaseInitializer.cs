using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CommunityIntranet.Infrastructure.Persistence;

public sealed partial class DatabaseInitializer
{
    private readonly CommunityIntranetDbContext _dbContext;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        CommunityIntranetDbContext dbContext,
        ILogger<DatabaseInitializer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task ApplyMigrationsAsync(CancellationToken cancellationToken)
    {
        var pendingMigrations = await _dbContext.Database
            .GetPendingMigrationsAsync(cancellationToken);

        if (!pendingMigrations.Any())
        {
            LogNoPendingMigrations();
            return;
        }

        LogApplyingMigrations(pendingMigrations.Count());

        await _dbContext.Database.MigrateAsync(cancellationToken);
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
}
