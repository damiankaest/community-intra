using Microsoft.AspNetCore.Routing;

namespace CommunityIntranet.Modules.Parties.Endpoints;

public static class PartyEndpoints
{
    public static IEndpointRouteBuilder MapPartyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        AdminPartyEndpoints.Map(endpoints);
        PublicPartyEndpoints.Map(endpoints);
        return endpoints;
    }
}
