using CommunityIntranet.Modules.Identity.Domain;

namespace CommunityIntranet.Modules.CounterStrike.Services;

public static class SteamIdentityMapper
{
    public static IReadOnlyDictionary<string, Guid> Map(
        IEnumerable<SteamIdentity> identities,
        IEnumerable<AnalyzerPlayerDto> players)
    {
        var requested = players
            .Select(player => player.SteamId.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .ToHashSet(StringComparer.Ordinal);
        return identities
            .Where(identity => requested.Contains(identity.SteamId64))
            .GroupBy(identity => identity.SteamId64, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().UserId, StringComparer.Ordinal);
    }
}
