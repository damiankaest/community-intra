using CommunityIntranet.Modules.AiAssistant.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIntranet.Modules.AiAssistant.Persistence;

public sealed class AssistantConversationConfiguration
    : IEntityTypeConfiguration<AssistantConversation>
{
    public void Configure(EntityTypeBuilder<AssistantConversation> builder)
    {
        builder.ToTable("conversations", "ai");
        builder.HasKey(conversation => conversation.Id);
        builder.Property(conversation => conversation.Title)
            .HasMaxLength(120);
        builder.Property(conversation => conversation.Tone)
            .HasConversion<string>()
            .HasMaxLength(24);
        builder.HasIndex(conversation => new
        {
            conversation.OrganizationId,
            conversation.MemberId,
            conversation.ArchivedAt,
            conversation.UpdatedAt
        });
    }
}

public sealed class AssistantMessageConfiguration
    : IEntityTypeConfiguration<AssistantMessage>
{
    public void Configure(EntityTypeBuilder<AssistantMessage> builder)
    {
        builder.ToTable("messages", "ai");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Role)
            .HasConversion<string>()
            .HasMaxLength(24);
        builder.Property(message => message.Content)
            .HasMaxLength(12000)
            .IsRequired();
        builder.Property(message => message.Model).HasMaxLength(100);
        builder.HasIndex(message => new
        {
            message.OrganizationId,
            message.ConversationId,
            message.CreatedAt
        });
    }
}
