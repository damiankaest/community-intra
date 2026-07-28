using CommunityIntranet.Modules.Projects.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIntranet.Modules.Projects.Persistence;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects", "projects");
        builder.HasKey(project => project.Id);
        builder.Property(project => project.Name).HasMaxLength(160).IsRequired();
        builder.Property(project => project.Description).HasMaxLength(4000);
        builder.Property(project => project.Status)
            .HasConversion<string>()
            .HasMaxLength(30);
        builder.Property(project => project.Priority)
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(project => project.ConcurrencyToken)
            .IsConcurrencyToken();
        builder.HasIndex(project => new
        {
            project.OrganizationId,
            project.Status
        });
        builder.HasIndex(project => new
        {
            project.OrganizationId,
            project.OwnerMemberId
        });
    }
}
