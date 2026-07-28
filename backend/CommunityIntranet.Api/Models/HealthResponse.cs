using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CommunityIntranet.Api.Models;

public sealed record HealthResponse(
    string Status,
    DateTimeOffset CheckedAt,
    IReadOnlyDictionary<string, HealthCheckEntryResponse> Checks)
{
    public static HealthResponse From(HealthReport report, DateTimeOffset checkedAt)
    {
        var checks = report.Entries.ToDictionary(
            entry => entry.Key,
            entry => new HealthCheckEntryResponse(
                entry.Value.Status.ToString(),
                entry.Value.Description,
                entry.Value.Duration.TotalMilliseconds));

        return new HealthResponse(report.Status.ToString(), checkedAt, checks);
    }
}

public sealed record HealthCheckEntryResponse(
    string Status,
    string? Description,
    double DurationMilliseconds);
