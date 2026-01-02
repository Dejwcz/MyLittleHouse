using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MujDomecek.Application.DTOs;
using MujDomecek.Domain.Aggregates.User;

namespace MujDomecek.Api.Tests;

public sealed class AdminEndpointTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AdminEndpointTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AdminEndpoints_AsNonAdmin_ReturnsForbidden()
    {
        var user = await RegisterLoginAsync();
        var client = CreateAuthenticatedClient(user.Token);

        var response = await client.GetAsync("/admin/stats/dashboard");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoints_ReturnDashboardHealthAndUsers()
    {
        var admin = await RegisterAdminAsync();
        var client = CreateAuthenticatedClient(admin.Token);

        var dashboard = await client.GetAsync("/admin/stats/dashboard");
        dashboard.EnsureSuccessStatusCode();
        var dashboardDto = await dashboard.Content.ReadFromJsonAsync<AdminDashboardResponse>();
        Assert.NotNull(dashboardDto);

        var health = await client.GetAsync("/admin/stats/health");
        health.EnsureSuccessStatusCode();
        var healthDto = await health.Content.ReadFromJsonAsync<HealthCheckResponse>();
        Assert.NotNull(healthDto);

        var usersResponse = await client.GetAsync("/admin/users");
        usersResponse.EnsureSuccessStatusCode();
        var users = await usersResponse.Content.ReadFromJsonAsync<AdminUserListResponse>();
        Assert.NotNull(users);
        Assert.True(users!.Total >= 1);
    }

    [Fact]
    public async Task AdminEndpoints_CanBlockAndUnblockUser()
    {
        var admin = await RegisterAdminAsync();
        var client = CreateAuthenticatedClient(admin.Token);

        var target = await RegisterLoginAsync();

        var blockResponse = await client.PostAsJsonAsync(
            $"/admin/users/{target.UserId}/block",
            new BlockUserRequest("test"));

        Assert.Equal(HttpStatusCode.NoContent, blockResponse.StatusCode);

        var blockedUser = await client.GetFromJsonAsync<AdminUserDto>($"/admin/users/{target.UserId}");
        Assert.NotNull(blockedUser);
        Assert.Equal("blocked", blockedUser!.Status);

        var unblockResponse = await client.PostAsync($"/admin/users/{target.UserId}/unblock", null);
        Assert.Equal(HttpStatusCode.NoContent, unblockResponse.StatusCode);

        var unblockedUser = await client.GetFromJsonAsync<AdminUserDto>($"/admin/users/{target.UserId}");
        Assert.NotNull(unblockedUser);
        Assert.Equal("active", unblockedUser!.Status);
    }

    [Fact]
    public async Task AdminEndpoints_CanManageTagsSettingsAndAudit()
    {
        var admin = await RegisterAdminAsync();
        var client = CreateAuthenticatedClient(admin.Token);

        var tagName = $"tag-{Guid.NewGuid():N}";
        var createTagResponse = await client.PostAsJsonAsync(
            "/admin/tags",
            new CreateTagRequest(tagName, "tag", null));

        Assert.Equal(HttpStatusCode.Created, createTagResponse.StatusCode);
        var createdTag = await createTagResponse.Content.ReadFromJsonAsync<TagDto>();
        Assert.NotNull(createdTag);

        var updateTagResponse = await client.PutAsJsonAsync(
            $"/admin/tags/{createdTag!.Id}",
            new UpdateTagRequest($"{tagName}-updated", "tag2", null));

        updateTagResponse.EnsureSuccessStatusCode();

        var deactivateResponse = await client.PostAsync($"/admin/tags/{createdTag.Id}/deactivate", null);
        Assert.Equal(HttpStatusCode.NoContent, deactivateResponse.StatusCode);

        var reorderResponse = await client.PostAsJsonAsync(
            "/admin/tags/reorder",
            new ReorderTagsRequest(new[] { new TagOrderItem(createdTag.Id, (short)5) }));

        Assert.Equal(HttpStatusCode.NoContent, reorderResponse.StatusCode);

        var settingsResponse = await client.GetAsync("/admin/settings");
        settingsResponse.EnsureSuccessStatusCode();
        var settings = await settingsResponse.Content.ReadFromJsonAsync<AdminSettingsResponse>();
        Assert.NotNull(settings);
        Assert.NotEmpty(settings!.Items);

        var updatePayload = new
        {
            items = new[]
            {
                new { key = "Constraints.Zaznam.TitleMaxLength", value = 123 }
            }
        };
        var updateSettingsResponse = await client.PutAsJsonAsync("/admin/settings", updatePayload);
        Assert.Equal(HttpStatusCode.NoContent, updateSettingsResponse.StatusCode);

        var updatedSettings = await client.GetFromJsonAsync<AdminSettingsResponse>("/admin/settings");
        Assert.NotNull(updatedSettings);
        var titleSetting = updatedSettings!.Items.Single(i => i.Key == "Constraints.Zaznam.TitleMaxLength");
        Assert.Equal(123, titleSetting.Value.GetInt32());
        Assert.Equal("db", titleSetting.Source);

        var auditResponse = await client.GetAsync("/admin/audit");
        auditResponse.EnsureSuccessStatusCode();
        var audit = await auditResponse.Content.ReadFromJsonAsync<AuditLogListResponse>();
        Assert.NotNull(audit);
    }

    private HttpClient CreateAuthenticatedClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<(string Token, Guid UserId, string Email)> RegisterAdminAsync()
    {
        var email = NewEmail();
        const string password = "Passw0rd!";

        var userId = await RegisterAsync(email, password);
        await ConfirmEmailAsync(email);
        await EnsureAdminRoleAsync(email);
        var token = await LoginAsync(email, password);

        return (token, userId, email);
    }

    private async Task EnsureAdminRoleAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            var created = await roleManager.CreateAsync(new IdentityRole<Guid>("Admin"));
            Assert.True(created.Succeeded);
        }

        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);

        if (!await userManager.IsInRoleAsync(user!, "Admin"))
        {
            var result = await userManager.AddToRoleAsync(user!, "Admin");
            Assert.True(result.Succeeded);
        }
    }

    private async Task<(string Token, Guid UserId, string Email)> RegisterLoginAsync()
    {
        var email = NewEmail();
        const string password = "Passw0rd!";

        var userId = await RegisterAsync(email, password);
        await ConfirmEmailAsync(email);
        var token = await LoginAsync(email, password);

        return (token, userId, email);
    }

    private async Task<Guid> RegisterAsync(string email, string password)
    {
        var payload = new RegisterRequest("Test", "User", email, password);
        var response = await _client.PostAsJsonAsync("/auth/register", payload);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.NotNull(result.UserId);
        return result.UserId!.Value;
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(payload);
        return payload!.AccessToken;
    }

    private async Task ConfirmEmailAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);

        user!.EmailConfirmed = true;
        var result = await userManager.UpdateAsync(user);
        Assert.True(result.Succeeded);
    }

    private static string NewEmail() => $"user-{Guid.NewGuid():N}@example.com";
}
