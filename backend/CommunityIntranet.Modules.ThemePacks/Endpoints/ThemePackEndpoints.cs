using CommunityIntranet.Modules.ThemePacks.Contracts;
using CommunityIntranet.Modules.ThemePacks.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CommunityIntranet.Modules.ThemePacks.Endpoints;

public static class ThemePackEndpoints
{
    public static IEndpointRouteBuilder MapThemePackEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/theme-packs")
            .WithTags("Theme Packs")
            .RequireAuthorization();

        group.MapGet("/", ListAsync);
        group.MapGet("/{key}", GetAsync);

        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        IThemePackCatalog catalog,
        CancellationToken cancellationToken)
    {
        var themePacks = await catalog.ListAsync(cancellationToken);
        return Results.Ok(themePacks.Select(ToResponse));
    }

    private static async Task<IResult> GetAsync(
        string key,
        IThemePackCatalog catalog,
        CancellationToken cancellationToken)
    {
        var themePack = await catalog.FindByKeyAsync(key, cancellationToken);
        return themePack is null
            ? Results.NotFound()
            : Results.Ok(ToResponse(themePack));
    }

    private static ThemePackResponse ToResponse(ThemePackDefinition themePack) =>
        new(
            themePack.Id,
            themePack.Key,
            themePack.Name,
            themePack.Description,
            themePack.Version,
            themePack.Author,
            themePack.IsSystemTheme,
            themePack.Configuration);
}
