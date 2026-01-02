using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MujDomecek.API.Extensions;
using MujDomecek.API.Services;
using MujDomecek.Application.DTOs;
using MujDomecek.Domain.Aggregates.Zaznam;
using MujDomecek.Domain.ValueObjects;
using MujDomecek.Infrastructure.Persistence;

namespace MujDomecek.API.Endpoints;

public static class CommentEndpoints
{
    public static IEndpointRouteBuilder MapCommentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("").RequireAuthorization();

        group.MapGet("/zaznamy/{zaznamId:guid}/comments", GetCommentsAsync);
        group.MapPost("/zaznamy/{zaznamId:guid}/comments", CreateCommentAsync);
        group.MapPut("/comments/{id:guid}", UpdateCommentAsync);
        group.MapDelete("/comments/{id:guid}", DeleteCommentAsync);

        return endpoints;
    }

    private static async Task<IResult> GetCommentsAsync(
        ClaimsPrincipal user,
        Guid zaznamId,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var propertyId = await GetPropertyIdAsync(dbContext, zaznamId);
        if (propertyId is null)
            return Results.NotFound();

        if (!await HasPropertyAccessAsync(dbContext, propertyId.Value, userId))
            return Results.Forbid();

        var comments = await dbContext.Comments
            .Where(c => c.ZaznamId == zaznamId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        if (comments.Count == 0)
            return Results.Ok(Array.Empty<CommentDto>());

        var commentIds = comments.Select(c => c.Id).ToList();
        var mentionLookup = await CommentMentionHelper.LoadMentionLookupAsync(dbContext, commentIds);

        var authorIds = comments.Select(c => c.AuthorUserId).Distinct().ToList();
        var authors = await dbContext.Users
            .Where(u => authorIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email })
            .ToListAsync();

        var authorLookup = authors.ToDictionary(a => a.Id, a => a);

        var dtos = comments.Select(c =>
        {
            authorLookup.TryGetValue(c.AuthorUserId, out var author);
            mentionLookup.TryGetValue(c.Id, out var mentions);

            return new CommentDto(
                c.Id,
                c.ZaznamId,
                c.Content,
                mentions ?? [],
                new CommentAuthorDto(
                    c.AuthorUserId,
                    BuildDisplayName(author?.FirstName, author?.LastName, author?.Email),
                    null),
                c.CreatedAt,
                c.UpdatedAt,
                c.CreatedAt != c.UpdatedAt);
        }).ToList();

        return Results.Ok(dtos);
    }

    private static async Task<IResult> CreateCommentAsync(
        ClaimsPrincipal user,
        Guid zaznamId,
        [FromBody] CreateCommentRequest request,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Content))
            return Results.BadRequest();

        var propertyId = await GetPropertyIdAsync(dbContext, zaznamId);
        if (propertyId is null)
            return Results.NotFound();

        if (!await HasPropertyAccessAsync(dbContext, propertyId.Value, userId))
            return Results.Forbid();

        var mentionTokens = CommentMentionHelper.ExtractMentionTokens(request.Content);
        var mentionCandidates = await CommentMentionHelper.GetMentionCandidatesAsync(dbContext, propertyId.Value);
        var mentions = CommentMentionHelper.ResolveMentions(mentionTokens, mentionCandidates, userId);

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            ZaznamId = zaznamId,
            AuthorUserId = userId,
            Content = request.Content,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (mentions.Count > 0)
        {
            comment.Mentions = mentions
                .Select(m => new CommentMention { CommentId = comment.Id, MentionedUserId = m.UserId })
                .ToList();
        }

        dbContext.Comments.Add(comment);
        ActivityNotificationHelper.AddActivity(
            dbContext,
            propertyId.Value,
            userId,
            ActivityType.CommentAdded,
            "comment",
            comment.Id,
            new { zaznamId });

        foreach (var mention in mentions)
        {
            ActivityNotificationHelper.AddNotification(
                dbContext,
                mention.UserId,
                NotificationType.MentionInComment,
                new
                {
                    commentId = comment.Id,
                    zaznamId,
                    propertyId = propertyId.Value,
                    mentionedByUserId = userId
                });
        }

        var zaznamOwnerId = await GetZaznamOwnerIdAsync(dbContext, zaznamId);
        if (zaznamOwnerId != Guid.Empty
            && zaznamOwnerId != userId
            && mentions.All(m => m.UserId != zaznamOwnerId))
        {
            ActivityNotificationHelper.AddNotification(
                dbContext,
                zaznamOwnerId,
                NotificationType.CommentOnYourZaznam,
                new
                {
                    commentId = comment.Id,
                    zaznamId,
                    propertyId = propertyId.Value,
                    commentedByUserId = userId
                });
        }

        await dbContext.SaveChangesAsync();

        var author = await dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.FirstName, u.LastName, u.Email })
            .FirstOrDefaultAsync();

        var dto = new CommentDto(
            comment.Id,
            comment.ZaznamId,
            comment.Content,
            mentions,
            new CommentAuthorDto(comment.AuthorUserId, BuildDisplayName(author?.FirstName, author?.LastName, author?.Email), null),
            comment.CreatedAt,
            comment.UpdatedAt,
            false);

        return Results.Created($"/comments/{comment.Id}", dto);
    }

    private static async Task<IResult> UpdateCommentAsync(
        ClaimsPrincipal user,
        Guid id,
        [FromBody] UpdateCommentRequest request,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var comment = await dbContext.Comments.FirstOrDefaultAsync(c => c.Id == id);
        if (comment is null)
            return Results.NotFound();

        var propertyId = await GetPropertyIdAsync(dbContext, comment.ZaznamId);
        if (propertyId is null)
            return Results.NotFound();

        if (!await HasPropertyAccessAsync(dbContext, propertyId.Value, userId))
            return Results.Forbid();

        if (comment.AuthorUserId != userId)
            return Results.Forbid();

        if (string.IsNullOrWhiteSpace(request.Content))
            return Results.BadRequest();

        var mentionTokens = CommentMentionHelper.ExtractMentionTokens(request.Content);
        var mentionCandidates = await CommentMentionHelper.GetMentionCandidatesAsync(dbContext, propertyId.Value);
        var mentions = CommentMentionHelper.ResolveMentions(mentionTokens, mentionCandidates, userId);

        var existingMentions = await dbContext.CommentMentions
            .Where(m => m.CommentId == comment.Id)
            .ToListAsync();

        var desiredIds = mentions.Select(m => m.UserId).ToHashSet();
        var existingIds = existingMentions.Select(m => m.MentionedUserId).ToHashSet();

        foreach (var mention in existingMentions.Where(m => !desiredIds.Contains(m.MentionedUserId)))
            dbContext.CommentMentions.Remove(mention);

        var newIds = desiredIds.Except(existingIds).ToHashSet();
        foreach (var newId in newIds)
        {
            dbContext.CommentMentions.Add(new CommentMention
            {
                CommentId = comment.Id,
                MentionedUserId = newId
            });
        }

        foreach (var mention in mentions.Where(m => newIds.Contains(m.UserId)))
        {
            ActivityNotificationHelper.AddNotification(
                dbContext,
                mention.UserId,
                NotificationType.MentionInComment,
                new
                {
                    commentId = comment.Id,
                    zaznamId = comment.ZaznamId,
                    propertyId = propertyId.Value,
                    mentionedByUserId = userId
                });
        }

        comment.Content = request.Content;
        comment.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteCommentAsync(
        ClaimsPrincipal user,
        Guid id,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var comment = await dbContext.Comments.FirstOrDefaultAsync(c => c.Id == id);
        if (comment is null)
            return Results.NotFound();

        var propertyId = await GetPropertyIdAsync(dbContext, comment.ZaznamId);
        if (propertyId is null)
            return Results.NotFound();

        if (!await HasPropertyAccessAsync(dbContext, propertyId.Value, userId))
            return Results.Forbid();

        if (comment.AuthorUserId != userId)
            return Results.Forbid();

        comment.IsDeleted = true;
        comment.DeletedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<Guid?> GetPropertyIdAsync(ApplicationDbContext dbContext, Guid zaznamId)
    {
        var propertyId = await dbContext.Zaznamy
            .Where(z => z.Id == zaznamId)
            .Select(z => z.PropertyId)
            .FirstOrDefaultAsync();

        return propertyId == Guid.Empty ? null : propertyId;
    }

    private static Task<bool> HasPropertyAccessAsync(ApplicationDbContext dbContext, Guid propertyId, Guid userId)
    {
        return dbContext.Properties.AnyAsync(p => p.Id == propertyId &&
            (dbContext.Projects.Any(pr => pr.Id == p.ProjectId && pr.OwnerId == userId)
            || dbContext.ProjectMembers.Any(pm => pm.ProjectId == p.ProjectId && pm.UserId == userId)
            || dbContext.PropertyMembers.Any(pm => pm.PropertyId == p.Id && pm.UserId == userId)));
    }

    private static async Task<Guid> GetZaznamOwnerIdAsync(ApplicationDbContext dbContext, Guid zaznamId)
    {
        return await dbContext.Activities
            .Where(a => a.TargetId == zaznamId && a.Type == ActivityType.ZaznamCreated)
            .OrderBy(a => a.CreatedAt)
            .Select(a => a.ActorUserId)
            .FirstOrDefaultAsync();
    }

    private static string BuildDisplayName(string? firstName, string? lastName, string? email)
    {
        var name = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? email ?? string.Empty : name;
    }
}

