using System.Security.Claims;
using CommunityIntranet.Modules.Parties.Domain;
using CommunityIntranet.Modules.Parties.Persistence;
using CommunityIntranet.Modules.Parties.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.Parties.Endpoints;

internal static class PartyEndpointHelpers
{
    internal const string GuestSessionHeader = "X-Party-Session";

    internal static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    internal static async Task<Party?> GetOwnedPartyAsync(
        IPartyDbContext dbContext,
        Guid partyId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken,
        bool tracked = false)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return null;
        }

        var query = tracked ? dbContext.Parties : dbContext.Parties.AsNoTracking();
        return await query.SingleOrDefaultAsync(
            party => party.Id == partyId
                && party.OwnerUserId == userId
                && !party.IsArchived,
            cancellationToken);
    }

    internal static async Task<(Party? Party, PartyGuest? Guest)> GetGuestAccessAsync(
        IPartyDbContext dbContext,
        string slug,
        HttpContext httpContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var party = await dbContext.Parties.SingleOrDefaultAsync(
            item => item.Slug == slug && !item.IsArchived,
            cancellationToken);
        if (party is null || !party.IsActive)
        {
            return (party, null);
        }

        if (!httpContext.Request.Headers.TryGetValue(GuestSessionHeader, out var values))
        {
            return (party, null);
        }

        var rawToken = values.ToString();
        if (string.IsNullOrWhiteSpace(rawToken) || rawToken.Length > 128)
        {
            return (party, null);
        }

        var tokenHash = PartyTokenService.Hash(rawToken);
        var guest = await dbContext.PartyGuests.SingleOrDefaultAsync(
            item => item.PartyId == party.Id
                && item.SessionTokenHash == tokenHash
                && !item.IsRemoved,
            cancellationToken);
        if (guest is not null)
        {
            guest.LastSeenAt = timeProvider.GetUtcNow();
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return (party, guest);
    }

    internal static IResult Validation(string key, string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { [key] = [message] });

    internal static string? Clean(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var clean = value.Trim();
        return clean[..Math.Min(clean.Length, maxLength)];
    }
}
