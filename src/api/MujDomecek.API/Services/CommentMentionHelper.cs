using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MujDomecek.Application.DTOs;
using MujDomecek.Infrastructure.Persistence;

namespace MujDomecek.API.Services;

public static class CommentMentionHelper
{
    private static readonly Regex MentionRegex = new(
        @"@(?<name>[\p{L}\p{M}][\p{L}\p{M}\p{N}._-]*)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static IReadOnlyList<string> ExtractMentionTokens(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Array.Empty<string>();

        var matches = MentionRegex.Matches(content);
        if (matches.Count == 0)
            return Array.Empty<string>();

        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in matches)
        {
            if (!match.Success)
                continue;

            var value = match.Groups["name"].Value;
            if (!string.IsNullOrWhiteSpace(value))
                tokens.Add(value.Trim());
        }

        return tokens.ToList();
    }

    public static async Task<IReadOnlyList<MentionCandidate>> GetMentionCandidatesAsync(
        ApplicationDbContext dbContext,
        Guid propertyId)
    {
        var projectId = await dbContext.Properties
            .Where(p => p.Id == propertyId)
            .Select(p => p.ProjectId)
            .FirstOrDefaultAsync();

        if (projectId == Guid.Empty)
            return Array.Empty<MentionCandidate>();

        var ownerId = await dbContext.Projects
            .Where(p => p.Id == projectId)
            .Select(p => p.OwnerId)
            .FirstOrDefaultAsync();

        var projectMembers = await dbContext.ProjectMembers
            .Where(m => m.ProjectId == projectId)
            .Select(m => m.UserId)
            .ToListAsync();

        var propertyMembers = await dbContext.PropertyMembers
            .Where(m => m.PropertyId == propertyId)
            .Select(m => m.UserId)
            .ToListAsync();

        var userIds = projectMembers
            .Concat(propertyMembers)
            .Append(ownerId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (userIds.Count == 0)
            return Array.Empty<MentionCandidate>();

        return await dbContext.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new MentionCandidate(u.Id, u.FirstName, u.LastName, u.Email))
            .ToListAsync();
    }

    public static IReadOnlyList<MentionDto> ResolveMentions(
        IReadOnlyList<string> tokens,
        IReadOnlyList<MentionCandidate> candidates,
        Guid? excludeUserId = null)
    {
        if (tokens.Count == 0 || candidates.Count == 0)
            return Array.Empty<MentionDto>();

        var lookup = BuildLookup(candidates);
        var results = new List<MentionDto>();
        var seen = new HashSet<Guid>();

        foreach (var token in tokens)
        {
            var normalized = NormalizeMentionKey(token);
            if (normalized is null)
                continue;

            if (!lookup.TryGetValue(normalized, out var matches))
                continue;

            if (matches.Count != 1)
                continue;

            var candidate = matches[0];
            if (excludeUserId.HasValue && candidate.Id == excludeUserId.Value)
                continue;

            if (seen.Add(candidate.Id))
                results.Add(new MentionDto(candidate.Id, BuildDisplayName(candidate.FirstName, candidate.LastName, candidate.Email)));
        }

        return results;
    }

    public static async Task<IReadOnlyDictionary<Guid, List<MentionDto>>> LoadMentionLookupAsync(
        ApplicationDbContext dbContext,
        IReadOnlyCollection<Guid> commentIds)
    {
        if (commentIds.Count == 0)
            return new Dictionary<Guid, List<MentionDto>>();

        var rows = await dbContext.CommentMentions
            .Where(m => commentIds.Contains(m.CommentId))
            .Join(dbContext.Users,
                m => m.MentionedUserId,
                u => u.Id,
                (m, u) => new
                {
                    m.CommentId,
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.Email
                })
            .ToListAsync();

        return rows
            .GroupBy(r => r.CommentId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(r => new MentionDto(r.Id, BuildDisplayName(r.FirstName, r.LastName, r.Email))).ToList());
    }

    private static Dictionary<string, List<MentionCandidate>> BuildLookup(IEnumerable<MentionCandidate> candidates)
    {
        var lookup = new Dictionary<string, List<MentionCandidate>>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in candidates)
        {
            foreach (var key in GetMentionKeys(candidate))
            {
                var normalized = NormalizeMentionKey(key);
                if (normalized is null)
                    continue;

                if (!lookup.TryGetValue(normalized, out var list))
                {
                    list = [];
                    lookup[normalized] = list;
                }

                list.Add(candidate);
            }
        }

        return lookup;
    }

    private static IEnumerable<string> GetMentionKeys(MentionCandidate candidate)
    {
        if (!string.IsNullOrWhiteSpace(candidate.FirstName))
            yield return candidate.FirstName;

        if (!string.IsNullOrWhiteSpace(candidate.LastName))
            yield return candidate.LastName;

        var fullName = $"{candidate.FirstName} {candidate.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
            yield return fullName;

        if (!string.IsNullOrWhiteSpace(candidate.Email))
        {
            var parts = candidate.Email.Split('@', 2);
            if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
                yield return parts[0];
        }
    }

    private static string? NormalizeMentionKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim();
    }

    private static string BuildDisplayName(string? firstName, string? lastName, string? email)
    {
        var name = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? email ?? string.Empty : name;
    }

    public sealed record MentionCandidate(Guid Id, string? FirstName, string? LastName, string? Email);
}
