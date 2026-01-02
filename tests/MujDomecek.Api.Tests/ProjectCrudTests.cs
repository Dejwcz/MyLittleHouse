using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MujDomecek.Application.DTOs;
using MujDomecek.Domain.Aggregates.User;

namespace MujDomecek.Api.Tests;

public sealed class ProjectCrudTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProjectCrudTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateProject_ListsAndGetsDetail()
    {
        var token = await LoginConfirmedUserAsync();
        var authClient = CreateAuthenticatedClient(token);

        var createResponse = await authClient.PostAsJsonAsync("/projects", new CreateProjectRequest("Alpha", "First project"));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<ProjectDto>();
        Assert.NotNull(created);

        var listResponse = await authClient.GetAsync("/projects");
        listResponse.EnsureSuccessStatusCode();
        var list = await listResponse.Content.ReadFromJsonAsync<ProjectListResponse>();
        Assert.NotNull(list);
        Assert.Contains(list!.Items, item => item.Id == created!.Id);

        var detailResponse = await authClient.GetAsync($"/projects/{created!.Id}");
        detailResponse.EnsureSuccessStatusCode();
        var detail = await detailResponse.Content.ReadFromJsonAsync<ProjectDetailDto>();
        Assert.NotNull(detail);
        Assert.Equal(created.Id, detail!.Id);
        Assert.Equal(created.Name, detail.Name);
    }

    [Fact]
    public async Task UpdateProject_AsOwner_UpdatesFields()
    {
        var token = await LoginConfirmedUserAsync();
        var authClient = CreateAuthenticatedClient(token);

        var createResponse = await authClient.PostAsJsonAsync("/projects", new CreateProjectRequest("Alpha", "First project"));
        var created = await createResponse.Content.ReadFromJsonAsync<ProjectDto>();
        Assert.NotNull(created);

        var updateResponse = await authClient.PutAsJsonAsync(
            $"/projects/{created!.Id}",
            new UpdateProjectRequest("Beta", "Updated project"));

        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<ProjectDto>();
        Assert.NotNull(updated);
        Assert.Equal("Beta", updated!.Name);
        Assert.Equal("Updated project", updated.Description);
    }

    [Fact]
    public async Task UpdateProject_AsDifferentUser_ReturnsForbidden()
    {
        var ownerToken = await LoginConfirmedUserAsync();
        var ownerClient = CreateAuthenticatedClient(ownerToken);

        var createResponse = await ownerClient.PostAsJsonAsync("/projects", new CreateProjectRequest("Alpha", "First project"));
        var created = await createResponse.Content.ReadFromJsonAsync<ProjectDto>();
        Assert.NotNull(created);

        var otherToken = await LoginConfirmedUserAsync();
        var otherClient = CreateAuthenticatedClient(otherToken);

        var updateResponse = await otherClient.PutAsJsonAsync(
            $"/projects/{created!.Id}",
            new UpdateProjectRequest("Nope", null));

        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteProject_AsOwner_RemovesProject()
    {
        var token = await LoginConfirmedUserAsync();
        var authClient = CreateAuthenticatedClient(token);

        var createResponse = await authClient.PostAsJsonAsync("/projects", new CreateProjectRequest("Alpha", "First project"));
        var created = await createResponse.Content.ReadFromJsonAsync<ProjectDto>();
        Assert.NotNull(created);

        var deleteResponse = await authClient.DeleteAsync($"/projects/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await authClient.GetAsync($"/projects/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private HttpClient CreateAuthenticatedClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
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
