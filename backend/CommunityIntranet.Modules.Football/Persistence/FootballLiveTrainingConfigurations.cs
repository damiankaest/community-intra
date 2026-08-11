using CommunityIntranet.Modules.Football.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIntranet.Modules.Football.Persistence;

public sealed class FootballLiveTrainingRunConfiguration : IEntityTypeConfiguration<FootballLiveTrainingRun>
{
    public void Configure(EntityTypeBuilder<FootballLiveTrainingRun> builder)
    {
        builder.ToTable("live_training_runs", "football");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.OrganizationId, x.SessionId }).IsUnique();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasOne<FootballSession>()
            .WithMany()
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<FootballTrainingBlock>()
            .WithMany()
            .HasForeignKey(x => x.ActiveTrainingBlockId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class FootballLiveTrainingBlockRunConfiguration : IEntityTypeConfiguration<FootballLiveTrainingBlockRun>
{
    public void Configure(EntityTypeBuilder<FootballLiveTrainingBlockRun> builder)
    {
        builder.ToTable("live_training_block_runs", "football");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.OrganizationId, x.SessionId, x.TrainingBlockId }).IsUnique();
        builder.HasOne<FootballSession>()
            .WithMany()
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<FootballTrainingBlock>()
            .WithMany()
            .HasForeignKey(x => x.TrainingBlockId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
