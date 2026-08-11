using CommunityIntranet.Modules.Football.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIntranet.Modules.Football.Persistence;

public sealed class FootballTrainingCoachTaskConfiguration : IEntityTypeConfiguration<FootballTrainingCoachTask>
{
    public void Configure(EntityTypeBuilder<FootballTrainingCoachTask> builder)
    {
        builder.ToTable("training_coach_tasks", "football");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.OrganizationId, x.SessionId, x.TrainingBlockId, x.SortOrder });
        builder.Property(x => x.Role).HasMaxLength(120);
        builder.Property(x => x.Task).HasMaxLength(1000);
        builder.HasOne<FootballTrainingBlock>()
            .WithMany()
            .HasForeignKey(x => x.TrainingBlockId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
