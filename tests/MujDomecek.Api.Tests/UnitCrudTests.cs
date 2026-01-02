using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MujDomecek.Application.DTOs;
using MujDomecek.Domain.Aggregates.User;

namespace MujDomecek.Api.Tests;

public sealed class UnitCrudTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UnitCrudTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateUnit_ListsAndGetsDetail()
    {
        var token = await LoginConfirmedUserAsync();
        var authClient = CreateAuthenticatedClient(token);
        var projectId = await CreateProjectAsync(authClient);
        var propertyId = await CreatePropertyAsync(authClient, projectId);

        var createResponse = await authClient.PostAsJsonAsync(
            "/units",
            new CreateUnitRequest(propertyId, null, "Main Room", null, "room"));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<UnitDto>();
        Assert.NotNull(created);

        var listResponse = await authClient.GetAsync($"/units?propertyId={propertyId}");
        listResponse.EnsureSuccessStatusCode();
        var list = await listResponse.Content.ReadFromJsonAsync<List<UnitDto>>();
        Assert.NotNull(list);
        Assert.Contains(list!, item => item.Id == created!.Id);

        var detailResponse = await authClient.GetAsync($"/units/{created!.Id}");
        detailResponse.EnsureSuccessStatusCode();
        var detail = await detailResponse.Content.ReadFromJsonAsync<UnitDto>();
        Assert.NotNull(detail);
        Assert.Equal("Main Room", detail!.Name);
        Assert.Equal(propertyId, detail.PropertyId);
    }

    [Fact]
    public async Task UpdateUnit_WithParent_UpdatesFields()
    {
        var token = await LoginConfirmedUserAsync();
        var authClient = CreateAuthenticatedClient(token);
        var projectId = await CreateProjectAsync(authClient);
        var propertyId = await CreatePropertyAsync(authClient, projectId);

        var parentResponse = await authClient.PostAsJsonAsync(
            "/units",
            new CreateUnitRequest(propertyId, null, "Parent", null, "house"));
        var parent = await parentResponse.Content.ReadFromJsonAsync<UnitDto>();
        Assert.NotNull(parent);

        var childResponse = await authClient.PostAsJsonAsync(
            "/units",
            new CreateUnitRequest(propertyId, null, "Child", null, "room"));
        var child = await childResponse.Content.ReadFromJsonAsync<UnitDto>();
        Assert.NotNull(child);

        var updateResponse = await authClient.PutAsJsonAsync(
            $"/units/{child!.Id}",
            new UpdateUnitRequest("Garage", "Updated", "garage", parent!.Id));

        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<UnitDto>();
        Assert.NotNull(updated);
        Assert.Equal("Garage", updated!.Name);
        Assert.Equal("garage", updated.UnitType);
        Assert.Equal(parent.Id, updated.ParentUnitId);
    }

    [Fact]
    public async Task DeleteUnit_RemovesUnit()
    {
        var token = await LoginConfirmedUserAsync();
        var authClient = CreateAuthenticatedClient(token);
        var projectId = await CreateProjectAsync(authClient);
        var propertyId = await CreatePropertyAsync(authClient, projectId);

        var createResponse = await authClient.PostAsJsonAsync(
            "/units",
            new CreateUnitRequest(propertyId, null, "To Delete", null, "other"));
        var created = await createResponse.Content.ReadFromJsonAsync<UnitDto>();
        Assert.NotNull(created);

        var deleteResponse = await authClient.DeleteAsync($"/units/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await authClient.GetAsync($"/units/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task CreateUnit_AsNonMember_ReturnsForbidden()
    {
        var ownerToken = await LoginConfirmedUserAsync();
        var ownerClient = CreateAuthenticatedClient(ownerToken);
        var projectId = await CreateProjectAsync(ownerClient);
        var propertyId = await CreatePropertyAsync(ownerClient, projectId);

        var otherToken = await LoginConfirmedUserAsync();
        var otherClient = CreateAuthenticatedClient(otherToken);

        var response = await otherClient.PostAsJsonAsync(
            "/units",
            new CreateUnitRequest(propertyId, null, "Blocked", null, "room"));

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
