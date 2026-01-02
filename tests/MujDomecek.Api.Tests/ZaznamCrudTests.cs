using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MujDomecek.Application.DTOs;
using MujDomecek.Domain.Aggregates.User;

namespace MujDomecek.Api.Tests;

public sealed class ZaznamCrudTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ZaznamCrudTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateZaznam_ListsAndGetsDetail()
    {
        var token = await LoginConfirmedUserAsync();
        var authClient = CreateAuthenticatedClient(token);
        var projectId = await CreateProjectAsync(authClient);
        var propertyId = await CreatePropertyAsync(authClient, projectId);

        var createResponse = await authClient.PostAsJsonAsync(
            "/zaznamy",
            new CreateZaznamRequest(propertyId, null, "Repair", "Fixing", null, 100, "complete", null, null));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<ZaznamDto>();
        Assert.NotNull(created);

        var listResponse = await authClient.GetAsync($"/zaznamy?propertyId={propertyId}");
        listResponse.EnsureSuccessStatusCode();
        var list = await listResponse.Content.ReadFromJsonAsync<ZaznamListResponse>();
        Assert.NotNull(list);
        Assert.Contains(list!.Items, item => item.Id == created!.Id);

        var detailResponse = await authClient.GetAsync($"/zaznamy/{created!.Id}");
        detailResponse.EnsureSuccessStatusCode();
        var detail = await detailResponse.Content.ReadFromJsonAsync<ZaznamDetailDto>();
        Assert.NotNull(detail);
        Assert.Equal(created.Id, detail!.Id);
        Assert.Equal("Repair", detail.Title);
    }

    [Fact]
    public async Task UpdateZaznam_UpdatesFields()
    {
        var token = await LoginConfirmedUserAsync();
        var authClient = CreateAuthenticatedClient(token);
        var projectId = await CreateProjectAsync(authClient);
        var propertyId = await CreatePropertyAsync(authClient, projectId);
        var unitId = await CreateUnitAsync(authClient, propertyId);

        var createResponse = await authClient.PostAsJsonAsync(
            "/zaznamy",
            new CreateZaznamRequest(propertyId, null, "Initial", null, null, null, "draft", null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<ZaznamDto>();
        Assert.NotNull(created);

        var updateResponse = await authClient.PutAsJsonAsync(
            $"/zaznamy/{created!.Id}",
            new UpdateZaznamRequest(unitId, "Updated", "Updated desc", null, 250, new[] { "important" }, null));

        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<ZaznamDto>();
        Assert.NotNull(updated);
        Assert.Equal("Updated", updated!.Title);
        Assert.Equal("Updated desc", updated.Description);
        Assert.Equal(250, updated.Cost);
        Assert.Equal(unitId, updated.UnitId);
        Assert.Contains(updated.Flags, flag => flag == "important");
    }

    [Fact]
    public async Task DeleteZaznam_RemovesZaznam()
    {
        var token = await LoginConfirmedUserAsync();
        var authClient = CreateAuthenticatedClient(token);
        var projectId = await CreateProjectAsync(authClient);
        var propertyId = await CreatePropertyAsync(authClient, projectId);

        var createResponse = await authClient.PostAsJsonAsync(
            "/zaznamy",
            new CreateZaznamRequest(propertyId, null, "To Delete", null, null, null, "complete", null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<ZaznamDto>();
        Assert.NotNull(created);

        var deleteResponse = await authClient.DeleteAsync($"/zaznamy/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await authClient.GetAsync($"/zaznamy/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task CompleteZaznam_WithoutTitle_ReturnsBadRequest()
    {
        var token = await LoginConfirmedUserAsync();
        var authClient = CreateAuthenticatedClient(token);
        var projectId = await CreateProjectAsync(authClient);
        var propertyId = await CreatePropertyAsync(authClient, projectId);

        var createResponse = await authClient.PostAsJsonAsync(
            "/zaznamy",
            new CreateZaznamRequest(propertyId, null, null, null, null, null, "draft", null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<ZaznamDto>();
        Assert.NotNull(created);

        var completeResponse = await authClient.PostAsync($"/zaznamy/{created!.Id}/complete", null);

        Assert.Equal(HttpStatusCode.BadRequest, completeResponse.StatusCode);
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

    private async Task<Guid> CreatePropertyAsync(HttpClient client, Guid projectId)
    {
        var response = await client.PostAsJsonAsync(
            "/properties",
            new CreatePropertyRequest(projectId, "Property", null, null, null, null));
        response.EnsureSuccessStatusCode();
        var property = await response.Content.ReadFromJsonAsync<PropertyDto>();
        Assert.NotNull(property);
        return property!.Id;
    }

    private async Task<Guid> CreateUnitAsync(HttpClient client, Guid propertyId)
    {
        var response = await client.PostAsJsonAsync(
            "/units",
            new CreateUnitRequest(propertyId, null, "Unit", null, "room"));
        response.EnsureSuccessStatusCode();
        var unit = await response.Content.ReadFromJsonAsync<UnitDto>();
        Assert.NotNull(unit);
        return unit!.Id;
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
