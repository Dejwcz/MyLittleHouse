using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MujDomecek.API.Extensions;
using MujDomecek.Application.DTOs;
using MujDomecek.Domain.Aggregates.User;
using MujDomecek.Infrastructure.Persistence;

namespace MujDomecek.API.Endpoints;

public static class ContactEndpoints
{
    public static IEndpointRouteBuilder MapContactEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/contacts").RequireAuthorization();

        group.MapGet("/", GetContactsAsync);
        group.MapPost("/", CreateContactAsync);
        group.MapGet("/{id:guid}", GetContactAsync);
        group.MapPut("/{id:guid}", UpdateContactAsync);
        group.MapDelete("/{id:guid}", DeleteContactAsync);

        return endpoints;
    }

    private static async Task<IResult> GetContactsAsync(
        ClaimsPrincipal user,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var contacts = await dbContext.Contacts
            .Where(c => c.OwnerUserId == userId)
            .ToListAsync();

        var registeredLookup = await BuildRegisteredLookupAsync(dbContext, contacts.Select(c => c.Email));
        var dtos = contacts.Select(c => ToContactDto(c, registeredLookup)).ToList();

        return Results.Ok(dtos);
    }

    private static async Task<IResult> CreateContactAsync(
        ClaimsPrincipal user,
        [FromBody] CreateContactRequest request,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var email = request.Email.Trim();
        if (string.IsNullOrWhiteSpace(email))
            return Results.BadRequest();

        var contact = new Contact
        {
            Id = Guid.NewGuid(),
            OwnerUserId = userId,
            Email = email,
            DisplayName = request.DisplayName,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Contacts.Add(contact);
        await dbContext.SaveChangesAsync();

        var dto = await BuildContactDtoAsync(dbContext, contact);
        return Results.Created($"/contacts/{contact.Id}", dto);
    }

    private static async Task<IResult> GetContactAsync(
        ClaimsPrincipal user,
        Guid id,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var contact = await dbContext.Contacts
            .FirstOrDefaultAsync(c => c.Id == id && c.OwnerUserId == userId);
        if (contact is null)
            return Results.NotFound();

        var dto = await BuildContactDtoAsync(dbContext, contact);
        return Results.Ok(dto);
    }

    private static async Task<IResult> UpdateContactAsync(
        ClaimsPrincipal user,
        Guid id,
        [FromBody] UpdateContactRequest request,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var contact = await dbContext.Contacts
            .FirstOrDefaultAsync(c => c.Id == id && c.OwnerUserId == userId);
        if (contact is null)
            return Results.NotFound();

        if (request.DisplayName is not null)
            contact.DisplayName = request.DisplayName;

        await dbContext.SaveChangesAsync();

        var dto = await BuildContactDtoAsync(dbContext, contact);
        return Results.Ok(dto);
    }

    private static async Task<IResult> DeleteContactAsync(
        ClaimsPrincipal user,
        Guid id,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var contact = await dbContext.Contacts
            .FirstOrDefaultAsync(c => c.Id == id && c.OwnerUserId == userId);
        if (contact is null)
            return Results.NotFound();

        dbContext.Contacts.Remove(contact);
        await dbContext.SaveChangesAsync();
        return Results.NoContent();
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

    private static async Task<ContactDto> BuildContactDtoAsync(
        ApplicationDbContext dbContext,
        Contact contact)
    {
        var lookup = await BuildRegisteredLookupAsync(dbContext, new[] { contact.Email });
        return ToContactDto(contact, lookup);
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

