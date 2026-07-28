namespace CommunityIntranet.Api.Models;

public sealed record SystemInfoResponse(
    string Name,
    string Version,
    string Environment,
    string Status);
