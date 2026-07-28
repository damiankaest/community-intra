using System.Reflection;
using CommunityIntranet.Api.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CommunityIntranet.Api.Endpoints;

public static class SystemEndpoints
{
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api").WithTags("System");

        group.MapGet(
                "/system/info",
                (IHostEnvironment environment) =>
                {
                    var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3)
                        ?? "0.1.0";

                    return Results.Ok(
                        new SystemInfoResponse(
                            "Community Intranet",
                            version,
                            environment.EnvironmentName,
                            "Operational"));
                })
            .WithName("GetSystemInfo")
            .WithOpenApi()
            .Produces<SystemInfoResponse>();

        group.MapGet(
                "/health",
                async (
                    HealthCheckService healthCheckService,
                    TimeProvider timeProvider,
                    CancellationToken cancellationToken) =>
                {
                    var report = await healthCheckService.CheckHealthAsync(cancellationToken);
                    var response = HealthResponse.From(report, timeProvider.GetUtcNow());
                    var statusCode = report.Status == HealthStatus.Unhealthy
                        ? StatusCodes.Status503ServiceUnavailable
                        : StatusCodes.Status200OK;

                    return Results.Json(response, statusCode: statusCode);
                })
            .WithName("GetHealth")
            .WithOpenApi()
            .Produces<HealthResponse>()
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return endpoints;
    }
}
