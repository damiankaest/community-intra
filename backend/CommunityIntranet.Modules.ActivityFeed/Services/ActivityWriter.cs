using System.Text.Json;
using CommunityIntranet.BuildingBlocks.ActivityFeed;
using CommunityIntranet.Modules.ActivityFeed.Domain;
using CommunityIntranet.Modules.ActivityFeed.Persistence;

namespace CommunityIntranet.Modules.ActivityFeed.Services;

public sealed class ActivityWriter(
    IActivityDbContext dbContext,
    TimeProvider timeProvider) : IActivityWriter
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    public void Add(ActivityDraft activity)
    {
        dbContext.Activities.Add(new ActivityEntry
        {
            Id = Guid.NewGuid(),
            OrganizationId = activity.OrganizationId,
            ActivityType = activity.ActivityType,
            ActorMemberId = activity.ActorMemberId,
            EntityType = activity.EntityType,
            EntityId = activity.EntityId,
            DataJson = JsonSerializer.Serialize(
                activity.Data,
                SerializerOptions),
            EventVersion = 1,
            CreatedAt = timeProvider.GetUtcNow()
        });
    }
}
