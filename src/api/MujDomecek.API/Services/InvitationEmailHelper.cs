using Microsoft.EntityFrameworkCore;
using MujDomecek.Application.Abstractions;
using MujDomecek.Application.DTOs;
using MujDomecek.Domain.Aggregates.Project;
using MujDomecek.Domain.ValueObjects;
using MujDomecek.Infrastructure.Persistence;

namespace MujDomecek.API.Services;

public static class InvitationEmailHelper
{
    public static async Task SendInvitationAsync(
        ApplicationDbContext dbContext,
        IEmailDispatcher emailDispatcher,
        EmailTemplateService emailTemplates,
        Invitation invitation,
        string inviteUrl,
        string targetName,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(invitation.Email))
            return;

        var inviter = await dbContext.Users
            .Where(u => u.Id == invitation.CreatedBy)
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email })
            .FirstOrDefaultAsync(ct);

        var inviterName = BuildDisplayName(inviter?.FirstName, inviter?.LastName, inviter?.Email);

        var normalized = NormalizeEmail(invitation.Email);
        var recipient = normalized is null
            ? null
            : await dbContext.Users
                .Where(u => u.NormalizedEmail == normalized)
                .Select(u => new { u.Id, u.IsDeleted })
                .FirstOrDefaultAsync(ct);

        if (recipient is not null)
        {
            if (recipient.IsDeleted || recipient.Id == invitation.CreatedBy)
                return;

            var prefs = await dbContext.UserPreferences
                .Where(p => p.UserId == recipient.Id)
                .Select(p => (bool?)p.EmailInvitations)
                .FirstOrDefaultAsync(ct);

            if (prefs.HasValue && !prefs.Value)
                return;
        }

        var template = emailTemplates.Render(
            "invitation",
            new Dictionary<string, string?>
            {
                ["inviterName"] = inviterName,
                ["targetType"] = ToTypeString(invitation.TargetType),
                ["targetName"] = targetName,
                ["role"] = ToRoleString(invitation.Role),
                ["inviteUrl"] = inviteUrl
            });

        await emailDispatcher.SendAsync(new EmailJobRequest(
            invitation.Email,
            template.Subject,
            template.Body), ct);
    }

    public static async Task SendInvitationAcceptedAsync(
        ApplicationDbContext dbContext,
        IEmailDispatcher emailDispatcher,
        EmailTemplateService emailTemplates,
        Invitation invitation,
        string targetName,
        string acceptedByName,
        CancellationToken ct)
    {
        var inviter = await dbContext.Users
            .Where(u => u.Id == invitation.CreatedBy)
            .Select(u => new { u.Id, u.Email, u.FirstName, u.LastName })
            .FirstOrDefaultAsync(ct);

        if (inviter is null || string.IsNullOrWhiteSpace(inviter.Email))
            return;

        var prefs = await dbContext.UserPreferences
            .Where(p => p.UserId == inviter.Id)
            .Select(p => (bool?)p.EmailInvitations)
            .FirstOrDefaultAsync(ct);

        if (prefs.HasValue && !prefs.Value)
            return;

        var template = emailTemplates.Render(
            "invitation-accepted",
            new Dictionary<string, string?>
            {
                ["userName"] = acceptedByName,
                ["targetType"] = ToTypeString(invitation.TargetType),
                ["targetName"] = targetName
            });

        await emailDispatcher.SendAsync(new EmailJobRequest(
            inviter.Email,
            template.Subject,
            template.Body), ct);
    }

    private static string? NormalizeEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email)
            ? null
            : email.Trim().ToUpperInvariant();
    }

    private static string BuildDisplayName(string? firstName, string? lastName, string? email)
    {
        var name = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? email ?? string.Empty : name;
    }

    private static string ToTypeString(InvitationTargetType type)
    {
        return type == InvitationTargetType.Property ? "property" : "project";
    }

    private static string ToRoleString(MemberRole role)
    {
        return role.ToString().ToLowerInvariant();
    }
}
