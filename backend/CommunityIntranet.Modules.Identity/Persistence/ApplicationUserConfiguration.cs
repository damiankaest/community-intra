using CommunityIntranet.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIntranet.Modules.Identity.Persistence;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("users", "identity");
        builder.Property(user => user.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(user => user.AvatarUrl).HasMaxLength(500);
        builder.Property(user => user.CreatedAt).IsRequired();
        builder.Property(user => user.IsActive).IsRequired();
    }
}
