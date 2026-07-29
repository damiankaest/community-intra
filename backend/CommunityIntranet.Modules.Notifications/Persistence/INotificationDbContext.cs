using CommunityIntranet.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.Notifications.Persistence;

public interface INotificationDbContext
{
    DbSet<MemberNotification> Notifications { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
