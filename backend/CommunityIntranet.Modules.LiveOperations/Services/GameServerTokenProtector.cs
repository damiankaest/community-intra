using Microsoft.AspNetCore.DataProtection;

namespace CommunityIntranet.Modules.LiveOperations.Services;

public sealed class GameServerTokenProtector(
    IDataProtectionProvider dataProtectionProvider)
    : IGameServerTokenProtector
{
    private readonly IDataProtector protector = dataProtectionProvider
        .CreateProtector("CommunityIntranet.LiveOperations.ApiToken.v1");

    public string Protect(Guid organizationId, string apiToken) =>
        protector.Protect($"{organizationId:N}:{apiToken}");

    public string Unprotect(Guid organizationId, string protectedApiToken)
    {
        var value = protector.Unprotect(protectedApiToken);
        var prefix = $"{organizationId:N}:";
        if (!value.StartsWith(prefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The protected server token belongs to another organization.");
        }

        return value[prefix.Length..];
    }
}
