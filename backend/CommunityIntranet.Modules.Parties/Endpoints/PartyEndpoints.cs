using Microsoft.AspNetCore.Routing;

namespace CommunityIntranet.Modules.Parties.Endpoints;

public static class PartyEndpoints
{
    public static IEndpointRouteBuilder MapPartyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        AdminPartyEndpoints.Map(endpoints);
        PublicPartyEndpoints.Map(endpoints);
        SpotifyPartyEndpoints.Map(endpoints);
        return endpoints;
    }
}
