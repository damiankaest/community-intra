namespace CommunityIntranet.Modules.LiveOperations.Services;

public interface IGameServerTokenProtector
{
    string Protect(Guid organizationId, string apiToken);

    string Unprotect(Guid organizationId, string protectedApiToken);
}
