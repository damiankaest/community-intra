using System.Security.Claims;
using CommunityIntranet.BuildingBlocks.ActivityFeed;
using CommunityIntranet.BuildingBlocks.Authorization;
using CommunityIntranet.BuildingBlocks.Tenancy;
using CommunityIntranet.Modules.Members.Contracts;
using CommunityIntranet.Modules.Members.Domain;
using CommunityIntranet.Modules.Members.Persistence;
using CommunityIntranet.Modules.Members.Services;
using CommunityIntranet.Modules.Organizations.Persistence;
using CommunityIntranet.Modules.ThemePacks.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.Members.Endpoints;

public static class MemberEndpoints
{
    public static IEndpointRouteBuilder MapMemberEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var organizationGroup = endpoints
            .MapGroup("/api/organizations/{organizationId:guid}")
            .WithTags("Members")
            .RequireAuthorization();

        organizationGroup.MapGet("/members", ListMembersAsync);
        organizationGroup.MapGet("/members/{memberId:guid}", GetMemberAsync);
        organizationGroup.MapPatch("/members/{memberId:guid}", UpdateMemberAsync);
        organizationGroup.MapGet("/departments", ListDepartmentsAsync);
        organizationGroup.MapPost("/departments", CreateDepartmentAsync);
        organizationGroup.MapPut(
            "/departments/{departmentId:guid}",
            UpdateDepartmentAsync);
        organizationGroup.MapDelete(
            "/departments/{departmentId:guid}",
            ArchiveDepartmentAsync);
        organizationGroup.MapGet("/invitations", ListInvitationsAsync);
        organizationGroup.MapPost("/invitations", CreateInvitationAsync)
            .RequireRateLimiting("invitations");
        organizationGroup.MapDelete(
            "/invitations/{invitationId:guid}",
            RevokeInvitationAsync);

