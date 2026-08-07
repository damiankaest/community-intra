using CommunityIntranet.Modules.Parties.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.Parties.Persistence;

public interface IPartyDbContext
{
    DbSet<Party> Parties { get; }
    DbSet<PartyGuest> PartyGuests { get; }
    DbSet<PartyMedia> PartyMedia { get; }
    DbSet<PartyOrderItem> PartyOrderItems { get; }
    DbSet<PartyOrder> PartyOrders { get; }
    DbSet<PartyMusicRequest> PartyMusicRequests { get; }
    DbSet<PartyMusicVote> PartyMusicVotes { get; }
    DbSet<PartyGuestbookEntry> PartyGuestbookEntries { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
