using CommunityIntranet.Modules.TimeTracking.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIntranet.Modules.TimeTracking.Persistence;

public sealed class WorkLogEntryConfiguration
    : IEntityTypeConfiguration<WorkLogEntry>
{
    public void Configure(EntityTypeBuilder<WorkLogEntry> builder)
    {
        builder.ToTable("work_log_entries", "time_tracking");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Kind)
            .HasConversion<string>()
            .HasMaxLength(24);
        builder.Property(entry => entry.Note)
            .HasMaxLength(240)
            .IsRequired();
        builder.HasIndex(entry => new
        {
            entry.OrganizationId,
            entry.CreatedAt
        });
    }
}