        var invitationGroup = endpoints
            .MapGroup("/api/invitations")
            .WithTags("Invitations")
            .RequireRateLimiting("invitations");
        invitationGroup.MapPost("/resolve", ResolveInvitationAsync)
            .AllowAnonymous();
        invitationGroup.MapPost("/accept", AcceptInvitationAsync)
            .RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> ListMembersAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        IMemberDbContext dbContext,
        IOrganizationAccessService accessService,
        CancellationToken cancellationToken)
    {
        var accessResult = await GetMembershipAsync(
            organizationId,
            principal,
            accessService,
            cancellationToken);
        if (accessResult.Result is not null)
        {
            return accessResult.Result;
        }

        var members = await (
            from member in dbContext.OrganizationMembers.AsNoTracking()
            join user in dbContext.Users.AsNoTracking()
                on member.UserId equals user.Id
            join department in dbContext.Departments.AsNoTracking()
                on member.DepartmentId equals (Guid?)department.Id
                into departments
            from department in departments.DefaultIfEmpty()
            where member.OrganizationId == organizationId
            orderby member.IsActive descending, user.DisplayName
            select new MemberResponse(
                member.Id,
                member.UserId,
                user.DisplayName,
                user.Email ?? string.Empty,
                user.AvatarUrl,
                member.PermissionRole,
                member.VisibleTitle,
                member.DepartmentId,
                department == null ? null : department.Name,
                member.StatusMessage,
                member.JoinedAt,
                member.IsActive))
            .ToArrayAsync(cancellationToken);

        return Results.Ok(members);
    }

    private static async Task<IResult> GetMemberAsync(
        Guid organizationId,
        Guid memberId,
        ClaimsPrincipal principal,
        IMemberDbContext dbContext,
        IOrganizationAccessService accessService,
        CancellationToken cancellationToken)
    {
        var accessResult = await GetMembershipAsync(
            organizationId,
            principal,
            accessService,
            cancellationToken);
        if (accessResult.Result is not null)
        {
            return accessResult.Result;
        }

        var member = await (
            from membership in dbContext.OrganizationMembers.AsNoTracking()
            join user in dbContext.Users.AsNoTracking()
                on membership.UserId equals user.Id
            join department in dbContext.Departments.AsNoTracking()
                on membership.DepartmentId equals (Guid?)department.Id
                into departments
            from department in departments.DefaultIfEmpty()
            where membership.OrganizationId == organizationId
                && membership.Id == memberId
            select new MemberResponse(
                membership.Id,
                membership.UserId,
                user.DisplayName,
                user.Email ?? string.Empty,
                user.AvatarUrl,
                membership.PermissionRole,
                membership.VisibleTitle,
                membership.DepartmentId,
                department == null ? null : department.Name,
                membership.StatusMessage,
                membership.JoinedAt,
                membership.IsActive))
            .SingleOrDefaultAsync(cancellationToken);

        return member is null ? Results.NotFound() : Results.Ok(member);
    }

    private static async Task<IResult> UpdateMemberAsync(
        Guid organizationId,
        Guid memberId,
        UpdateMemberRequest request,
        ClaimsPrincipal principal,
        IMemberDbContext dbContext,
        IOrganizationAccessService accessService,
        IActivityWriter activityWriter,
        CancellationToken cancellationToken)
    {
        var accessResult = await GetMembershipAsync(
            organizationId,
            principal,
            accessService,
            cancellationToken);
        if (accessResult.Result is not null)
        {
            return accessResult.Result;
        }

        var caller = accessResult.Membership!;
        if (!caller.PermissionRole.CanManageOrganization())
        {
            return Results.Forbid();
        }

        if (!TryGetUserId(principal, out var callerUserId))
        {
            return Results.Unauthorized();
        }

        var member = await dbContext.OrganizationMembers.SingleOrDefaultAsync(
            item => item.Id == memberId && item.OrganizationId == organizationId,
            cancellationToken);
        if (member is null)
        {
            return Results.NotFound();
        }

        var validation = await ValidateMemberUpdateAsync(
            request,
            member,
            caller,
            callerUserId,
            organizationId,
            dbContext,
            cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        var previousTitle = member.VisibleTitle;
        member.PermissionRole = request.PermissionRole;
        member.VisibleTitle = NormalizeOptional(request.VisibleTitle);
        member.DepartmentId = request.DepartmentId;
        member.StatusMessage = NormalizeOptional(request.StatusMessage);
        member.IsActive = request.IsActive;
        if (!string.Equals(
                previousTitle,
                member.VisibleTitle,
                StringComparison.Ordinal))
        {
            activityWriter.Add(new ActivityDraft(
                organizationId,
                "member.title-changed",
                caller.MemberId,
                "member",
                member.Id,
                new Dictionary<string, string?>
                {
                    ["visibleTitle"] = member.VisibleTitle
                }));
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult?> ValidateMemberUpdateAsync(
        UpdateMemberRequest request,
        OrganizationMember target,
        OrganizationMembership caller,
        Guid callerUserId,
        Guid organizationId,
        IMemberDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.PermissionRole))
        {
            return ValidationProblem(
                "PermissionRole",
                "The permission role is invalid.");
        }

        if (NormalizeOptional(request.VisibleTitle)?.Length > 100)
        {
            return ValidationProblem(
                "VisibleTitle",
                "The visible title may contain at most 100 characters.");
        }

        if (NormalizeOptional(request.StatusMessage)?.Length > 280)
        {
            return ValidationProblem(
                "StatusMessage",
                "The status message may contain at most 280 characters.");
        }

        if (target.PermissionRole == PermissionRole.Owner
            && (request.PermissionRole != PermissionRole.Owner
                || !request.IsActive))
        {
            return ValidationProblem(
                "PermissionRole",
                "The owner cannot be demoted or deactivated.");
        }

        if (target.PermissionRole != PermissionRole.Owner
            && request.PermissionRole == PermissionRole.Owner)
        {
            return ValidationProblem(
                "PermissionRole",
                "Ownership transfer is not supported yet.");
        }

        if (caller.PermissionRole != PermissionRole.Owner
            && (target.PermissionRole == PermissionRole.Administrator
                || request.PermissionRole == PermissionRole.Administrator))
        {
            return Results.Forbid();
        }

        if (target.UserId == callerUserId && !request.IsActive)
        {
            return ValidationProblem(
                "IsActive",
                "You cannot deactivate your own membership.");
        }

        if (request.DepartmentId is not null
            && !await dbContext.Departments.AnyAsync(
                department =>
                    department.Id == request.DepartmentId
                    && department.OrganizationId == organizationId
                    && !department.IsArchived,
                cancellationToken))
        {
            return ValidationProblem(
                "DepartmentId",
                "The selected department does not exist.");
        }

        return null;
    }

    private static async Task<IResult> ListDepartmentsAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        IMemberDbContext dbContext,
        IOrganizationAccessService accessService,
        CancellationToken cancellationToken)
    {
        var accessResult = await GetMembershipAsync(
            organizationId,
            principal,
            accessService,
            cancellationToken);
        if (accessResult.Result is not null)
        {
            return accessResult.Result;
        }

        var departments = await dbContext.Departments
            .AsNoTracking()
            .Where(department =>
                department.OrganizationId == organizationId
                && !department.IsArchived)
            .OrderBy(department => department.SortOrder)
            .ThenBy(department => department.Name)
            .Select(department => new DepartmentResponse(
                department.Id,
                department.Name,
                department.Description,
                department.SortOrder,
                department.Icon,
                department.IsArchived))
            .ToArrayAsync(cancellationToken);

        return Results.Ok(departments);
    }

    private static async Task<IResult> CreateDepartmentAsync(
        Guid organizationId,
        CreateDepartmentRequest request,
        ClaimsPrincipal principal,
        IMemberDbContext dbContext,
        IOrganizationAccessService accessService,
        CancellationToken cancellationToken)
    {
        var accessResult = await GetManagerAsync(
            organizationId,
            principal,
            accessService,
            cancellationToken);
        if (accessResult.Result is not null)
        {
            return accessResult.Result;
        }

        var validation = ValidateDepartment(request.Name, request.Description, request.Icon);
        if (validation is not null)
        {
            return validation;
        }

        var normalizedName = request.Name.Trim();
        if (await dbContext.Departments.AnyAsync(
            department =>
                department.OrganizationId == organizationId
                && department.Name == normalizedName,
            cancellationToken))
        {
            return ValidationProblem(
                "Name",
                "A department with this name already exists.");
        }

        var nextSortOrder = await dbContext.Departments
            .Where(department => department.OrganizationId == organizationId)
            .Select(department => (int?)department.SortOrder)
            .MaxAsync(cancellationToken) + 1 ?? 0;
        var department = new Department
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = normalizedName,
            Description = NormalizeOptional(request.Description),
            Icon = request.Icon.Trim(),
            SortOrder = nextSortOrder,
            IsArchived = false
        };
        dbContext.Departments.Add(department);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/api/organizations/{organizationId}/departments/{department.Id}",
            ToDepartmentResponse(department));
    }

    private static async Task<IResult> UpdateDepartmentAsync(
        Guid organizationId,
        Guid departmentId,
        UpdateDepartmentRequest request,
        ClaimsPrincipal principal,
        IMemberDbContext dbContext,
        IOrganizationAccessService accessService,
        CancellationToken cancellationToken)
    {
        var accessResult = await GetManagerAsync(
            organizationId,
            principal,
            accessService,
            cancellationToken);
        if (accessResult.Result is not null)
        {
            return accessResult.Result;
        }

        var validation = ValidateDepartment(request.Name, request.Description, request.Icon);
        if (validation is not null)
        {
            return validation;
        }

        var department = await dbContext.Departments.SingleOrDefaultAsync(
            item =>
                item.Id == departmentId
                && item.OrganizationId == organizationId
                && !item.IsArchived,
            cancellationToken);
        if (department is null)
        {
            return Results.NotFound();
        }

        var normalizedName = request.Name.Trim();
        if (await dbContext.Departments.AnyAsync(
            item =>
                item.OrganizationId == organizationId
                && item.Id != departmentId
                && item.Name == normalizedName,
            cancellationToken))
        {
            return ValidationProblem(
                "Name",
                "A department with this name already exists.");
        }

        department.Name = normalizedName;
        department.Description = NormalizeOptional(request.Description);
        department.Icon = request.Icon.Trim();
        department.SortOrder = request.SortOrder;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(ToDepartmentResponse(department));
    }

    private static async Task<IResult> ArchiveDepartmentAsync(
        Guid organizationId,
        Guid departmentId,
        ClaimsPrincipal principal,
        IMemberDbContext dbContext,
        IOrganizationAccessService accessService,
        CancellationToken cancellationToken)
    {
        var accessResult = await GetManagerAsync(
            organizationId,
            principal,
            accessService,
            cancellationToken);
        if (accessResult.Result is not null)
        {
            return accessResult.Result;
        }

        var department = await dbContext.Departments.SingleOrDefaultAsync(
            item =>
                item.Id == departmentId
                && item.OrganizationId == organizationId
                && !item.IsArchived,
            cancellationToken);
        if (department is null)
        {
            return Results.NotFound();
        }

        department.IsArchived = true;
        var members = await dbContext.OrganizationMembers
            .Where(member =>
                member.OrganizationId == organizationId
                && member.DepartmentId == departmentId)
            .ToListAsync(cancellationToken);
        foreach (var member in members)
        {
            member.DepartmentId = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ListInvitationsAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        IMemberDbContext dbContext,
        IOrganizationAccessService accessService,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var accessResult = await GetManagerAsync(
            organizationId,
            principal,
            accessService,
            cancellationToken);
        if (accessResult.Result is not null)
        {
            return accessResult.Result;
        }

        var now = timeProvider.GetUtcNow();
        var invitations = await (
            from invitation in dbContext.OrganizationInvitations.AsNoTracking()
            join user in dbContext.Users.AsNoTracking()
                on invitation.CreatedByUserId equals user.Id
            where invitation.OrganizationId == organizationId
            orderby invitation.CreatedAt descending
            select new InvitationResponse(
                invitation.Id,
                user.DisplayName,
                invitation.DefaultPermissionRole,
                invitation.CreatedAt,
                invitation.ExpiresAt,
                invitation.MaximumUses,
                invitation.CurrentUses,
                invitation.IsRevoked,
                !invitation.IsRevoked
                    && invitation.ExpiresAt > now
                    && invitation.CurrentUses < invitation.MaximumUses))
            .ToArrayAsync(cancellationToken);

        return Results.Ok(invitations);
    }

    private static async Task<IResult> CreateInvitationAsync(
        Guid organizationId,
        CreateInvitationRequest request,
        ClaimsPrincipal principal,
        IMemberDbContext dbContext,
        IOrganizationAccessService accessService,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var accessResult = await GetManagerAsync(
            organizationId,
            principal,
            accessService,
            cancellationToken);
        if (accessResult.Result is not null)
        {
            return accessResult.Result;
        }

        var caller = accessResult.Membership!;
        if (!TryGetUserId(principal, out var userId))
        {
            return Results.Unauthorized();
        }

        var validation = ValidateInvitation(request, caller.PermissionRole);
        if (validation is not null)
        {
            return validation;
        }

        var now = timeProvider.GetUtcNow();
        var token = InvitationTokenService.Create();
        var invitation = new OrganizationInvitation
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            TokenHash = token.TokenHash,
            CreatedByUserId = userId,
            DefaultPermissionRole = request.DefaultPermissionRole,
            CreatedAt = now,
            ExpiresAt = now.AddDays(request.ExpiresInDays),
            MaximumUses = request.MaximumUses,
            CurrentUses = 0,
            IsRevoked = false
        };
        dbContext.OrganizationInvitations.Add(invitation);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/api/organizations/{organizationId}/invitations/{invitation.Id}",
            new CreatedInvitationResponse(
                invitation.Id,
                token.RawToken,
                invitation.DefaultPermissionRole,
                invitation.ExpiresAt,
                invitation.MaximumUses));
    }

    private static async Task<IResult> RevokeInvitationAsync(
        Guid organizationId,
        Guid invitationId,
        ClaimsPrincipal principal,
        IMemberDbContext dbContext,
        IOrganizationAccessService accessService,
        CancellationToken cancellationToken)
    {
        var accessResult = await GetManagerAsync(
            organizationId,
            principal,
            accessService,
            cancellationToken);
        if (accessResult.Result is not null)
        {
            return accessResult.Result;
        }

        var invitation = await dbContext.OrganizationInvitations.SingleOrDefaultAsync(
            item =>
                item.Id == invitationId
                && item.OrganizationId == organizationId,
            cancellationToken);
        if (invitation is null)
        {
            return Results.NotFound();
        }

        invitation.IsRevoked = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ResolveInvitationAsync(
        ResolveInvitationRequest request,
        IMemberDbContext dbContext,
        IOrganizationDbContext organizationDbContext,
        IThemePackCatalog themePackCatalog,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (!TryHashToken(request.Token, out var tokenHash))
        {
            return InvalidInvitation();
        }

        var invitation = await dbContext.OrganizationInvitations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.TokenHash == tokenHash,
                cancellationToken);
        if (invitation is null || !IsUsable(invitation, timeProvider.GetUtcNow()))
        {
            return InvalidInvitation();
        }

        var organization = await organizationDbContext.Organizations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item =>
                    item.Id == invitation.OrganizationId
                    && !item.IsArchived,
                cancellationToken);
        if (organization is null)
        {
            return InvalidInvitation();
        }

        var themePack = organization.ThemePackId is null
            ? null
            : await themePackCatalog.FindByIdAsync(
                organization.ThemePackId.Value,
                cancellationToken);

        return Results.Ok(new InvitationPreviewResponse(
            invitation.Id,
            organization.Id,
            organization.Name,
            themePack?.Key ?? "generic-corporate",
            invitation.DefaultPermissionRole,
            invitation.ExpiresAt,
            invitation.MaximumUses - invitation.CurrentUses));
    }

    private static async Task<IResult> AcceptInvitationAsync(
        AcceptInvitationRequest request,
        ClaimsPrincipal principal,
        IMemberDbContext dbContext,
        IOrganizationDbContext organizationDbContext,
        IActivityWriter activityWriter,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
        {
            return Results.Unauthorized();
        }

        if (!TryHashToken(request.Token, out var tokenHash))
        {
            return InvalidInvitation();
        }

        var invitation = await dbContext.OrganizationInvitations
            .SingleOrDefaultAsync(
                item => item.TokenHash == tokenHash,
                cancellationToken);
        var now = timeProvider.GetUtcNow();
        if (invitation is null || !IsUsable(invitation, now))
        {
            return InvalidInvitation();
        }

        var organization = await organizationDbContext.Organizations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item =>
                    item.Id == invitation.OrganizationId
                    && !item.IsArchived,
                cancellationToken);
        if (organization is null)
        {
            return InvalidInvitation();
        }

        var membership = await dbContext.OrganizationMembers.SingleOrDefaultAsync(
            member =>
                member.OrganizationId == invitation.OrganizationId
                && member.UserId == userId,
            cancellationToken);
        if (membership?.IsActive == true)
        {
            return Results.Conflict(new
            {
                title = "Membership already exists",
                detail = "You are already an active member of this organization."
            });
        }

        if (membership is null)
        {
            membership = new OrganizationMember
            {
                Id = Guid.NewGuid(),
                OrganizationId = invitation.OrganizationId,
                UserId = userId,
                PermissionRole = invitation.DefaultPermissionRole,
                JoinedAt = now,
                IsActive = true
            };
            dbContext.OrganizationMembers.Add(membership);
        }
        else
        {
            membership.PermissionRole = invitation.DefaultPermissionRole;
            membership.JoinedAt = now;
            membership.IsActive = true;
        }

        invitation.CurrentUses++;
        var displayName = await dbContext.Users
            .Where(user => user.Id == userId)
            .Select(user => user.DisplayName)
            .SingleAsync(cancellationToken);
        activityWriter.Add(new ActivityDraft(
            invitation.OrganizationId,
            "member.joined",
            membership.Id,
            "member",
            membership.Id,
            new Dictionary<string, string?>
            {
                ["memberName"] = displayName
            }));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new
            {
                title = "Invitation was used concurrently",
                detail = "Please check the invitation again."
            });
        }

        return Results.Ok(new AcceptedInvitationResponse(
            organization.Id,
            organization.Name,
            membership.Id,
            membership.PermissionRole));
    }

    private static IResult? ValidateInvitation(
        CreateInvitationRequest request,
        PermissionRole callerRole)
    {
        if (!Enum.IsDefined(request.DefaultPermissionRole)
            || request.DefaultPermissionRole == PermissionRole.Owner)
        {
            return ValidationProblem(
                "DefaultPermissionRole",
                "Invitations cannot grant this permission role.");
        }

        if (callerRole != PermissionRole.Owner
            && request.DefaultPermissionRole == PermissionRole.Administrator)
        {
            return Results.Forbid();
        }

        if (request.ExpiresInDays is < 1 or > 30)
        {
            return ValidationProblem(
                "ExpiresInDays",
                "Invitations must expire after 1 to 30 days.");
        }

        if (request.MaximumUses is < 1 or > 100)
        {
            return ValidationProblem(
                "MaximumUses",
                "Invitations may be used between 1 and 100 times.");
        }

        return null;
    }

    private static IResult? ValidateDepartment(
        string name,
        string? description,
        string icon)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100)
        {
            return ValidationProblem(
                "Name",
                "The department name must contain 1 to 100 characters.");
        }

        if (NormalizeOptional(description)?.Length > 500)
        {
            return ValidationProblem(
                "Description",
                "The description may contain at most 500 characters.");
        }

        if (string.IsNullOrWhiteSpace(icon) || icon.Trim().Length > 50)
        {
            return ValidationProblem(
                "Icon",
                "The icon must contain 1 to 50 characters.");
        }

        return null;
    }

    private static DepartmentResponse ToDepartmentResponse(Department department) =>
        new(
            department.Id,
            department.Name,
            department.Description,
            department.SortOrder,
            department.Icon,
            department.IsArchived);

    private static bool IsUsable(
        OrganizationInvitation invitation,
        DateTimeOffset now) =>
        !invitation.IsRevoked
        && invitation.ExpiresAt > now
        && invitation.CurrentUses < invitation.MaximumUses;

    private static bool TryHashToken(string? rawToken, out string tokenHash)
    {
        tokenHash = string.Empty;
        if (string.IsNullOrWhiteSpace(rawToken) || rawToken.Length > 200)
        {
            return false;
        }

        tokenHash = InvitationTokenService.Hash(rawToken.Trim());
        return true;
    }

    private static IResult InvalidInvitation() =>
        Results.Problem(
            title: "Invitation unavailable",
            detail: "The invitation is invalid, expired, revoked, or fully used.",
            statusCode: StatusCodes.Status410Gone);

    private static IResult ValidationProblem(string key, string message) =>
        Results.ValidationProblem(
            new Dictionary<string, string[]> { [key] = [message] });

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static async Task<MembershipResult> GetManagerAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        IOrganizationAccessService accessService,
        CancellationToken cancellationToken)
    {
        var accessResult = await GetMembershipAsync(
            organizationId,
            principal,
            accessService,
            cancellationToken);
        if (accessResult.Result is not null)
        {
            return accessResult;
        }

        return accessResult.Membership!.PermissionRole.CanManageOrganization()
            ? accessResult
            : new MembershipResult(null, Results.Forbid());
    }

    private static async Task<MembershipResult> GetMembershipAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        IOrganizationAccessService accessService,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
        {
            return new MembershipResult(null, Results.Unauthorized());
        }

        var membership = await accessService.GetActiveMembershipAsync(
            organizationId,
            userId,
            cancellationToken);
        return membership is null
            ? new MembershipResult(null, Results.NotFound())
            : new MembershipResult(membership, null);
    }

    private static bool TryGetUserId(
        ClaimsPrincipal principal,
        out Guid userId)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        return Guid.TryParse(value, out userId);
    }

    private sealed record MembershipResult(
        OrganizationMembership? Membership,
        IResult? Result);
}
