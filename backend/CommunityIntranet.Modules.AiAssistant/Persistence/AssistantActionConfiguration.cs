using CommunityIntranet.Modules.AiAssistant.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIntranet.Modules.AiAssistant.Persistence;

public sealed class AssistantActionConfiguration
    : IEntityTypeConfiguration<AssistantAction>
{
    public void Configure(EntityTypeBuilder<AssistantAction> builder)
    {
        builder.ToTable("actions", "ai");
        builder.HasKey(action => action.Id);
        builder.Property(action => action.Kind)
            .HasConversion<string>()
            .HasMaxLength(40);
        builder.Property(action => action.Status)
            .HasConversion<string>()
            .HasMaxLength(24);
        builder.Property(action => action.PayloadJson).HasColumnType("jsonb");
        builder.Property(action => action.ConcurrencyToken)
            .IsConcurrencyToken();
        builder.HasIndex(action => new
        {
            action.OrganizationId,
            action.ConversationId,
            action.Status,
            action.CreatedAt
        });
    }
}
