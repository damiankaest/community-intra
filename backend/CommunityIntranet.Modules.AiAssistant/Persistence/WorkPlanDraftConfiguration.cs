using CommunityIntranet.Modules.AiAssistant.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIntranet.Modules.AiAssistant.Persistence;

public sealed class WorkPlanDraftConfiguration
    : IEntityTypeConfiguration<WorkPlanDraft>
{
    public void Configure(EntityTypeBuilder<WorkPlanDraft> builder)
    {
        builder.ToTable("work_plan_drafts", "ai");
        builder.HasKey(draft => draft.Id);
        builder.Property(draft => draft.Prompt).HasMaxLength(2000);
        builder.Property(draft => draft.Tone).HasConversion<string>().HasMaxLength(24);
        builder.Property(draft => draft.ProposalJson).HasColumnType("jsonb");
        builder.Property(draft => draft.Model).HasMaxLength(100);
        builder.Property(draft => draft.ConcurrencyToken).IsConcurrencyToken();
        builder.HasIndex(draft => new
        {
            draft.OrganizationId,
            draft.CreatedByMemberId,
            draft.CreatedAt
        });
    }
}
