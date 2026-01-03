using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MujDomecek.Application.DTOs;
using MujDomecek.Domain.Aggregates.Zaznam;
using MujDomecek.Domain.Aggregates.User;
using MujDomecek.Domain.ValueObjects;
using MujDomecek.Infrastructure.Persistence;

namespace MujDomecek.Api.Tests;

public sealed class PropertyCrudTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PropertyCrudTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateProperty_ListsAndGetsDetail()
    {
        var token = await LoginConfirmedUserAsync();
        var authClient = CreateAuthenticatedClient(token);
        var projectId = await CreateProjectAsync(authClient);

        var createResponse = await authClient.PostAsJsonAsync(
            "/properties",
            new CreatePropertyRequest(projectId, "Main House", "Primary property", "house", 50.1m, 14.5m, 150));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<PropertyDto>();
        Assert.NotNull(created);

        var listResponse = await authClient.GetAsync($"/properties?projectId={projectId}");
        listResponse.EnsureSuccessStatusCode();
        var list = await listResponse.Content.ReadFromJsonAsync<PropertyListResponse>();
        Assert.NotNull(list);
        Assert.Contains(list!.Items, item => item.Id == created!.Id);

        var detailResponse = await authClient.GetAsync($"/properties/{created!.Id}");
        detailResponse.EnsureSuccessStatusCode();
        var detail = await detailResponse.Content.ReadFromJsonAsync<PropertyDto>();
        Assert.NotNull(detail);
        Assert.Equal(created.Id, detail!.Id);
        Assert.Equal("Main House", detail.Name);
        Assert.Equal("house", detail.PropertyType);
    }

    [Fact]
    public async Task UpdateProperty_AsOwner_UpdatesFields()
    {
        var token = await LoginConfirmedUserAsync();
        var authClient = CreateAuthenticatedClient(token);
        var projectId = await CreateProjectAsync(authClient);

        var createResponse = await authClient.PostAsJsonAsync(
            "/properties",
            new CreatePropertyRequest(projectId, "Old Name", null, "other", null, null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<PropertyDto>();
        Assert.NotNull(created);

        var updateResponse = await authClient.PutAsJsonAsync(
            $"/properties/{created!.Id}",
            new UpdatePropertyRequest("New Name", "Updated", null, 49.0m, 15.0m, 200));

        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<PropertyDto>();
        Assert.NotNull(updated);
        Assert.Equal("New Name", updated!.Name);
        Assert.Equal("Updated", updated.Description);
        Assert.Equal(200, updated.GeoRadius);
    }

    [Fact]
    public async Task UpdatePropertyCover_AsOwner_UpdatesCover()
    {
        var token = await LoginConfirmedUserAsync();
        var authClient = CreateAuthenticatedClient(token);
        var projectId = await CreateProjectAsync(authClient);

        var createResponse = await authClient.PostAsJsonAsync(
            "/properties",
            new CreatePropertyRequest(projectId, "Cover Home", null, "other", null, null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<PropertyDto>();
        Assert.NotNull(created);

        var mediaId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Media.Add(new Media
            {
                Id = mediaId,
                OwnerType = OwnerType.Property,
                OwnerId = created!.Id,
                Type = MediaType.Photo,
                StorageKey = "media/property-cover.jpg",
                MimeType = "image/jpeg",
                SizeBytes = 1234
            });
            await dbContext.SaveChangesAsync();
        }

        var patchResponse = await authClient.SendAsync(new HttpRequestMessage(
            HttpMethod.Patch,
            $"/properties/{created!.Id}/cover")
        {
            Content = JsonContent.Create(new CoverMediaRequest(mediaId))
        });

        patchResponse.EnsureSuccessStatusCode();
        var updated = await patchResponse.Content.ReadFromJsonAsync<PropertyDto>();
        Assert.NotNull(updated);
        Assert.Equal(mediaId, updated!.CoverMediaId);
        Assert.False(string.IsNullOrWhiteSpace(updated.CoverUrl));
    }

    [Fact]
    public async Task DeleteProperty_AsOwner_RemovesProperty()
    {
        var token = await LoginConfirmedUserAsync();
        var authClient = CreateAuthenticatedClient(token);
        var projectId = await CreateProjectAsync(authClient);

        var createResponse = await authClient.PostAsJsonAsync(
            "/properties",
            new CreatePropertyRequest(projectId, "To Delete", null, "other", null, null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<PropertyDto>();
        Assert.NotNull(created);

        var deleteResponse = await authClient.DeleteAsync($"/properties/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await authClient.GetAsync($"/properties/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task CreateProperty_AsNonMember_ReturnsForbidden()
    {
        var ownerToken = await LoginConfirmedUserAsync();
        var ownerClient = CreateAuthenticatedClient(ownerToken);
        var projectId = await CreateProjectAsync(ownerClient);

        var otherToken = await LoginConfirmedUserAsync();
        var otherClient = CreateAuthenticatedClient(otherToken);

        var response = await otherClient.PostAsJsonAsync(
            "/properties",
            new CreatePropertyRequest(projectId, "Blocked", null, "other", null, null, null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private HttpClient CreateAuthenticatedClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<Guid> CreateProjectAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/projects", new CreateProjectRequest("Alpha", "Project"));
        response.EnsureSuccessStatusCode();
        var project = await response.Content.ReadFromJsonAsync<ProjectDto>();
        Assert.NotNull(project);
        return project!.Id;
    }

    private async Task<string> LoginConfirmedUserAsync()
    {
        var email = NewEmail();
        const string password = "Passw0rd!";

        await RegisterAsync(email, password);
        await ConfirmEmailAsync(email);

        var loginResponse = await _client.PostAsJsonAsync("/auth/login", new LoginRequest(email, password));
        loginResponse.EnsureSuccessStatusCode();
        var payload = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(payload);
        return payload!.AccessToken;
    }

    private async Task RegisterAsync(string email, string password)
    {
        var payload = new RegisterRequest("Test", "User", email, password);
        var response = await _client.PostAsJsonAsync("/auth/register", payload);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.NotNull(result.UserId);
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
