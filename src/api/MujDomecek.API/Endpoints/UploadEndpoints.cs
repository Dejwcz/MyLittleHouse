using System.Security.Claims;
using Microsoft.Extensions.Options;
using MujDomecek.API.Extensions;
using MujDomecek.Application.Abstractions;
using MujDomecek.Application.DTOs;
using MujDomecek.Infrastructure.Options;

namespace MujDomecek.API.Endpoints;

public static class UploadEndpoints
{
    public static IEndpointRouteBuilder MapUploadEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/upload").RequireAuthorization();

        group.MapPost("/request", RequestUploadAsync);
        group.MapPost("/confirm", ConfirmUploadAsync);
        group.MapDelete("/{storageKey}", DeleteUploadAsync);

        return endpoints;
    }

    private static async Task<IResult> RequestUploadAsync(
        ClaimsPrincipal user,
        [FromBody] UploadRequestRequest request,
        IStorageService storageService,
        IOptions<StorageOptions> storageOptions)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        if (!IsAllowedMimeType(request.MimeType))
            return Results.BadRequest(new { error = "unsupported_mime_type" });

        if (request.SizeBytes <= 0)
            return Results.BadRequest(new { error = "invalid_size" });

        var storageKey = BuildStorageKey(userId, request.FileName);
        var expiresIn = GetPresignedExpiry(storageOptions.Value);
        var upload = await storageService.GetUploadUrlAsync(storageKey, request.MimeType, expiresIn);

        var response = new UploadRequestResponse(storageKey, upload.Url, upload.ExpiresAt);
        return await Task.FromResult(Results.Ok(response));
    }

    private static async Task<IResult> ConfirmUploadAsync(
        ClaimsPrincipal user,
        [FromBody] UploadConfirmRequest request,
        IStorageService storageService,
        IOptions<StorageOptions> storageOptions)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.StorageKey))
            return Results.BadRequest(new { error = "missing_storage_key" });

        var expiresIn = GetPresignedExpiry(storageOptions.Value);
        var download = await storageService.GetDownloadUrlAsync(request.StorageKey, expiresIn);
        var thumbnailUrl = storageService.GetThumbnailUrl(request.StorageKey);

        var response = new UploadConfirmResponse(request.StorageKey, download.Url, thumbnailUrl);
        return await Task.FromResult(Results.Ok(response));
    }

    private static async Task<IResult> DeleteUploadAsync(
        ClaimsPrincipal user,
        string storageKey,
        IStorageService storageService)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(storageKey))
            return Results.BadRequest();

        await storageService.DeleteAsync(storageKey);
        return await Task.FromResult(Results.NoContent());
    }

    private static bool IsAllowedMimeType(string? mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
            return false;

        return mimeType.ToLowerInvariant() switch
        {
            "image/jpeg" => true,
            "image/png" => true,
            "image/webp" => true,
            "image/heic" => true,
            "application/pdf" => true,
            _ => false
        };
    }

    private static string BuildStorageKey(Guid userId, string fileName)
    {
        var extension = Path.GetExtension(fileName) ?? string.Empty;
        return $"uploads/{userId:D}/{Guid.NewGuid():N}{extension}";
    }

    private static TimeSpan GetPresignedExpiry(StorageOptions options)
    {
        var minutes = options.PresignedExpiryMinutes;
        return minutes > 0 ? TimeSpan.FromMinutes(minutes) : TimeSpan.FromMinutes(60);
    }
}

