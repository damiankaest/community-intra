namespace CommunityIntranet.BuildingBlocks.ActivityFeed;

public interface IActivityWriter
{
    void Add(ActivityDraft activity);
}
