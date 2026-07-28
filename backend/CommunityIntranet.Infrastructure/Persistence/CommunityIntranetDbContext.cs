using CommunityIntranet.Modules.Identity.Domain;
using CommunityIntranet.Modules.Identity.Persistence;
using CommunityIntranet.Modules.Members.Domain;
using CommunityIntranet.Modules.Members.Persistence;
using CommunityIntranet.Modules.Organizations.Domain;
using CommunityIntranet.Modules.Organizations.Persistence;
using CommunityIntranet.Modules.ThemePacks.Domain;
using CommunityIntranet.Modules.ThemePacks.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Infrastructure.Persistence;

public sealed class CommunityIntranetDbContext(
    DbContextOptions<CommunityIntranetDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options),
        IIdentityDbContext,
        IOrganizationDbContext,
        IMemberDbContext,
        IThemePackDbContext
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<OrganizationMember> OrganizationMembers =>
        Set<OrganizationMember>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<OrganizationInvitation> OrganizationInvitations =>
        Set<OrganizationInvitation>();

    public DbSet<ThemePack> ThemePacks => Set<ThemePack>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(CommunityIntranetDbContext).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationUser).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(Organization).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(OrganizationMember).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(ThemePack).Assembly);

        builder.Entity<IdentityRole<Guid>>().ToTable("roles", "identity");
        builder.Entity<IdentityUserClaim<Guid>>()
            .ToTable("user_claims", "identity");
        builder.Entity<IdentityUserLogin<Guid>>()
            .ToTable("user_logins", "identity");
        builder.Entity<IdentityUserRole<Guid>>()
            .ToTable("user_roles", "identity");
        builder.Entity<IdentityRoleClaim<Guid>>()
            .ToTable("role_claims", "identity");
        builder.Entity<IdentityUserToken<Guid>>()
            .ToTable("user_tokens", "identity");

        builder.Entity<Organization>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(organization => organization.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OrganizationMember>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(member => member.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Organization>()
            .HasOne<ThemePack>()
            .WithMany()
            .HasForeignKey(organization => organization.ThemePackId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
