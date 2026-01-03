using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MujDomecek.API.Extensions;
using MujDomecek.Application.DTOs;
using MujDomecek.Domain.Aggregates.Project;
using MujDomecek.Domain.Aggregates.Property;
using MujDomecek.Domain.Aggregates.Zaznam;
using MujDomecek.Domain.ValueObjects;
using MujDomecek.Infrastructure.Persistence;

namespace MujDomecek.API.Endpoints;

public static class SharingEndpoints
{
    public static IEndpointRouteBuilder MapSharingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/sharing").RequireAuthorization();

        group.MapGet("/my-shares", GetMySharesAsync);
        group.MapGet("/shared-with-me", GetSharedWithMeAsync);
        group.MapGet("/pending-invitations", GetPendingInvitationsAsync);

        return endpoints;
    }

    private static async Task<IResult> GetMySharesAsync(
        ClaimsPrincipal user,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var projects = await dbContext.Projects
            .Where(p => p.OwnerId == userId)
            .ToListAsync();

        var projectIds = projects.Select(p => p.Id).ToList();
        var properties = await dbContext.Properties
            .Where(p => projectIds.Contains(p.ProjectId))
            .ToListAsync();

        var projectMembers = await dbContext.ProjectMembers
            .Where(m => projectIds.Contains(m.ProjectId))
            .ToListAsync();

        var propertyIds = properties.Select(p => p.Id).ToList();
        var propertyMembers = await dbContext.PropertyMembers
            .Where(m => propertyIds.Contains(m.PropertyId))
            .ToListAsync();

        var zaznamy = await dbContext.Zaznamy
            .Where(z => propertyIds.Contains(z.PropertyId))
            .ToListAsync();

        var zaznamIds = zaznamy.Select(z => z.Id).ToList();
        var zaznamMembers = await dbContext.ZaznamMembers
            .Where(m => zaznamIds.Contains(m.ZaznamId))
            .ToListAsync();

        var now = DateTime.UtcNow;
        var projectInvites = await dbContext.Invitations
            .Where(i => i.TargetType == InvitationTargetType.Project
                        && projectIds.Contains(i.TargetId)
                        && i.Status == InvitationStatus.Pending
                        && i.ExpiresAt > now)
            .ToListAsync();

        var propertyInvites = await dbContext.Invitations
            .Where(i => i.TargetType == InvitationTargetType.Property
                        && propertyIds.Contains(i.TargetId)
                        && i.Status == InvitationStatus.Pending
                        && i.ExpiresAt > now)
            .ToListAsync();

        var zaznamInvites = await dbContext.Invitations
            .Where(i => i.TargetType == InvitationTargetType.Zaznam
                        && zaznamIds.Contains(i.TargetId)
                        && i.Status == InvitationStatus.Pending
                        && i.ExpiresAt > now)
            .ToListAsync();

        var userIds = projectMembers.Select(m => m.UserId)
            .Concat(propertyMembers.Select(m => m.UserId))
            .Concat(zaznamMembers.Select(m => m.UserId))
            .Concat(projectInvites.Select(i => i.CreatedBy))
            .Concat(propertyInvites.Select(i => i.CreatedBy))
            .Concat(zaznamInvites.Select(i => i.CreatedBy))
            .Concat(projects.Select(p => p.OwnerId))
            .Distinct()
            .ToList();

        var users = await dbContext.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new UserInfo(u.Id, u.Email, u.FirstName, u.LastName))
            .ToListAsync();

        var userLookup = users.ToDictionary(u => u.Id, u => u);

        var items = new List<ShareItemDto>();

        var projectLookup = projects.ToDictionary(p => p.Id, p => p);
        var propertyLookup = properties.ToDictionary(p => p.Id, p => p);

        foreach (var project in projects)
        {
            var members = BuildProjectShareMembers(
                project,
                projectMembers.Where(m => m.ProjectId == project.Id).ToList(),
                projectInvites.Where(i => i.TargetId == project.Id).ToList(),
                userLookup);

            var hasAdditionalMembers = members.Any(m => m.Role != "owner" || m.Status == "pending");
            if (!hasAdditionalMembers)
                continue;

            items.Add(new ShareItemDto(
                "project",
                project.Id,
                project.Name,
                null,
                members));
        }

        foreach (var property in properties)
        {
            var invites = propertyInvites.Where(i => i.TargetId == property.Id).ToList();
            var members = BuildPropertyShareMembers(
                property,
                propertyMembers.Where(m => m.PropertyId == property.Id).ToList(),
                invites,
                projectLookup[property.ProjectId].OwnerId,
                userLookup);

            var hasAdditionalMembers = members.Any(m => m.Role != "owner" || m.Status == "pending");
            if (!hasAdditionalMembers)
                continue;

            items.Add(new ShareItemDto(
                "property",
                property.Id,
                property.Name,
                projectLookup[property.ProjectId].Name,
                members));
        }

        foreach (var zaznam in zaznamy)
        {
            if (!propertyLookup.TryGetValue(zaznam.PropertyId, out var property))
                continue;

            var project = projectLookup[property.ProjectId];
            var invites = zaznamInvites.Where(i => i.TargetId == zaznam.Id).ToList();
            var members = BuildZaznamShareMembers(
                zaznam,
                zaznamMembers.Where(m => m.ZaznamId == zaznam.Id).ToList(),
                invites,
                project.OwnerId,
                userLookup);

            var hasAdditionalMembers = members.Any(m => m.Role != "owner" || m.Status == "pending");
            if (!hasAdditionalMembers)
                continue;

            items.Add(new ShareItemDto(
                "zaznam",
                zaznam.Id,
                zaznam.Title ?? string.Empty,
                property.Name,
                members));
        }

        return Results.Ok(new MySharesResponse(items));
    }

    private static async Task<IResult> GetSharedWithMeAsync(
        ClaimsPrincipal user,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var projectMembers = await dbContext.ProjectMembers
            .Where(m => m.UserId == userId)
            .ToListAsync();

        var projectIds = projectMembers.Select(m => m.ProjectId).ToList();
        var projects = await dbContext.Projects
            .Where(p => projectIds.Contains(p.Id) && p.OwnerId != userId)
            .ToListAsync();

        var propertyMembers = await dbContext.PropertyMembers
            .Where(m => m.UserId == userId)
            .ToListAsync();

        var propertyIds = propertyMembers.Select(m => m.PropertyId).ToList();
        var properties = await dbContext.Properties
            .Where(p => propertyIds.Contains(p.Id))
            .ToListAsync();

        var zaznamMembers = await dbContext.ZaznamMembers
            .Where(m => m.UserId == userId)
            .ToListAsync();

        var zaznamIds = zaznamMembers.Select(m => m.ZaznamId).ToList();
        var zaznamy = await dbContext.Zaznamy
            .Where(z => zaznamIds.Contains(z.Id))
            .ToListAsync();

        var propertyProjectIds = properties.Select(p => p.ProjectId).Distinct().ToList();
        var propertyProjects = await dbContext.Projects
            .Where(p => propertyProjectIds.Contains(p.Id))
            .ToListAsync();

        var zaznamPropertyIds = zaznamy.Select(z => z.PropertyId).Distinct().ToList();
        var zaznamProperties = await dbContext.Properties
            .Where(p => zaznamPropertyIds.Contains(p.Id))
            .ToListAsync();

        var zaznamProjectIds = zaznamProperties.Select(p => p.ProjectId).Distinct().ToList();
        var zaznamProjects = await dbContext.Projects
            .Where(p => zaznamProjectIds.Contains(p.Id))
            .ToListAsync();

        var ownerIds = projects.Select(p => p.OwnerId)
            .Concat(propertyProjects.Select(p => p.OwnerId))
            .Concat(zaznamProjects.Select(p => p.OwnerId))
            .Distinct()
            .ToList();

        var owners = await dbContext.Users
            .Where(u => ownerIds.Contains(u.Id))
            .Select(u => new UserInfo(u.Id, u.Email, u.FirstName, u.LastName))
            .ToListAsync();

        var ownerLookup = owners.ToDictionary(u => u.Id, u => u);
        var projectLookup = projects.ToDictionary(p => p.Id, p => p);
        var propertyProjectLookup = propertyProjects.ToDictionary(p => p.Id, p => p);
        var zaznamLookup = zaznamy.ToDictionary(z => z.Id, z => z);
        var zaznamPropertyLookup = zaznamProperties.ToDictionary(p => p.Id, p => p);
        var zaznamProjectLookup = zaznamProjects.ToDictionary(p => p.Id, p => p);

        var items = new List<SharedWithMeItemDto>();

        foreach (var member in projectMembers)
        {
            if (!projectLookup.TryGetValue(member.ProjectId, out var project))
                continue;

            if (!ownerLookup.TryGetValue(project.OwnerId, out var owner))
                continue;

            items.Add(new SharedWithMeItemDto(
                "project",
                project.Id,
                project.Name,
                new SharedOwnerDto(owner.Id, owner.Email ?? string.Empty, BuildDisplayName(owner)),
                ToRoleString(member.Role),
                member.CreatedAt));
        }

        foreach (var member in propertyMembers)
        {
            var property = properties.FirstOrDefault(p => p.Id == member.PropertyId);
            if (property is null)
                continue;

            if (!propertyProjectLookup.TryGetValue(property.ProjectId, out var project))
                continue;

            if (!ownerLookup.TryGetValue(project.OwnerId, out var owner))
                continue;

            items.Add(new SharedWithMeItemDto(
                "property",
                property.Id,
                property.Name,
                new SharedOwnerDto(owner.Id, owner.Email ?? string.Empty, BuildDisplayName(owner)),
                ToRoleString(member.Role),
                member.CreatedAt));
        }

        foreach (var member in zaznamMembers)
        {
            if (!zaznamLookup.TryGetValue(member.ZaznamId, out var zaznam))
                continue;

            if (!zaznamPropertyLookup.TryGetValue(zaznam.PropertyId, out var property))
                continue;

            if (!zaznamProjectLookup.TryGetValue(property.ProjectId, out var project))
                continue;

            if (!ownerLookup.TryGetValue(project.OwnerId, out var owner))
                continue;

            items.Add(new SharedWithMeItemDto(
                "zaznam",
                zaznam.Id,
                zaznam.Title ?? string.Empty,
                new SharedOwnerDto(owner.Id, owner.Email ?? string.Empty, BuildDisplayName(owner)),
                ToRoleString(member.Role),
                member.CreatedAt));
        }

        return Results.Ok(new SharedWithMeResponse(items));
    }

    private static async Task<IResult> GetPendingInvitationsAsync(
        ClaimsPrincipal user,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var appUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (appUser?.Email is null)
            return Results.Ok(new PendingInvitationsResponse(Array.Empty<PendingInvitationDto>()));

        var normalizedEmail = NormalizeEmail(appUser.Email);
        var now = DateTime.UtcNow;

        var invitations = await dbContext.Invitations
            .Where(i => i.Status == InvitationStatus.Pending && i.ExpiresAt > now)
            .ToListAsync();

        invitations = invitations
            .Where(i => NormalizeEmail(i.Email) == normalizedEmail)
            .ToList();

        if (invitations.Count == 0)
            return Results.Ok(new PendingInvitationsResponse(Array.Empty<PendingInvitationDto>()));

        var projectIds = invitations
            .Where(i => i.TargetType == InvitationTargetType.Project)
            .Select(i => i.TargetId)
            .Distinct()
            .ToList();

        var propertyIds = invitations
            .Where(i => i.TargetType == InvitationTargetType.Property)
            .Select(i => i.TargetId)
            .Distinct()
            .ToList();

        var zaznamIds = invitations
            .Where(i => i.TargetType == InvitationTargetType.Zaznam)
            .Select(i => i.TargetId)
            .Distinct()
            .ToList();

        var projects = await dbContext.Projects
            .Where(p => projectIds.Contains(p.Id))
            .ToListAsync();

        var properties = await dbContext.Properties
            .Where(p => propertyIds.Contains(p.Id))
            .ToListAsync();

        var zaznamy = await dbContext.Zaznamy
            .Where(z => zaznamIds.Contains(z.Id))
            .ToListAsync();

        var inviterIds = invitations.Select(i => i.CreatedBy).Distinct().ToList();
        var inviters = await dbContext.Users
            .Where(u => inviterIds.Contains(u.Id))
            .Select(u => new UserInfo(u.Id, u.Email, u.FirstName, u.LastName))
            .ToListAsync();

        var inviterLookup = inviters.ToDictionary(u => u.Id, u => u);

        var dtos = new List<PendingInvitationDto>();

        foreach (var invitation in invitations)
        {
            var targetName = invitation.TargetType switch
            {
                InvitationTargetType.Project => projects.FirstOrDefault(p => p.Id == invitation.TargetId)?.Name,
                InvitationTargetType.Property => properties.FirstOrDefault(p => p.Id == invitation.TargetId)?.Name,
                InvitationTargetType.Zaznam => zaznamy.FirstOrDefault(z => z.Id == invitation.TargetId)?.Title,
                _ => null
            };

            inviterLookup.TryGetValue(invitation.CreatedBy, out var inviter);
            var invitedBy = new InvitationActorDto(
                invitation.CreatedBy,
                inviter?.Email ?? string.Empty,
                BuildDisplayName(inviter));

            dtos.Add(new PendingInvitationDto(
                invitation.Id,
                ToInvitationType(invitation.TargetType),
                invitation.TargetId,
                targetName ?? string.Empty,
                ToRoleString(invitation.Role),
                invitedBy,
                invitation.CreatedAt,
                invitation.ExpiresAt));
        }

        return Results.Ok(new PendingInvitationsResponse(dtos));
    }

    private static List<ShareMemberDto> BuildProjectShareMembers(
        Project project,
        List<ProjectMember> members,
        List<Invitation> invitations,
        IReadOnlyDictionary<Guid, UserInfo> userLookup)
    {
        var result = new List<ShareMemberDto>();

        if (userLookup.TryGetValue(project.OwnerId, out var owner))
        {
            result.Add(new ShareMemberDto(
                owner.Id,
                owner.Email ?? string.Empty,
                BuildDisplayName(owner),
                "owner",
                "active",
                project.CreatedAt,
                null,
                false));
        }

        foreach (var member in members)
        {
            if (member.UserId == project.OwnerId)
                continue;

            if (!userLookup.TryGetValue(member.UserId, out var user))
                continue;

            result.Add(new ShareMemberDto(
                member.UserId,
                user.Email ?? string.Empty,
                BuildDisplayName(user),
                ToRoleString(member.Role),
                "active",
                member.CreatedAt,
                null,
                !string.IsNullOrWhiteSpace(member.PermissionsJson)));
        }

        foreach (var invitation in invitations)
        {
            result.Add(new ShareMemberDto(
                Guid.Empty,
                invitation.Email,
                invitation.Email,
                ToRoleString(invitation.Role),
                "pending",
                null,
                invitation.CreatedAt,
                !string.IsNullOrWhiteSpace(invitation.PermissionsJson)));
        }

        return result;
    }

    private static List<ShareMemberDto> BuildPropertyShareMembers(
        Property property,
        List<PropertyMember> members,
        List<Invitation> invitations,
        Guid ownerId,
        IReadOnlyDictionary<Guid, UserInfo> userLookup)
    {
        var result = new List<ShareMemberDto>();

        if (userLookup.TryGetValue(ownerId, out var owner))
        {
            result.Add(new ShareMemberDto(
                owner.Id,
                owner.Email ?? string.Empty,
                BuildDisplayName(owner),
                "owner",
                "active",
                property.CreatedAt,
                null,
                false));
        }

        foreach (var member in members)
        {
            if (!userLookup.TryGetValue(member.UserId, out var user))
                continue;

            result.Add(new ShareMemberDto(
                member.UserId,
                user.Email ?? string.Empty,
                BuildDisplayName(user),
                ToRoleString(member.Role),
                "active",
                member.CreatedAt,
                null,
                !string.IsNullOrWhiteSpace(member.PermissionsJson)));
        }

        foreach (var invitation in invitations)
        {
            result.Add(new ShareMemberDto(
                Guid.Empty,
                invitation.Email,
                invitation.Email,
                ToRoleString(invitation.Role),
                "pending",
                null,
                invitation.CreatedAt,
                !string.IsNullOrWhiteSpace(invitation.PermissionsJson)));
        }

        return result;
    }

    private static List<ShareMemberDto> BuildZaznamShareMembers(
        Zaznam zaznam,
        List<ZaznamMember> members,
        List<Invitation> invitations,
        Guid ownerId,
        IReadOnlyDictionary<Guid, UserInfo> userLookup)
    {
        var result = new List<ShareMemberDto>();

        if (userLookup.TryGetValue(ownerId, out var owner))
        {
            result.Add(new ShareMemberDto(
                owner.Id,
                owner.Email ?? string.Empty,
                BuildDisplayName(owner),
                "owner",
                "active",
                zaznam.CreatedAt,
                null,
                false));
        }

        foreach (var member in members)
        {
            if (!userLookup.TryGetValue(member.UserId, out var user))
                continue;

            result.Add(new ShareMemberDto(
                member.UserId,
                user.Email ?? string.Empty,
                BuildDisplayName(user),
                ToRoleString(member.Role),
                "active",
                member.CreatedAt,
                null,
                !string.IsNullOrWhiteSpace(member.PermissionsJson)));
        }

        foreach (var invitation in invitations)
        {
            result.Add(new ShareMemberDto(
                Guid.Empty,
                invitation.Email,
                invitation.Email,
                ToRoleString(invitation.Role),
                "pending",
                null,
                invitation.CreatedAt,
                !string.IsNullOrWhiteSpace(invitation.PermissionsJson)));
        }

        return result;
    }

    private static string BuildDisplayName(UserInfo? user)
    {
        if (user is null)
            return string.Empty;

        var name = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? user.Email ?? string.Empty : name;
    }

    private static string ToRoleString(MemberRole role)
    {
        return role.ToString().ToLowerInvariant();
    }

    private static string ToInvitationType(InvitationTargetType type)
    {
        return type switch
        {
            InvitationTargetType.Project => "project",
            InvitationTargetType.Property => "property",
            InvitationTargetType.Zaznam => "zaznam",
            _ => "project"
        };
    }

    private static string? NormalizeEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email)
            ? null
            : email.Trim().ToUpperInvariant();
    }

    private sealed record UserInfo(Guid Id, string? Email, string FirstName, string LastName);
}
