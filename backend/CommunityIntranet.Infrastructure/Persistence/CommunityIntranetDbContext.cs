using CommunityIntranet.Modules.Identity.Domain;
using CommunityIntranet.Modules.Identity.Persistence;
using CommunityIntranet.Modules.Members.Domain;
using CommunityIntranet.Modules.Members.Persistence;
using CommunityIntranet.Modules.Organizations.Domain;
using CommunityIntranet.Modules.Organizations.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Infrastructure.Persistence;

public sealed class CommunityIntranetDbContext(
    DbContextOptions<CommunityIntranetDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options),
        IIdentityDbContext,
        IOrganizationDbContext,
        IMemberDbContext
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<OrganizationMember> OrganizationMembers =>
        Set<OrganizationMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CommunityIntranetDbContext).Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationUser).Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Organization).Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrganizationMember).Assembly);

        modelBuilder.Entity<IdentityRole<Guid>>().ToTable("roles", "identity");
        modelBuilder.Entity<IdentityUserClaim<Guid>>()
            .ToTable("user_claims", "identity");
        modelBuilder.Entity<IdentityUserLogin<Guid>>()
            .ToTable("user_logins", "identity");
        modelBuilder.Entity<IdentityUserRole<Guid>>()
            .ToTable("user_roles", "identity");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>()
            .ToTable("role_claims", "identity");
        modelBuilder.Entity<IdentityUserToken<Guid>>()
            .ToTable("user_tokens", "identity");

        modelBuilder.Entity<Organization>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(organization => organization.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<OrganizationMember>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(member => member.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
