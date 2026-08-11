using CommunityIntranet.Modules.Football.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIntranet.Modules.Football.Persistence;

public sealed class FootballMemberProfileConfiguration : IEntityTypeConfiguration<FootballMemberProfile>
{
    public void Configure(EntityTypeBuilder<FootballMemberProfile> builder)
    {
        builder.ToTable("member_profiles", "football");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.OrganizationId, x.MemberId }).IsUnique();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.TeamRole).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Position).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Strengths).HasColumnType("text[]");
        builder.Property(x => x.DevelopmentAreas).HasColumnType("text[]");
        builder.Property(x => x.SecondaryPositions).HasColumnType("text[]");
    }
}

public sealed class FootballPlayerAvailabilityConfiguration : IEntityTypeConfiguration<FootballPlayerAvailability>
{
    public void Configure(EntityTypeBuilder<FootballPlayerAvailability> builder)
    {
        builder.ToTable("player_availability", "football");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.OrganizationId, x.MemberId }).IsUnique();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Note).HasMaxLength(500);
    }
}

public sealed class FootballExerciseConfiguration : IEntityTypeConfiguration<FootballExercise>
{
    public void Configure(EntityTypeBuilder<FootballExercise> builder)
    {
        builder.ToTable("exercises", "football");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.OrganizationId, x.IsArchived, x.Category });
        builder.Property(x => x.Title).HasMaxLength(160);
        builder.Property(x => x.Description).HasMaxLength(3000);
        builder.Property(x => x.Focus).HasMaxLength(1000);
        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Location).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Intensity).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Equipment).HasColumnType("text[]");
        builder.Property(x => x.Tags).HasColumnType("text[]");
    }
}

public sealed class FootballSessionConfiguration : IEntityTypeConfiguration<FootballSession>
{
    public void Configure(EntityTypeBuilder<FootballSession> builder)
    {
        builder.ToTable("sessions", "football");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.OrganizationId, x.StartsAt });
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Title).HasMaxLength(180);
        builder.Property(x => x.Focus).HasMaxLength(1000);
        builder.Property(x => x.Location).HasMaxLength(300);
        builder.Property(x => x.Opponent).HasMaxLength(180);
    }
}

public sealed class FootballAttendanceConfiguration : IEntityTypeConfiguration<FootballAttendance>
{
    public void Configure(EntityTypeBuilder<FootballAttendance> builder)
    {
        builder.ToTable("attendance", "football");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.OrganizationId, x.SessionId, x.MemberId }).IsUnique();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Note).HasMaxLength(500);
    }
}

public sealed class FootballSessionLoadConfiguration : IEntityTypeConfiguration<FootballSessionLoad>
{
    public void Configure(EntityTypeBuilder<FootballSessionLoad> builder)
    {
        builder.ToTable("session_load", "football");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.OrganizationId, x.SessionId, x.MemberId }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.MemberId, x.UpdatedAt });
        builder.Property(x => x.Note).HasMaxLength(500);
    }
}

public sealed class FootballTrainingBlockConfiguration : IEntityTypeConfiguration<FootballTrainingBlock>
{
    public void Configure(EntityTypeBuilder<FootballTrainingBlock> builder)
    {
        builder.ToTable("training_blocks", "football");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.OrganizationId, x.SessionId, x.SortOrder });
        builder.Property(x => x.Title).HasMaxLength(180);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.CoachingPoints).HasMaxLength(2000);
        builder.Property(x => x.AiReason).HasMaxLength(1500);
    }
}

public sealed class FootballExerciseFeedbackConfiguration : IEntityTypeConfiguration<FootballExerciseFeedback>
{
    public void Configure(EntityTypeBuilder<FootballExerciseFeedback> builder)
    {
        builder.ToTable("exercise_feedback", "football");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.OrganizationId, x.SessionId, x.ExerciseId, x.MemberId }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.ExerciseId, x.UpdatedAt });
        builder.Property(x => x.Comment).HasMaxLength(1000);
    }
}
