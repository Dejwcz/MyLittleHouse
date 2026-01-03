using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MujDomecek.API.Options.Jobs;
using MujDomecek.API.Serialization;
using MujDomecek.API.Services;
using MujDomecek.Application.Abstractions;
using MujDomecek.Domain.Aggregates.Zaznam;
using MujDomecek.Domain.ValueObjects;
using MujDomecek.Infrastructure.Persistence;
using Quartz;

namespace MujDomecek.API.Jobs;

public sealed class ExportDataJob : IJob
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ExportDataOptions _options;
    private readonly IStorageService _storageService;
    private readonly IEmailSender _emailSender;
    private readonly EmailTemplateService _emailTemplates;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<ExportDataJob> _logger;

    public ExportDataJob(
        ApplicationDbContext dbContext,
        IOptions<ExportDataOptions> options,
        IStorageService storageService,
        IEmailSender emailSender,
        EmailTemplateService emailTemplates,
        IHostEnvironment environment,
        ILogger<ExportDataJob> logger)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _storageService = storageService;
        _emailSender = emailSender;
        _emailTemplates = emailTemplates;
        _environment = environment;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var userId = ParseGuid(context.MergedJobDataMap.GetString("userId"));
        if (userId == Guid.Empty)
            return;

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, context.CancellationToken);
        if (user is null || string.IsNullOrWhiteSpace(user.Email))
            return;

        var now = DateTime.UtcNow;
        var format = context.MergedJobDataMap.GetString("format");
        var useJson = string.Equals(format, "json", StringComparison.OrdinalIgnoreCase);

        var preferences = await _dbContext.UserPreferences
            .Where(p => p.UserId == userId)
            .Select(p => new
            {
                p.PushNewComments,
                p.PushMentions,
                p.PushSharedActivity,
                p.PushDraftReminders,
                p.EmailWeeklySummary,
                p.EmailInvitations,
                p.SyncEnabled,
                p.SyncOnMobileData
            })
            .FirstOrDefaultAsync(context.CancellationToken)
            ?? new
            {
                PushNewComments = true,
                PushMentions = true,
                PushSharedActivity = true,
                PushDraftReminders = true,
                EmailWeeklySummary = false,
                EmailInvitations = true,
                SyncEnabled = true,
                SyncOnMobileData = false
            };

        var projects = await _dbContext.Projects
            .IgnoreQueryFilters()
            .Where(p => p.OwnerId == userId)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.CreatedAt,
                p.UpdatedAt,
                p.IsDeleted,
                p.DeletedAt
            })
            .ToListAsync(context.CancellationToken);

        var projectIds = projects.Select(p => p.Id).ToList();

        var properties = await _dbContext.Properties
            .IgnoreQueryFilters()
            .Where(p => projectIds.Contains(p.ProjectId))
            .Select(p => new
            {
                p.Id,
                p.ProjectId,
                p.Name,
                p.Description,
                p.Latitude,
                p.Longitude,
                p.GeoRadius,
                p.CreatedAt,
                p.UpdatedAt,
                p.IsDeleted,
                p.DeletedAt
            })
            .ToListAsync(context.CancellationToken);

        var propertyIds = properties.Select(p => p.Id).ToList();

        var units = await _dbContext.Units
            .IgnoreQueryFilters()
            .Where(u => propertyIds.Contains(u.PropertyId))
            .Select(u => new
            {
                u.Id,
                u.PropertyId,
                u.ParentUnitId,
                u.Name,
                u.Description,
                UnitType = u.UnitType.ToString().ToLowerInvariant(),
                u.CreatedAt,
                u.UpdatedAt,
                u.IsDeleted,
                u.DeletedAt
            })
            .ToListAsync(context.CancellationToken);

        var zaznamy = await _dbContext.Zaznamy
            .IgnoreQueryFilters()
            .Where(z => propertyIds.Contains(z.PropertyId))
            .Select(z => new
            {
                z.Id,
                z.PropertyId,
                z.UnitId,
                z.Title,
                z.Description,
                z.Date,
                z.Cost,
                Status = z.Status.ToString().ToLowerInvariant(),
                Flags = FlagsToStrings(z.Flags),
                z.CreatedAt,
                z.UpdatedAt,
                z.IsDeleted,
                z.DeletedAt
            })
            .ToListAsync(context.CancellationToken);

        var zaznamIds = zaznamy.Select(z => z.Id).ToList();

        var tagLinks = await _dbContext.ZaznamTags
            .Where(t => zaznamIds.Contains(t.ZaznamId))
            .Select(t => new { t.ZaznamId, t.TagId })
            .ToListAsync(context.CancellationToken);

        var tagIds = tagLinks.Select(t => t.TagId).Distinct().ToList();
        var tags = await _dbContext.Tags
            .Where(t => tagIds.Contains(t.Id))
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Icon,
                t.SortOrder,
                t.IsActive
            })
            .ToListAsync(context.CancellationToken);

        var documentsQuery = _dbContext.Media
            .IgnoreQueryFilters()
            .Where(d => d.OwnerType == OwnerType.Zaznam && zaznamIds.Contains(d.OwnerId));

        if (!_options.IncludePhotos)
            documentsQuery = documentsQuery.Where(d => d.Type != MediaType.Photo);

        var documents = await documentsQuery
            .Select(d => new
            {
                d.Id,
                ZaznamId = d.OwnerId,
                Type = d.Type.ToString().ToLowerInvariant(),
                d.StorageKey,
                d.OriginalFileName,
                d.MimeType,
                d.SizeBytes,
                d.Description,
                d.CreatedAt,
                d.UpdatedAt,
                d.IsDeleted,
                d.DeletedAt
            })
            .ToListAsync(context.CancellationToken);

        var comments = await _dbContext.Comments
            .IgnoreQueryFilters()
            .Where(c => zaznamIds.Contains(c.ZaznamId))
            .Select(c => new
            {
                c.Id,
                c.ZaznamId,
                c.AuthorUserId,
                c.Content,
                c.CreatedAt,
                c.UpdatedAt,
                c.IsDeleted,
                c.DeletedAt
            })
            .ToListAsync(context.CancellationToken);

        var contacts = await _dbContext.Contacts
            .Where(c => c.OwnerUserId == userId)
            .Select(c => new { c.Id, c.Email, c.DisplayName, c.CreatedAt })
            .ToListAsync(context.CancellationToken);

        var contactGroups = await _dbContext.ContactGroups
            .Where(g => g.OwnerUserId == userId)
            .Select(g => new { g.Id, g.Name, g.CreatedAt })
            .ToListAsync(context.CancellationToken);

        var contactGroupIds = contactGroups.Select(g => g.Id).ToList();
        var contactGroupMembers = await _dbContext.ContactGroupMembers
            .Where(m => contactGroupIds.Contains(m.GroupId))
            .Select(m => new { m.Id, m.GroupId, m.ContactId })
            .ToListAsync(context.CancellationToken);

        var notifications = await _dbContext.Notifications
            .Where(n => n.UserId == userId)
            .Select(n => new
            {
                n.Id,
                Type = n.Type.ToString().ToLowerInvariant(),
                n.PayloadJson,
                n.ReadAt,
                n.CreatedAt
            })
            .ToListAsync(context.CancellationToken);

        var invitations = await _dbContext.Invitations
            .Where(i => i.CreatedBy == userId)
            .Select(i => new
            {
                i.Id,
                TargetType = i.TargetType.ToString().ToLowerInvariant(),
                i.TargetId,
                i.Email,
                Role = i.Role.ToString().ToLowerInvariant(),
                i.PermissionsJson,
                Status = i.Status.ToString().ToLowerInvariant(),
                i.CreatedAt,
                i.ExpiresAt
            })
            .ToListAsync(context.CancellationToken);

        var projectMembers = await _dbContext.ProjectMembers
            .Where(m => projectIds.Contains(m.ProjectId))
            .Select(m => new
            {
                m.Id,
                m.ProjectId,
                m.UserId,
                Role = m.Role.ToString().ToLowerInvariant(),
                m.PermissionsJson,
                m.CreatedAt
            })
            .ToListAsync(context.CancellationToken);

        var propertyMembers = await _dbContext.PropertyMembers
            .Where(m => propertyIds.Contains(m.PropertyId))
            .Select(m => new
            {
                m.Id,
                m.PropertyId,
                m.UserId,
                Role = m.Role.ToString().ToLowerInvariant(),
                m.PermissionsJson,
                m.CreatedAt
            })
            .ToListAsync(context.CancellationToken);

        var exportPayload = new
        {
            generatedAt = now,
            user = new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.PhoneNumber,
                user.PreferredLanguage,
                ThemePreference = user.ThemePreference.ToString().ToLowerInvariant(),
                user.CreatedAt
            },
            preferences,
            projects,
            projectMembers,
            properties,
            propertyMembers,
            units,
            zaznamy,
            zaznamTags = tagLinks,
            tags,
            documents,
            comments,
            contacts,
            contactGroups,
            contactGroupMembers,
            invitations,
            notifications
        };

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        jsonOptions.Converters.Add(new DateOnlyJsonConverter());

        var json = JsonSerializer.Serialize(exportPayload, jsonOptions);
        var storageKeys = documents
            .Select(d => d.StorageKey)
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct()
            .ToList();

        var includeFiles = _options.IncludePhotos && !useJson;
        var exportPath = await WriteExportAsync(
            userId,
            json,
            storageKeys,
            includeFiles,
            useJson,
            context.CancellationToken);
        if (string.IsNullOrWhiteSpace(exportPath))
            return;

        var fileInfo = new FileInfo(exportPath);
        if (fileInfo.Length > _options.MaxSizeBytes)
        {
            _logger.LogWarning(
                "ExportData: export too large ({Size} bytes) for user {UserId}",
                fileInfo.Length,
                userId);
            return;
        }

        var storageKey = $"exports/{userId:D}/{Path.GetFileName(exportPath)}";
        await using (var exportStream = File.OpenRead(exportPath))
        {
            await _storageService.UploadAsync(
                storageKey,
                exportStream,
                useJson ? "application/json" : "application/zip",
                context.CancellationToken);
        }

        var download = await _storageService.GetDownloadUrlAsync(
            storageKey,
            TimeSpan.FromHours(_options.ExpirationHours),
            context.CancellationToken);
        var downloadUrl = download.Url;
        var expiresAt = download.ExpiresAt;

        var exportEmail = _emailTemplates.Render(
            "export-ready",
            new Dictionary<string, string?>
            {
                ["userName"] = BuildDisplayName(user.FirstName, user.LastName, user.Email),
                ["downloadUrl"] = downloadUrl,
                ["expiresAt"] = expiresAt.ToString("O"),
                ["sizeBytes"] = fileInfo.Length.ToString()
            });

        await _emailSender.SendAsync(user.Email ?? string.Empty, exportEmail.Subject, exportEmail.Body, context.CancellationToken);
        _logger.LogInformation("ExportData: Export ready for user {UserId}", userId);
    }

    private Task<string?> WriteExportAsync(
        Guid userId,
        string json,
        IReadOnlyList<string> storageKeys,
        bool includeFiles,
        bool asJson,
        CancellationToken ct)
    {
        return asJson
            ? WriteJsonExportAsync(userId, json, ct)
            : WriteZipExportAsync(userId, json, storageKeys, includeFiles, ct);
    }

    private async Task<string?> WriteJsonExportAsync(Guid userId, string json, CancellationToken ct)
    {
        var exportDir = Path.Combine(_environment.ContentRootPath, "exports", userId.ToString("D"));
        Directory.CreateDirectory(exportDir);

        var fileName = $"export_{DateTime.UtcNow:yyyyMMddHHmmss}.json";
        var filePath = Path.Combine(exportDir, fileName);

        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await using var writer = new StreamWriter(fileStream, Encoding.UTF8);
        await writer.WriteAsync(json.AsMemory(), ct);

        return filePath;
    }

    private async Task<string?> WriteZipExportAsync(
        Guid userId,
        string json,
        IReadOnlyList<string> storageKeys,
        bool includeFiles,
        CancellationToken ct)
    {
        var exportDir = Path.Combine(_environment.ContentRootPath, "exports", userId.ToString("D"));
        Directory.CreateDirectory(exportDir);

        var fileName = $"export_{DateTime.UtcNow:yyyyMMddHHmmss}.zip";
        var filePath = Path.Combine(exportDir, fileName);

        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create);
        var entry = archive.CreateEntry("export.json", CompressionLevel.Optimal);
        await using (var entryStream = entry.Open())
        await using (var writer = new StreamWriter(entryStream, Encoding.UTF8))
        {
            await writer.WriteAsync(json.AsMemory(), ct);
        }

        if (includeFiles)
            await AddFilesAsync(archive, storageKeys, ct);

        return filePath;
    }

    private async Task AddFilesAsync(ZipArchive archive, IReadOnlyList<string> storageKeys, CancellationToken ct)
    {
        if (storageKeys.Count == 0)
            return;

        foreach (var storageKey in storageKeys)
        {
            var normalizedKey = NormalizeKey(storageKey);
            if (string.IsNullOrWhiteSpace(normalizedKey) || normalizedKey.Contains("..", StringComparison.Ordinal))
                continue;

            await using var source = await _storageService.OpenReadAsync(normalizedKey, ct);
            if (source is null)
                continue;

            var entryPath = $"files/{normalizedKey}";
            var fileEntry = archive.CreateEntry(entryPath, CompressionLevel.Optimal);
            await using var target = fileEntry.Open();
            await source.CopyToAsync(target, ct);
        }
    }

    private static string NormalizeKey(string storageKey)
    {
        return storageKey.Replace('\\', '/').TrimStart('/');
    }

    private static Guid ParseGuid(string? value)
    {
        return Guid.TryParse(value, out var parsed) ? parsed : Guid.Empty;
    }

    private static IReadOnlyList<string> FlagsToStrings(ZaznamFlags flags)
    {
        if (flags == ZaznamFlags.None)
            return Array.Empty<string>();

        return Enum.GetValues<ZaznamFlags>()
            .Where(f => f != ZaznamFlags.None && flags.HasFlag(f))
            .Select(f => f.ToString().ToLowerInvariant())
            .ToList();
    }

    private static string BuildDisplayName(string? firstName, string? lastName, string? email)
    {
        var name = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? email ?? string.Empty : name;
    }
}
