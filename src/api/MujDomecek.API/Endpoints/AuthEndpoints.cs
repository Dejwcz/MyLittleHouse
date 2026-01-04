using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MujDomecek.API.Extensions;
using MujDomecek.API.Options;
using MujDomecek.API.Services;
using MujDomecek.Application.Abstractions;
using MujDomecek.Application.DTOs;
using MujDomecek.Domain.Aggregates.User;
using MujDomecek.Infrastructure.Persistence;

namespace MujDomecek.API.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth");

        group.MapPost("/register", RegisterAsync);
        group.MapGet("/confirm-email", ConfirmEmailAsync);
        group.MapPost("/resend-confirmation", ResendConfirmationAsync);
        group.MapPost("/login", LoginAsync);
        group.MapPost("/logout", LogoutAsync).RequireAuthorization();
        group.MapPost("/refresh", RefreshAsync);
        group.MapPost("/forgot-password", ForgotPasswordAsync);
        group.MapGet("/validate-reset-token", ValidateResetTokenAsync);
        group.MapPost("/reset-password", ResetPasswordAsync);
        group.MapPost("/change-password", ChangePasswordAsync).RequireAuthorization();

        group.MapGet("/google", GoogleLoginAsync);
        group.MapGet("/google/callback", GoogleCallbackAsync);
        group.MapGet("/apple", AppleLoginAsync);
        group.MapGet("/apple/callback", AppleCallbackAsync);
        group.MapPost("/link-google", LinkGoogleAsync).RequireAuthorization();
        group.MapPost("/unlink-google", UnlinkGoogleAsync).RequireAuthorization();
        group.MapPost("/link-apple", LinkAppleAsync).RequireAuthorization();
        group.MapPost("/unlink-apple", UnlinkAppleAsync).RequireAuthorization();

        group.MapGet("/sessions", GetSessionsAsync).RequireAuthorization();
        group.MapDelete("/sessions/{id:guid}", RevokeSessionAsync).RequireAuthorization();
        group.MapPost("/sessions/revoke-all", RevokeAllSessionsAsync).RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterRequest request,
        HttpRequest httpRequest,
        UserManager<AppUser> userManager,
        ApplicationDbContext dbContext,
        IEmailDispatcher emailDispatcher,
        EmailTemplateService emailTemplates,
        IClock clock,
        IConfiguration configuration)
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CreatedAt = clock.UtcNow
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return Results.BadRequest(ToApiError(result.Errors));

        dbContext.UserPreferences.Add(new UserPreferences { UserId = user.Id });
        await dbContext.SaveChangesAsync();

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmUrl = BuildConfirmEmailUrl(httpRequest, configuration, user.Id, token);
        var confirmationEmail = emailTemplates.Render(
            "email-confirmation",
            new Dictionary<string, string?>
            {
                ["userName"] = BuildDisplayName(user.FirstName, user.LastName, user.Email),
                ["confirmUrl"] = confirmUrl
            });

        await emailDispatcher.SendAsync(new EmailJobRequest(
            user.Email ?? string.Empty,
            confirmationEmail.Subject,
            confirmationEmail.Body));

        return Results.Ok(new RegisterResponse(
            true,
            "Registration successful. Check your email to confirm your account.",
            user.Id));
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequest request,
        HttpRequest httpRequest,
        HttpResponse response,
        UserManager<AppUser> userManager,
        ITokenService tokenService,
        ApplicationDbContext dbContext,
        IOptions<JwtOptions> jwtOptions,
        IConfiguration configuration,
        IStorageService storageService)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null)
            return Results.Unauthorized();

        if (user.IsDeleted)
            return Results.Unauthorized();

        if (user.IsBlocked)
            return Results.Forbid();

        if (!user.EmailConfirmed)
            return Results.BadRequest(new { error = "email_not_confirmed" });

        var passwordOk = await userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordOk)
            return Results.Unauthorized();

        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokenService.CreateAccessToken(user, roles);
        var refreshToken = tokenService.CreateRefreshToken();
        var refreshTokenHash = HashToken(refreshToken);
        var now = DateTime.UtcNow;
        var refreshExpiresAt = now.AddDays(jwtOptions.Value.RefreshTokenDays);
        var deviceInfo = GetDeviceInfo(httpRequest);

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            DeviceInfo = deviceInfo,
            CreatedAt = now,
            LastUsedAt = now,
            ExpiresAt = refreshExpiresAt
        });

        user.LastLoginAt = now;
        await dbContext.SaveChangesAsync();

        response.Cookies.Append(
            "refreshToken",
            refreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                Expires = refreshExpiresAt
            });

        return Results.Ok(new LoginResponse(
            accessToken,
            tokenService.AccessTokenLifetimeSeconds,
            ToUserProfile(user, true, storageService)));
    }

    private static async Task<IResult> RefreshAsync(
        HttpRequest request,
        HttpResponse response,
        UserManager<AppUser> userManager,
        ITokenService tokenService,
        ApplicationDbContext dbContext,
        IOptions<JwtOptions> jwtOptions,
        IConfiguration configuration,
        IStorageService storageService)
    {
        if (!request.Cookies.TryGetValue("refreshToken", out var refreshToken) || string.IsNullOrWhiteSpace(refreshToken))
            return Results.Unauthorized();

        var refreshTokenHash = HashToken(refreshToken);
        var token = await dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == refreshTokenHash);
        if (token is null || token.ExpiresAt <= DateTime.UtcNow || token.RevokedAt is not null)
            return Results.Unauthorized();

        var user = await userManager.FindByIdAsync(token.UserId.ToString());
        if (user is null)
            return Results.Unauthorized();

        if (user.IsDeleted)
            return Results.Unauthorized();

        if (user.IsBlocked)
            return Results.Forbid();

        token.RevokedAt = DateTime.UtcNow;
        token.LastUsedAt = DateTime.UtcNow;

        var newRefreshToken = tokenService.CreateRefreshToken();
        var refreshExpiresAt = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenDays);
        var deviceInfo = GetDeviceInfo(request);
        var newToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(newRefreshToken),
            DeviceInfo = deviceInfo,
            CreatedAt = DateTime.UtcNow,
            LastUsedAt = DateTime.UtcNow,
            ExpiresAt = refreshExpiresAt,
            ReplacedByTokenId = token.Id
        };

        token.ReplacedByTokenId = newToken.Id;
        dbContext.RefreshTokens.Add(newToken);

        await dbContext.SaveChangesAsync();

        response.Cookies.Append(
            "refreshToken",
            newRefreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                Expires = refreshExpiresAt
            });

        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokenService.CreateAccessToken(user, roles);

        return Results.Ok(new LoginResponse(
            accessToken,
            tokenService.AccessTokenLifetimeSeconds,
            ToUserProfile(user, true, storageService)));
    }

    private static async Task<IResult> LogoutAsync(
        ClaimsPrincipal user,
        HttpRequest request,
        HttpResponse response,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        if (request.Cookies.TryGetValue("refreshToken", out var refreshToken)
            && !string.IsNullOrWhiteSpace(refreshToken))
        {
            var refreshTokenHash = HashToken(refreshToken);
            var token = await dbContext.RefreshTokens
                .FirstOrDefaultAsync(t => t.UserId == userId && t.TokenHash == refreshTokenHash);
            if (token is not null && token.RevokedAt is null)
                token.RevokedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();
        }

        response.Cookies.Delete("refreshToken");
        return Results.NoContent();
    }

    private static async Task<IResult> ConfirmEmailAsync(
        Guid userId,
        string? token,
        UserManager<AppUser> userManager)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(token))
            return Results.BadRequest(new { error = "invalid_token" });

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Results.BadRequest(new { error = "invalid_user" });

        var result = await userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
            return Results.BadRequest(ToApiError(result.Errors));

        return Results.Ok(new ConfirmEmailResponse(true, "Email confirmed."));
    }

    private static async Task<IResult> ResendConfirmationAsync(
        [FromBody] ResendConfirmationRequest request,
        HttpRequest httpRequest,
        UserManager<AppUser> userManager,
        IEmailDispatcher emailDispatcher,
        EmailTemplateService emailTemplates,
        IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return Results.NoContent();

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || user.EmailConfirmed)
            return Results.NoContent();

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmUrl = BuildConfirmEmailUrl(httpRequest, configuration, user.Id, token);

        var confirmationEmail = emailTemplates.Render(
            "email-confirmation",
            new Dictionary<string, string?>
            {
                ["userName"] = BuildDisplayName(user.FirstName, user.LastName, user.Email),
                ["confirmUrl"] = confirmUrl
            });

        await emailDispatcher.SendAsync(new EmailJobRequest(
            user.Email ?? string.Empty,
            confirmationEmail.Subject,
            confirmationEmail.Body));

        return Results.NoContent();
    }

    private static async Task<IResult> ForgotPasswordAsync(
        [FromBody] ForgotPasswordRequest request,
        HttpRequest httpRequest,
        UserManager<AppUser> userManager,
        ApplicationDbContext dbContext,
        IEmailDispatcher emailDispatcher,
        EmailTemplateService emailTemplates,
        IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return Results.NoContent();

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || user.IsDeleted)
            return Results.NoContent();

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var tokenHash = HashToken(token);
        var now = DateTime.UtcNow;
        var expiresAt = now.Add(PasswordResetTokenLifetime);

        var activeTokens = await dbContext.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null && t.ExpiresAt > now)
            .ToListAsync();
        dbContext.PasswordResetTokens.RemoveRange(activeTokens);

        dbContext.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            CreatedAt = now,
            ExpiresAt = expiresAt
        });

        await dbContext.SaveChangesAsync();

        var resetUrl = BuildResetPasswordUrl(httpRequest, configuration, token);
        var resetEmail = emailTemplates.Render(
            "password-reset",
            new Dictionary<string, string?>
            {
                ["userName"] = BuildDisplayName(user.FirstName, user.LastName, user.Email),
                ["resetUrl"] = resetUrl
            });

        await emailDispatcher.SendAsync(new EmailJobRequest(
            user.Email ?? string.Empty,
            resetEmail.Subject,
            resetEmail.Body));

        return Results.NoContent();
    }

    private static async Task<IResult> ValidateResetTokenAsync(
        string? token,
        ApplicationDbContext dbContext)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Results.Ok(new ValidateResetTokenResponse(false));

        var tokenHash = HashToken(token);
        var now = DateTime.UtcNow;
        var valid = await dbContext.PasswordResetTokens.AnyAsync(t =>
            t.TokenHash == tokenHash && t.UsedAt == null && t.ExpiresAt > now);

        return Results.Ok(new ValidateResetTokenResponse(valid));
    }

    private static async Task<IResult> ResetPasswordAsync(
        [FromBody] ResetPasswordRequest request,
        UserManager<AppUser> userManager,
        ApplicationDbContext dbContext)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
            return Results.BadRequest(new { error = "invalid_request" });

        var tokenHash = HashToken(request.Token);
        var now = DateTime.UtcNow;
        var token = await dbContext.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (token is null || token.UsedAt is not null || token.ExpiresAt <= now)
            return Results.BadRequest(new { error = "invalid_token" });

        var user = await userManager.FindByIdAsync(token.UserId.ToString());
        if (user is null || user.IsDeleted)
            return Results.BadRequest(new { error = "invalid_user" });

        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
            return Results.BadRequest(ToApiError(result.Errors));

        token.UsedAt = now;

        var refreshTokens = await dbContext.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAt == null)
            .ToListAsync();
        foreach (var refreshToken in refreshTokens)
            refreshToken.RevokedAt = now;

        await dbContext.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> ChangePasswordAsync(
        ClaimsPrincipal user,
        [FromBody] ChangePasswordRequest request,
        HttpRequest httpRequest,
        UserManager<AppUser> userManager,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var appUser = await userManager.FindByIdAsync(userId.ToString());
        if (appUser is null)
            return Results.NotFound();

        var result = await userManager.ChangePasswordAsync(appUser, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            return Results.BadRequest(ToApiError(result.Errors));

        var currentHash = GetRefreshTokenHash(httpRequest);
        var tokens = await dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync();

        foreach (var token in tokens)
        {
            if (currentHash is not null && token.TokenHash == currentHash)
                continue;

            token.RevokedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> GetSessionsAsync(
        ClaimsPrincipal user,
        HttpRequest httpRequest,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var now = DateTime.UtcNow;
        var currentHash = GetRefreshTokenHash(httpRequest);

        var sessions = await dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new SessionDto(
                t.Id,
                t.DeviceInfo,
                t.CreatedAt,
                t.LastUsedAt ?? t.CreatedAt,
                currentHash != null && t.TokenHash == currentHash))
            .ToListAsync();

        return Results.Ok(new SessionsResponse(sessions));
    }

    private static async Task<IResult> RevokeSessionAsync(
        ClaimsPrincipal user,
        Guid id,
        HttpRequest httpRequest,
        HttpResponse response,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var token = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.UserId == userId && t.Id == id);
        if (token is null)
            return Results.NotFound();

        if (token.RevokedAt is null)
            token.RevokedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        var currentHash = GetRefreshTokenHash(httpRequest);
        if (currentHash is not null && token.TokenHash == currentHash)
            response.Cookies.Delete("refreshToken");

        return Results.NoContent();
    }

    private static async Task<IResult> RevokeAllSessionsAsync(
        ClaimsPrincipal user,
        HttpRequest httpRequest,
        HttpResponse response,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var currentHash = GetRefreshTokenHash(httpRequest);
        var tokens = await dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync();

        foreach (var token in tokens)
        {
            if (currentHash is not null && token.TokenHash == currentHash)
                continue;

            token.RevokedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync();

        if (currentHash is null)
            response.Cookies.Delete("refreshToken");

        return Results.NoContent();
    }

    private static IResult GoogleLoginAsync()
    {
        return Results.Problem("Google OIDC is not configured.", statusCode: StatusCodes.Status501NotImplemented);
    }

    private static IResult GoogleCallbackAsync()
    {
        return Results.Problem("Google OIDC is not configured.", statusCode: StatusCodes.Status501NotImplemented);
    }

    private static IResult AppleLoginAsync()
    {
        return Results.Problem("Apple OIDC is not configured.", statusCode: StatusCodes.Status501NotImplemented);
    }

    private static IResult AppleCallbackAsync()
    {
        return Results.Problem("Apple OIDC is not configured.", statusCode: StatusCodes.Status501NotImplemented);
    }

    private static IResult LinkGoogleAsync()
    {
        return Results.Problem("Google linking is not configured.", statusCode: StatusCodes.Status501NotImplemented);
    }

    private static async Task<IResult> UnlinkGoogleAsync(
        ClaimsPrincipal user,
        UserManager<AppUser> userManager)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var appUser = await userManager.FindByIdAsync(userId.ToString());
        if (appUser is null)
            return Results.NotFound();

        if (string.IsNullOrWhiteSpace(appUser.GoogleId))
            return Results.NoContent();

        var hasPassword = await userManager.HasPasswordAsync(appUser);
        if (!hasPassword && string.IsNullOrWhiteSpace(appUser.AppleId))
            return Results.BadRequest(new { error = "no_alternative_login" });

        appUser.GoogleId = null;
        var result = await userManager.UpdateAsync(appUser);
        if (!result.Succeeded)
            return Results.BadRequest(ToApiError(result.Errors));

        return Results.NoContent();
    }

    private static IResult LinkAppleAsync()
    {
        return Results.Problem("Apple linking is not configured.", statusCode: StatusCodes.Status501NotImplemented);
    }

    private static async Task<IResult> UnlinkAppleAsync(
        ClaimsPrincipal user,
        UserManager<AppUser> userManager)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var appUser = await userManager.FindByIdAsync(userId.ToString());
        if (appUser is null)
            return Results.NotFound();

        if (string.IsNullOrWhiteSpace(appUser.AppleId))
            return Results.NoContent();

        var hasPassword = await userManager.HasPasswordAsync(appUser);
        if (!hasPassword && string.IsNullOrWhiteSpace(appUser.GoogleId))
            return Results.BadRequest(new { error = "no_alternative_login" });

        appUser.AppleId = null;
        var result = await userManager.UpdateAsync(appUser);
        if (!result.Succeeded)
            return Results.BadRequest(ToApiError(result.Errors));

        return Results.NoContent();
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

    private static string BuildConfirmEmailUrl(
        HttpRequest request,
        IConfiguration configuration,
        Guid userId,
        string token)
    {
        var baseUrl = GetApiBaseUrl(request, configuration);
        var encodedToken = Uri.EscapeDataString(token);
        return $"{baseUrl}/auth/confirm-email?userId={userId:D}&token={encodedToken}";
    }

    private static string BuildResetPasswordUrl(
        HttpRequest request,
        IConfiguration configuration,
        string token)
    {
        var baseUrl = configuration["App:FrontendBaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = GetApiBaseUrl(request, configuration);

        var encodedToken = Uri.EscapeDataString(token);
        return $"{baseUrl.TrimEnd('/')}/reset-password?token={encodedToken}";
    }

    private static string GetApiBaseUrl(HttpRequest request, IConfiguration configuration)
    {
        var configured = configuration["App:ApiBaseUrl"];
        if (!string.IsNullOrWhiteSpace(configured))
            return configured.TrimEnd('/');

        return $"{request.Scheme}://{request.Host}{request.PathBase}".TrimEnd('/');
    }

    private static string BuildDisplayName(string? firstName, string? lastName, string? email)
    {
        var name = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? email ?? string.Empty : name;
    }

    private static string? GetDeviceInfo(HttpRequest request)
    {
        var userAgent = request.Headers.UserAgent.ToString();
        return string.IsNullOrWhiteSpace(userAgent) ? null : userAgent;
    }

    private static string? GetRefreshTokenHash(HttpRequest request)
    {
        if (!request.Cookies.TryGetValue("refreshToken", out var refreshToken)
            || string.IsNullOrWhiteSpace(refreshToken))
            return null;

        return HashToken(refreshToken);
    }

    private static string? BuildAvatarUrl(AppUser user, IStorageService storageService)
    {
        if (string.IsNullOrWhiteSpace(user.AvatarStorageKey))
            return null;

        return storageService.GetPublicUrl(user.AvatarStorageKey);
    }

    private static readonly TimeSpan PasswordResetTokenLifetime = TimeSpan.FromHours(1);

    private static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        return Convert.ToBase64String(SHA256.HashData(bytes));
    }

    private static object ToApiError(IEnumerable<IdentityError> errors)
    {
        var errorList = errors.ToList();
        var details = new Dictionary<string, string[]>();

        foreach (var error in errorList)
        {
            // Map Identity error codes to field names
            var field = error.Code switch
            {
                "DuplicateEmail" or "DuplicateUserName" => "email",
                "InvalidEmail" => "email",
                "PasswordTooShort" or "PasswordRequiresDigit" or "PasswordRequiresLower"
                    or "PasswordRequiresUpper" or "PasswordRequiresNonAlphanumeric"
                    or "PasswordRequiresUniqueChars" => "password",
                "PasswordMismatch" => "currentPassword",
                _ => "general"
            };

            if (!details.TryGetValue(field, out var existing))
            {
                details[field] = [error.Description];
            }
            else
            {
                details[field] = [.. existing, error.Description];
            }
        }

        var firstError = errorList.FirstOrDefault();
        return new
        {
            code = firstError?.Code ?? "VALIDATION_ERROR",
            message = firstError?.Description ?? "Validation failed",
            details
        };
    }
}

