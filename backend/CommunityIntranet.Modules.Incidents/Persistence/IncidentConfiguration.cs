using CommunityIntranet.Modules.Incidents.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIntranet.Modules.Incidents.Persistence;

public sealed class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.ToTable("incidents", "incidents");
        builder.HasKey(incident => incident.Id);
        builder.Property(incident => incident.Title)
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(incident => incident.Description)
            .HasMaxLength(6000)
            .IsRequired();
        builder.Property(incident => incident.Category)
            .HasMaxLength(120)
            .IsRequired();
        builder.Property(incident => incident.Resolution).HasMaxLength(6000);
        builder.Property(incident => incident.LessonsLearned)
            .HasMaxLength(4000);
        builder.Property(incident => incident.Severity)
            .HasConversion<string>()
            .HasMaxLength(30);
        builder.Property(incident => incident.Status)
            .HasConversion<string>()
            .HasMaxLength(30);
        builder.Property(incident => incident.ConcurrencyToken)
            .IsConcurrencyToken();
        builder.HasIndex(incident => new
        {
            incident.OrganizationId,
            incident.Status
        });
        builder.HasIndex(incident => new
        {
            incident.OrganizationId,
            incident.Severity
        });
    }
}
