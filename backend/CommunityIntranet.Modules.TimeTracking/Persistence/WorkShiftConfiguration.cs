using CommunityIntranet.Modules.TimeTracking.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIntranet.Modules.TimeTracking.Persistence;

public sealed class WorkShiftConfiguration
    : IEntityTypeConfiguration<WorkShift>
{
    public void Configure(EntityTypeBuilder<WorkShift> builder)
    {
        builder.ToTable("work_shifts", "time_tracking");
        builder.HasKey(shift => shift.Id);
        builder.Property(shift => shift.ConcurrencyToken)
            .IsConcurrencyToken();
        builder.HasIndex(shift => new
        {
            shift.OrganizationId,
            shift.MemberId,
            shift.EndedAt
        });
        builder.HasIndex(shift => new
            {
                shift.OrganizationId,
                shift.MemberId
            })
            .IsUnique()
            .HasFilter("\"EndedAt\" IS NULL");
    }
}
