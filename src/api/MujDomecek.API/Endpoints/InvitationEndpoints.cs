using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MujDomecek.API.Extensions;
using MujDomecek.API.Services;
using MujDomecek.Application.Abstractions;
using MujDomecek.Application.DTOs;
using MujDomecek.Domain.Aggregates.Project;
using MujDomecek.Domain.Aggregates.Property;
using MujDomecek.Domain.ValueObjects;
using MujDomecek.Infrastructure.Persistence;

namespace MujDomecek.API.Endpoints;

public static class InvitationEndpoints
{
    public static IEndpointRouteBuilder MapInvitationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/invitations").RequireAuthorization();

        group.MapGet("/{id:guid}", GetInvitationAsync);
        group.MapGet("/by-token", GetInvitationByTokenAsync);
        group.MapPost("/{id:guid}/accept", AcceptInvitationAsync);
        group.MapPost("/{id:guid}/decline", DeclineInvitationAsync);
        group.MapPost("/{id:guid}/resend", ResendInvitationAsync);
        group.MapDelete("/{id:guid}", DeleteInvitationAsync);

        return endpoints;
    }

    private static async Task<IResult> GetInvitationAsync(
        ClaimsPrincipal user,
        Guid id,
        ApplicationDbContext dbContext,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var appUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (appUser?.Email is null)
            return Results.Unauthorized();

        var invitation = await dbContext.Invitations.FirstOrDefaultAsync(i => i.Id == id);
        if (invitation is null)
            return Results.NotFound();

        if (!IsRecipientOrSender(invitation, appUser.Email, userId))
            return Results.Forbid();

        await TryExpireInvitationAsync(dbContext, invitation, ct);

        var dto = await BuildInvitationDtoAsync(dbContext, invitation, ct);
        return Results.Ok(dto);
    }

    private static async Task<IResult> GetInvitationByTokenAsync(
        ClaimsPrincipal user,
        string? token,
        ApplicationDbContext dbContext,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(token))
            return Results.BadRequest();

        var appUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (appUser?.Email is null)
            return Results.Unauthorized();

        var tokenHash = InvitationTokenHelper.HashToken(token);
        var invitation = await dbContext.Invitations.FirstOrDefaultAsync(i => i.TokenHash == tokenHash, ct);
        if (invitation is null)
            return Results.NotFound();

        if (!IsRecipientOrSender(invitation, appUser.Email, userId))
            return Results.Forbid();

        await TryExpireInvitationAsync(dbContext, invitation, ct);

        var dto = await BuildInvitationDtoAsync(dbContext, invitation, ct);
        return Results.Ok(dto);
    }

    private static async Task<IResult> AcceptInvitationAsync(
        ClaimsPrincipal user,
        Guid id,
        ApplicationDbContext dbContext,
        IEmailDispatcher emailDispatcher,
        EmailTemplateService emailTemplates,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var appUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (appUser?.Email is null)
            return Results.Unauthorized();

        var invitation = await dbContext.Invitations.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (invitation is null)
            return Results.NotFound();

        if (!IsRecipient(invitation, appUser.Email))
            return Results.Forbid();

        if (invitation.Status != InvitationStatus.Pending)
            return Results.BadRequest();

        if (invitation.ExpiresAt <= DateTime.UtcNow)
        {
            await ExpireInvitationAsync(dbContext, invitation, ct);
            return Results.BadRequest();
        }

        var targetName = invitation.TargetType == InvitationTargetType.Project
            ? await dbContext.Projects.Where(p => p.Id == invitation.TargetId).Select(p => p.Name).FirstOrDefaultAsync(ct)
            : await dbContext.Properties.Where(p => p.Id == invitation.TargetId).Select(p => p.Name).FirstOrDefaultAsync(ct);

        if (invitation.TargetType == InvitationTargetType.Project)
        {
            var exists = await dbContext.ProjectMembers
                .AnyAsync(m => m.ProjectId == invitation.TargetId && m.UserId == userId, ct);
            if (!exists)
            {
                dbContext.ProjectMembers.Add(new ProjectMember
                {
                    Id = Guid.NewGuid(),
                    ProjectId = invitation.TargetId,
                    UserId = userId,
                    Role = invitation.Role,
                    PermissionsJson = invitation.PermissionsJson,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        else
        {
            var exists = await dbContext.PropertyMembers
                .AnyAsync(m => m.PropertyId == invitation.TargetId && m.UserId == userId, ct);
            if (!exists)
            {
                dbContext.PropertyMembers.Add(new PropertyMember
                {
                    Id = Guid.NewGuid(),
                    PropertyId = invitation.TargetId,
                    UserId = userId,
                    Role = invitation.Role,
                    PermissionsJson = invitation.PermissionsJson,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        if (invitation.TargetType == InvitationTargetType.Property)
        {
            ActivityNotificationHelper.AddActivity(
                dbContext,
                invitation.TargetId,
                userId,
                ActivityType.MemberJoined,
                "member",
                userId,
                new { userId, email = appUser.Email });
        }

        if (invitation.CreatedBy != userId)
        {
            ActivityNotificationHelper.AddNotification(dbContext, invitation.CreatedBy, NotificationType.InvitationAccepted, new
            {
                invitationId = invitation.Id,
                targetType = ToTypeString(invitation.TargetType),
                targetId = invitation.TargetId,
                targetName = targetName ?? string.Empty,
                acceptedByUserId = userId,
                acceptedByEmail = appUser.Email
            });
        }

        invitation.Status = InvitationStatus.Accepted;
        await dbContext.SaveChangesAsync();

        if (invitation.CreatedBy != userId)
        {
            var acceptedByName = BuildDisplayName(new UserInfo(
                appUser.Id,
                appUser.Email,
                appUser.FirstName,
                appUser.LastName));

            await InvitationEmailHelper.SendInvitationAcceptedAsync(
                dbContext,
                emailDispatcher,
                emailTemplates,
                invitation,
                targetName ?? string.Empty,
                acceptedByName,
                ct);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> DeclineInvitationAsync(
        ClaimsPrincipal user,
        Guid id,
        ApplicationDbContext dbContext,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var appUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (appUser?.Email is null)
            return Results.Unauthorized();

        var invitation = await dbContext.Invitations.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (invitation is null)
            return Results.NotFound();

        if (!IsRecipient(invitation, appUser.Email))
            return Results.Forbid();

        if (invitation.Status != InvitationStatus.Pending)
            return Results.BadRequest();

        if (invitation.ExpiresAt <= DateTime.UtcNow)
        {
            await ExpireInvitationAsync(dbContext, invitation, ct);
            return Results.BadRequest();
        }

        var targetName = invitation.TargetType == InvitationTargetType.Project
            ? await dbContext.Projects.Where(p => p.Id == invitation.TargetId).Select(p => p.Name).FirstOrDefaultAsync(ct)
            : await dbContext.Properties.Where(p => p.Id == invitation.TargetId).Select(p => p.Name).FirstOrDefaultAsync(ct);

        if (invitation.CreatedBy != userId)
        {
            ActivityNotificationHelper.AddNotification(dbContext, invitation.CreatedBy, NotificationType.InvitationDeclined, new
            {
                invitationId = invitation.Id,
                targetType = ToTypeString(invitation.TargetType),
                targetId = invitation.TargetId,
                targetName = targetName ?? string.Empty,
                declinedByUserId = userId,
                declinedByEmail = appUser.Email
            });
        }

        invitation.Status = InvitationStatus.Declined;
        await dbContext.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<IResult> ResendInvitationAsync(
        ClaimsPrincipal user,
        HttpRequest httpRequest,
        Guid id,
        ApplicationDbContext dbContext,
        IEmailDispatcher emailDispatcher,
        EmailTemplateService emailTemplates,
        IConfiguration configuration,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var invitation = await dbContext.Invitations.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (invitation is null)
            return Results.NotFound();

        if (invitation.CreatedBy != userId)
            return Results.Forbid();

        if (invitation.Status == InvitationStatus.Accepted)
            return Results.BadRequest();

        var token = InvitationTokenHelper.CreateToken();
        var inviteUrl = InvitationTokenHelper.BuildInviteUrl(httpRequest, configuration, token.Token);

        invitation.TokenHash = token.TokenHash;
        invitation.Status = InvitationStatus.Pending;
        invitation.ExpiresAt = DateTime.UtcNow.AddDays(7);

        var targetName = await GetTargetNameAsync(dbContext, invitation, ct);
        var normalizedEmail = NormalizeEmail(invitation.Email);
        if (normalizedEmail is not null)
        {
            var recipient = await dbContext.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, ct);
            if (recipient is not null && recipient.Id != userId)
            {
                ActivityNotificationHelper.AddNotification(dbContext, recipient.Id, NotificationType.InvitationReceived, new
                {
                    invitationId = invitation.Id,
                    targetType = ToTypeString(invitation.TargetType),
                    targetId = invitation.TargetId,
                    targetName,
                    role = ToRoleString(invitation.Role),
                    invitedByUserId = userId
                });
            }
        }

        await dbContext.SaveChangesAsync();

        await InvitationEmailHelper.SendInvitationAsync(
            dbContext,
            emailDispatcher,
            emailTemplates,
            invitation,
            inviteUrl,
            targetName,
            ct);

        return Results.Ok(new InvitationLinkResponse(invitation.Id, inviteUrl, invitation.ExpiresAt));
    }

    private static async Task<IResult> DeleteInvitationAsync(
        ClaimsPrincipal user,
        Guid id,
        ApplicationDbContext dbContext,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var invitation = await dbContext.Invitations.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (invitation is null)
            return Results.NotFound();

        if (invitation.CreatedBy != userId)
            return Results.Forbid();

        dbContext.Invitations.Remove(invitation);
        await dbContext.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task TryExpireInvitationAsync(
        ApplicationDbContext dbContext,
        Invitation invitation,
        CancellationToken ct)
    {
        if (invitation.Status != InvitationStatus.Pending)
            return;

        if (invitation.ExpiresAt > DateTime.UtcNow)
            return;

        await ExpireInvitationAsync(dbContext, invitation, ct);
    }

    private static async Task ExpireInvitationAsync(
        ApplicationDbContext dbContext,
        Invitation invitation,
        CancellationToken ct)
    {
        if (invitation.Status == InvitationStatus.Expired)
            return;

        invitation.Status = InvitationStatus.Expired;

        var targetName = await GetTargetNameAsync(dbContext, invitation, ct);
        ActivityNotificationHelper.AddNotification(
            dbContext,
            invitation.CreatedBy,
            NotificationType.InvitationExpired,
            new
            {
                invitationId = invitation.Id,
                email = invitation.Email,
                targetType = ToTypeString(invitation.TargetType),
                targetId = invitation.TargetId,
                targetName
            });

        await dbContext.SaveChangesAsync();
    }

    private static async Task<string> GetTargetNameAsync(
        ApplicationDbContext dbContext,
        Invitation invitation,
        CancellationToken ct)
    {
        return invitation.TargetType == InvitationTargetType.Project
            ? await dbContext.Projects
                .Where(p => p.Id == invitation.TargetId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(ct) ?? string.Empty
            : await dbContext.Properties
                .Where(p => p.Id == invitation.TargetId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(ct) ?? string.Empty;
    }

    private static async Task<InvitationDto> BuildInvitationDtoAsync(
        ApplicationDbContext dbContext,
        Invitation invitation,
        CancellationToken ct)
    {
        string targetName;
        if (invitation.TargetType == InvitationTargetType.Project)
        {
            targetName = await dbContext.Projects
                .Where(p => p.Id == invitation.TargetId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(ct) ?? string.Empty;
        }
        else
        {
            targetName = await dbContext.Properties
                .Where(p => p.Id == invitation.TargetId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(ct) ?? string.Empty;
        }

        var inviter = await dbContext.Users
            .Where(u => u.Id == invitation.CreatedBy)
            .Select(u => new UserInfo(u.Id, u.Email, u.FirstName, u.LastName))
            .FirstOrDefaultAsync(ct);

        var invitedBy = new InvitationActorDto(
            invitation.CreatedBy,
            inviter?.Email ?? string.Empty,
            BuildDisplayName(inviter));

        return new InvitationDto(
            invitation.Id,
            ToTypeString(invitation.TargetType),
            invitation.TargetId,
            targetName,
            invitation.Email,
            ToRoleString(invitation.Role),
            DeserializePermissions(invitation.PermissionsJson),
            ToStatusString(invitation.Status),
            invitedBy,
            invitation.CreatedAt,
            invitation.ExpiresAt);
    }

    private static bool IsRecipientOrSender(Invitation invitation, string? email, Guid userId)
    {
        return invitation.CreatedBy == userId || IsRecipient(invitation, email);
    }

    private static bool IsRecipient(Invitation invitation, string? email)
    {
        var normalized = NormalizeEmail(email);
        return normalized is not null && normalized == NormalizeEmail(invitation.Email);
    }

    private static string ToRoleString(MemberRole role)
    {
        return role.ToString().ToLowerInvariant();
    }

    private static string ToStatusString(InvitationStatus status)
    {
        return status.ToString().ToLowerInvariant();
    }

    private static string ToTypeString(InvitationTargetType type)
    {
        return type == InvitationTargetType.Property ? "property" : "project";
    }

    private static IDictionary<string, bool>? DeserializePermissions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonSerializer.Deserialize<Dictionary<string, bool>>(json);
    }

    private static string? NormalizeEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email)
            ? null
            : email.Trim().ToUpperInvariant();
    }

    private static string BuildDisplayName(UserInfo? user)
    {
        if (user is null)
            return string.Empty;

        var name = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? user.Email ?? string.Empty : name;
    }

    private sealed record UserInfo(Guid Id, string? Email, string FirstName, string LastName);
}
