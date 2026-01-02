using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MujDomecek.API.Extensions;
using MujDomecek.Application.DTOs;
using MujDomecek.Domain.Aggregates.User;
using MujDomecek.Infrastructure.Persistence;

namespace MujDomecek.API.Endpoints;

public static class ContactGroupEndpoints
{
    public static IEndpointRouteBuilder MapContactGroupEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/contact-groups").RequireAuthorization();

        group.MapGet("/", GetGroupsAsync);
        group.MapPost("/", CreateGroupAsync);
        group.MapGet("/{id:guid}", GetGroupAsync);
        group.MapPut("/{id:guid}", UpdateGroupAsync);
        group.MapDelete("/{id:guid}", DeleteGroupAsync);
        group.MapPost("/{id:guid}/members", AddMemberAsync);
        group.MapDelete("/{id:guid}/members/{contactId:guid}", RemoveMemberAsync);

        return endpoints;
    }

    private static async Task<IResult> GetGroupsAsync(
        ClaimsPrincipal user,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var groups = await dbContext.ContactGroups
            .Where(g => g.OwnerUserId == userId)
            .ToListAsync();

        var dtos = await BuildGroupDtosAsync(dbContext, groups);
        return Results.Ok(dtos);
    }

    private static async Task<IResult> CreateGroupAsync(
        ClaimsPrincipal user,
        [FromBody] CreateContactGroupRequest request,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var name = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
            return Results.BadRequest();

        var group = new ContactGroup
        {
            Id = Guid.NewGuid(),
            OwnerUserId = userId,
            Name = name,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.ContactGroups.Add(group);

        if (request.ContactIds is { Count: > 0 })
        {
            var contacts = await dbContext.Contacts
                .Where(c => c.OwnerUserId == userId && request.ContactIds.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync();

            foreach (var contactId in contacts.Distinct())
            {
                dbContext.ContactGroupMembers.Add(new ContactGroupMember
                {
                    Id = Guid.NewGuid(),
                    GroupId = group.Id,
                    ContactId = contactId
                });
            }
        }

        await dbContext.SaveChangesAsync();

        var dto = (await BuildGroupDtosAsync(dbContext, new List<ContactGroup> { group })).First();
        return Results.Created($"/contact-groups/{group.Id}", dto);
    }

    private static async Task<IResult> GetGroupAsync(
        ClaimsPrincipal user,
        Guid id,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var group = await dbContext.ContactGroups
            .FirstOrDefaultAsync(g => g.Id == id && g.OwnerUserId == userId);
        if (group is null)
            return Results.NotFound();

        var dto = (await BuildGroupDtosAsync(dbContext, new List<ContactGroup> { group })).First();
        return Results.Ok(dto);
    }

    private static async Task<IResult> UpdateGroupAsync(
        ClaimsPrincipal user,
        Guid id,
        [FromBody] UpdateContactGroupRequest request,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var group = await dbContext.ContactGroups
            .FirstOrDefaultAsync(g => g.Id == id && g.OwnerUserId == userId);
        if (group is null)
            return Results.NotFound();

        if (!string.IsNullOrWhiteSpace(request.Name))
            group.Name = request.Name.Trim();

        await dbContext.SaveChangesAsync();

        var dto = (await BuildGroupDtosAsync(dbContext, new List<ContactGroup> { group })).First();
        return Results.Ok(dto);
    }

    private static async Task<IResult> DeleteGroupAsync(
        ClaimsPrincipal user,
        Guid id,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var group = await dbContext.ContactGroups
            .FirstOrDefaultAsync(g => g.Id == id && g.OwnerUserId == userId);
        if (group is null)
            return Results.NotFound();

        var members = await dbContext.ContactGroupMembers
            .Where(m => m.GroupId == group.Id)
            .ToListAsync();

        dbContext.ContactGroupMembers.RemoveRange(members);
        dbContext.ContactGroups.Remove(group);
        await dbContext.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<IResult> AddMemberAsync(
        ClaimsPrincipal user,
        Guid id,
        [FromBody] AddGroupMemberRequest request,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var group = await dbContext.ContactGroups
            .FirstOrDefaultAsync(g => g.Id == id && g.OwnerUserId == userId);
        if (group is null)
            return Results.NotFound();

        var contact = await dbContext.Contacts
            .FirstOrDefaultAsync(c => c.Id == request.ContactId && c.OwnerUserId == userId);
        if (contact is null)
            return Results.NotFound();

        var exists = await dbContext.ContactGroupMembers
            .AnyAsync(m => m.GroupId == group.Id && m.ContactId == contact.Id);
        if (exists)
            return Results.NoContent();

        dbContext.ContactGroupMembers.Add(new ContactGroupMember
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            ContactId = contact.Id
        });

        await dbContext.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> RemoveMemberAsync(
        ClaimsPrincipal user,
        Guid id,
        Guid contactId,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var group = await dbContext.ContactGroups
            .FirstOrDefaultAsync(g => g.Id == id && g.OwnerUserId == userId);
        if (group is null)
            return Results.NotFound();

        var member = await dbContext.ContactGroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == group.Id && m.ContactId == contactId);
        if (member is null)
            return Results.NotFound();

        dbContext.ContactGroupMembers.Remove(member);
        await dbContext.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<List<ContactGroupDto>> BuildGroupDtosAsync(
        ApplicationDbContext dbContext,
        IReadOnlyList<ContactGroup> groups)
    {
        if (groups.Count == 0)
            return [];

        var groupIds = groups.Select(g => g.Id).ToList();

        var members = await dbContext.ContactGroupMembers
            .Where(m => groupIds.Contains(m.GroupId))
            .Join(dbContext.Contacts, m => m.ContactId, c => c.Id, (m, c) => new { m.GroupId, Contact = c })
            .ToListAsync();

        var registeredLookup = await BuildRegisteredLookupAsync(dbContext, members.Select(m => m.Contact.Email));
        var membersByGroup = members
            .GroupBy(m => m.GroupId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(m => ToContactDto(m.Contact, registeredLookup)).ToList());

        var result = new List<ContactGroupDto>(groups.Count);

        foreach (var group in groups)
        {
            var groupMembers = membersByGroup.TryGetValue(group.Id, out var list)
                ? list
                : [];

            result.Add(new ContactGroupDto(
                group.Id,
                group.Name,
                groupMembers.Count,
                groupMembers,
                group.CreatedAt));
        }

        return result;
    }

    private static async Task<Dictionary<string, Guid>> BuildRegisteredLookupAsync(
        ApplicationDbContext dbContext,
        IEnumerable<string> emails)
    {
        var normalized = emails
            .Select(NormalizeEmail)
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Distinct()
            .ToList();

        if (normalized.Count == 0)
            return new Dictionary<string, Guid>(StringComparer.Ordinal);

        var users = await dbContext.Users
            .Where(u => u.NormalizedEmail != null && normalized.Contains(u.NormalizedEmail))
            .Select(u => new { u.Id, u.NormalizedEmail })
            .ToListAsync();

        return users.ToDictionary(u => u.NormalizedEmail!, u => u.Id, StringComparer.Ordinal);
    }

    private static ContactDto ToContactDto(
        Contact contact,
        IReadOnlyDictionary<string, Guid> registeredLookup)
    {
        var normalizedEmail = NormalizeEmail(contact.Email);
        Guid? registeredUserId = normalizedEmail is not null && registeredLookup.TryGetValue(normalizedEmail, out var userId)
            ? userId
            : null;

        return new ContactDto(
            contact.Id,
            contact.Email,
            contact.DisplayName,
            registeredUserId.HasValue,
            registeredUserId,
            contact.CreatedAt);
    }

    private static string? NormalizeEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email)
            ? null
            : email.Trim().ToUpperInvariant();
    }
}

