using CommunityIntranet.Modules.Members.Domain;
using CommunityIntranet.Modules.Organizations.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIntranet.Modules.Members.Persistence;

public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments", "members");
        builder.HasKey(department => department.Id);
        builder.Property(department => department.Name)
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(department => department.Description).HasMaxLength(500);
        builder.Property(department => department.Icon)
            .HasMaxLength(50)
            .IsRequired();
        builder.HasIndex(department => new
        {
            department.OrganizationId,
            department.Name
        }).IsUnique();
        builder.HasIndex(department => new
        {
            department.OrganizationId,
            department.IsArchived,
            department.SortOrder
        });
        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(department => department.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
