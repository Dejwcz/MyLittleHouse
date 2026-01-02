using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MujDomecek.Application.Abstractions;
using MujDomecek.API.Extensions;
using MujDomecek.API.Jobs;
using MujDomecek.Application.DTOs;
using MujDomecek.Domain.Aggregates.User;
using MujDomecek.Infrastructure.Persistence;
using Quartz;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace MujDomecek.API.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/users").RequireAuthorization();

        group.MapGet("/me", GetProfileAsync);
        group.MapPut("/me", UpdateProfileAsync);
        group.MapDelete("/me", DeleteAccountAsync);
        group.MapPost("/me/avatar", UploadAvatarAsync);
        group.MapDelete("/me/avatar", DeleteAvatarAsync);
        group.MapGet("/me/preferences", GetPreferencesAsync);
        group.MapPut("/me/preferences", UpdatePreferencesAsync);
        group.MapPost("/me/export", ExportDataAsync);

        return endpoints;
    }

    private static async Task<IResult> GetProfileAsync(
        ClaimsPrincipal user,
        UserManager<AppUser> userManager,
        IStorageService storageService)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var appUser = await userManager.FindByIdAsync(userId.ToString());
        if (appUser is null)
            return Results.NotFound();

        var hasPassword = await userManager.HasPasswordAsync(appUser);
        return Results.Ok(ToUserProfile(appUser, hasPassword, storageService));
    }

    private static async Task<IResult> UpdateProfileAsync(
        ClaimsPrincipal user,
        [FromBody] UpdateProfileRequest request,
        UserManager<AppUser> userManager,
        IStorageService storageService)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var appUser = await userManager.FindByIdAsync(userId.ToString());
        if (appUser is null)
            return Results.NotFound();

        if (!string.IsNullOrWhiteSpace(request.FirstName))
            appUser.FirstName = request.FirstName;
        if (!string.IsNullOrWhiteSpace(request.LastName))
            appUser.LastName = request.LastName;
        if (!string.IsNullOrWhiteSpace(request.Phone))
            appUser.PhoneNumber = request.Phone;
        if (!string.IsNullOrWhiteSpace(request.PreferredLanguage))
            appUser.PreferredLanguage = request.PreferredLanguage;
        if (!string.IsNullOrWhiteSpace(request.ThemePreference))
            appUser.ThemePreference = Enum.TryParse<ThemePreference>(request.ThemePreference, true, out var pref)
                ? pref
                : appUser.ThemePreference;

        var result = await userManager.UpdateAsync(appUser);
        if (!result.Succeeded)
            return Results.BadRequest(result.Errors);

        var hasPassword = await userManager.HasPasswordAsync(appUser);
        return Results.Ok(ToUserProfile(appUser, hasPassword, storageService));
    }

    private static async Task<IResult> DeleteAccountAsync(
        ClaimsPrincipal user,
        [FromBody] DeleteAccountRequest request,
        UserManager<AppUser> userManager,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        if (!string.Equals(request.Confirmation, "DELETE_MY_ACCOUNT", StringComparison.Ordinal))
            return Results.BadRequest(new { error = "invalid_confirmation" });

        var appUser = await dbContext.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
        if (appUser is null)
            return Results.NotFound();

        if (appUser.IsDeleted)
            return Results.NoContent();

        var hasPassword = await userManager.HasPasswordAsync(appUser);
        if (hasPassword)
        {
            var passwordOk = await userManager.CheckPasswordAsync(appUser, request.Password ?? string.Empty);
            if (!passwordOk)
                return Results.BadRequest(new { error = "invalid_password" });
        }

        var deletedEmail = $"deleted-{appUser.Id}@deleted.local";
        appUser.IsDeleted = true;
        appUser.DeletedAt = DateTime.UtcNow;
        appUser.Email = deletedEmail;
        appUser.UserName = deletedEmail;
        appUser.NormalizedEmail = deletedEmail.ToUpperInvariant();
        appUser.NormalizedUserName = deletedEmail.ToUpperInvariant();
        appUser.EmailConfirmed = false;
        appUser.PhoneNumber = null;
        appUser.AvatarStorageKey = null;
        appUser.GoogleId = null;
        appUser.AppleId = null;

        var tokens = await dbContext.RefreshTokens
            .Where(t => t.UserId == appUser.Id && t.RevokedAt == null)
            .ToListAsync();
        foreach (var token in tokens)
            token.RevokedAt = DateTime.UtcNow;

        var result = await userManager.UpdateAsync(appUser);
        if (!result.Succeeded)
            return Results.BadRequest(result.Errors);

        return Results.NoContent();
    }

    private static async Task<IResult> UploadAvatarAsync(
        ClaimsPrincipal user,
        IFormFile file,
        UserManager<AppUser> userManager,
        IStorageService storageService,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        if (file is null || file.Length == 0)
            return Results.BadRequest(new { error = "missing_file" });

        if (file.Length > AvatarMaxBytes)
            return Results.BadRequest(new { error = "file_too_large" });

        if (!IsAllowedAvatarType(file.ContentType))
            return Results.BadRequest(new { error = "unsupported_format" });

        var appUser = await userManager.FindByIdAsync(userId.ToString());
        if (appUser is null)
            return Results.NotFound();

        var extension = GetAvatarExtension(file);
        if (string.IsNullOrWhiteSpace(extension))
            return Results.BadRequest(new { error = "unsupported_format" });

        var storageKey = $"avatars/{userId:D}/{Guid.NewGuid():N}{extension}";
        var thumbKey = BuildAvatarThumbKey(storageKey);
        var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "image/jpeg" : file.ContentType;
        var encoder = GetAvatarEncoder(contentType);

        try
        {
            await using var input = file.OpenReadStream();
            using var image = await Image.LoadAsync(input, ct);

            await using var fullStream = new MemoryStream();
            await using var thumbStream = new MemoryStream();

            using (var full = image.Clone(ctx => ctx.Resize(new ResizeOptions
                   {
                       Mode = ResizeMode.Crop,
                       Size = new Size(AvatarSize, AvatarSize)
                   })))
            {
                await full.SaveAsync(fullStream, encoder, ct);
            }

            using (var thumb = image.Clone(ctx => ctx.Resize(new ResizeOptions
                   {
                       Mode = ResizeMode.Crop,
                       Size = new Size(AvatarThumbSize, AvatarThumbSize)
                   })))
            {
                await thumb.SaveAsync(thumbStream, encoder, ct);
            }

            fullStream.Position = 0;
            thumbStream.Position = 0;

            await storageService.UploadAsync(storageKey, fullStream, contentType, ct);
            await storageService.UploadAsync(thumbKey, thumbStream, contentType, ct);
        }
        catch (UnknownImageFormatException)
        {
            return Results.BadRequest(new { error = "invalid_image" });
        }

        var previousKey = appUser.AvatarStorageKey;
        appUser.AvatarStorageKey = storageKey;

        var result = await userManager.UpdateAsync(appUser);
        if (!result.Succeeded)
        {
            await storageService.DeleteAsync(storageKey, ct);
            await storageService.DeleteAsync(thumbKey, ct);
            return Results.BadRequest(result.Errors);
        }

        if (!string.IsNullOrWhiteSpace(previousKey))
        {
            await storageService.DeleteAsync(previousKey, ct);
            await storageService.DeleteAsync(BuildAvatarThumbKey(previousKey), ct);
        }

        var url = storageService.GetPublicUrl(storageKey) ?? storageKey;
        return Results.Ok(new AvatarUploadResponse(url));
    }

    private static async Task<IResult> DeleteAvatarAsync(
        ClaimsPrincipal user,
        UserManager<AppUser> userManager,
        IStorageService storageService,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var appUser = await userManager.FindByIdAsync(userId.ToString());
        if (appUser is null)
            return Results.NotFound();

        if (string.IsNullOrWhiteSpace(appUser.AvatarStorageKey))
            return Results.NoContent();

        var previousKey = appUser.AvatarStorageKey;
        appUser.AvatarStorageKey = null;
        var result = await userManager.UpdateAsync(appUser);
        if (!result.Succeeded)
            return Results.BadRequest(result.Errors);

        if (!string.IsNullOrWhiteSpace(previousKey))
        {
            await storageService.DeleteAsync(previousKey, ct);
            await storageService.DeleteAsync(BuildAvatarThumbKey(previousKey), ct);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> ExportDataAsync(
        ClaimsPrincipal user,
        [FromBody] ExportDataRequest? request,
        ISchedulerFactory schedulerFactory)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var format = request?.Format?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(format) && format is not ("json" or "zip"))
            return Results.BadRequest(new { error = "invalid_format" });

        var normalizedFormat = string.IsNullOrWhiteSpace(format) ? "json" : format;

        var scheduler = await schedulerFactory.GetScheduler();
        var jobKey = new JobKey($"ExportData-{userId:D}-{Guid.NewGuid():N}");
        var job = JobBuilder.Create<ExportDataJob>()
            .WithIdentity(jobKey)
            .UsingJobData("userId", userId.ToString("D"))
            .UsingJobData("format", normalizedFormat)
            .Build();

        var trigger = TriggerBuilder.Create()
            .ForJob(job)
            .StartNow()
            .Build();

        await scheduler.ScheduleJob(job, trigger);

        return Results.Accepted();
    }

    private static async Task<IResult> GetPreferencesAsync(
        ClaimsPrincipal user,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var prefs = await dbContext.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId)
            ?? new UserPreferences { UserId = userId };

        return Results.Ok(ToPreferencesDto(prefs));
    }

    private static async Task<IResult> UpdatePreferencesAsync(
        ClaimsPrincipal user,
        [FromBody] UpdatePreferencesRequest request,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var prefs = await dbContext.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
        if (prefs is null)
        {
            prefs = new UserPreferences { UserId = userId };
            dbContext.UserPreferences.Add(prefs);
        }

        if (request.Push is not null)
        {
            prefs.PushNewComments = request.Push.NewComments ?? prefs.PushNewComments;
            prefs.PushMentions = request.Push.Mentions ?? prefs.PushMentions;
            prefs.PushSharedActivity = request.Push.SharedActivity ?? prefs.PushSharedActivity;
            prefs.PushDraftReminders = request.Push.DraftReminders ?? prefs.PushDraftReminders;
        }

        if (request.Email is not null)
        {
            prefs.EmailWeeklySummary = request.Email.WeeklySummary ?? prefs.EmailWeeklySummary;
            prefs.EmailInvitations = request.Email.Invitations ?? prefs.EmailInvitations;
        }

        if (request.Sync is not null)
        {
            prefs.SyncEnabled = request.Sync.Enabled ?? prefs.SyncEnabled;
            prefs.SyncOnMobileData = request.Sync.OnMobileData ?? prefs.SyncOnMobileData;
        }

        await dbContext.SaveChangesAsync();
        return Results.Ok(ToPreferencesDto(prefs));
    }

    private static UserProfileResponse ToUserProfile(
        AppUser user,
        bool hasPassword,
        IStorageService storageService)
    {
        var avatarUrl = BuildAvatarUrl(user, storageService);
        return new UserProfileResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email ?? string.Empty,
            user.PhoneNumber,
            avatarUrl,
            user.PreferredLanguage,
            user.ThemePreference.ToString().ToLowerInvariant(),
            hasPassword,
            new LinkedAccountsDto(user.GoogleId, user.AppleId),
            user.CreatedAt);
    }

    private static UserPreferencesDto ToPreferencesDto(UserPreferences prefs)
    {
        return new UserPreferencesDto(
            new PushPreferencesDto(
                prefs.PushNewComments,
                prefs.PushMentions,
                prefs.PushSharedActivity,
                prefs.PushDraftReminders),
            new EmailPreferencesDto(
                prefs.EmailWeeklySummary,
                prefs.EmailInvitations),
            new SyncPreferencesDto(
                prefs.SyncEnabled,
                prefs.SyncOnMobileData));
    }

    private static string? BuildAvatarUrl(AppUser user, IStorageService storageService)
    {
        if (string.IsNullOrWhiteSpace(user.AvatarStorageKey))
            return null;

        return storageService.GetPublicUrl(user.AvatarStorageKey);
    }
    

    private static bool IsAllowedAvatarType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => true,
            "image/png" => true,
            "image/webp" => true,
            _ => false
        };
    }

    private static string GetAvatarExtension(IFormFile file)
    {
        var extension = file.ContentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => string.Empty
        };

        if (!string.IsNullOrWhiteSpace(extension))
            return extension;

        var fileExtension = Path.GetExtension(file.FileName);
        return string.IsNullOrWhiteSpace(fileExtension) ? string.Empty : fileExtension.ToLowerInvariant();
    }

    private static string BuildAvatarThumbKey(string storageKey)
    {
        var extension = Path.GetExtension(storageKey);
        if (string.IsNullOrWhiteSpace(extension))
            return $"{storageKey}_thumb";

        return storageKey.Replace(extension, $"_thumb{extension}", StringComparison.OrdinalIgnoreCase);
    }

    private static IImageEncoder GetAvatarEncoder(string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "image/png" => new PngEncoder(),
            "image/webp" => new WebpEncoder(),
            _ => new JpegEncoder { Quality = 90 }
        };
    }

    private const int AvatarSize = 256;
    private const int AvatarThumbSize = 64;
    private const long AvatarMaxBytes = 2 * 1024 * 1024;
}

