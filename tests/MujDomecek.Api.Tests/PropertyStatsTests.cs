using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MujDomecek.Application.DTOs;
using MujDomecek.Domain.Aggregates.User;

namespace MujDomecek.Api.Tests;

public sealed class PropertyStatsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PropertyStatsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetStats_ReturnsAggregates()
    {
        var owner = await RegisterLoginAsync();
        var ownerClient = CreateAuthenticatedClient(owner.Token);
        var projectId = await CreateProjectAsync(ownerClient);
        var propertyId = await CreatePropertyAsync(ownerClient, projectId);

        await CreateZaznamAsync(ownerClient, propertyId, "Draft", "draft", 100);
        await CreateZaznamAsync(ownerClient, propertyId, "Complete", "complete", 250);

        var response = await ownerClient.GetAsync($"/properties/{propertyId}/stats");
        response.EnsureSuccessStatusCode();

        var stats = await response.Content.ReadFromJsonAsync<PropertyStatsResponse>();
        Assert.NotNull(stats);
        Assert.Equal(2, stats!.ZaznamCount);
        Assert.Equal(1, stats.DraftCount);
        Assert.Equal(350, stats.TotalCost);
    }

    [Fact]
    public async Task GetStats_AsNonMember_ReturnsForbidden()
    {
        var owner = await RegisterLoginAsync();
        var ownerClient = CreateAuthenticatedClient(owner.Token);
        var projectId = await CreateProjectAsync(ownerClient);
        var propertyId = await CreatePropertyAsync(ownerClient, projectId);

        var other = await RegisterLoginAsync();
        var otherClient = CreateAuthenticatedClient(other.Token);

        var response = await otherClient.GetAsync($"/properties/{propertyId}/stats");
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
            new CreatePropertyRequest(projectId, "Property", null, "other", null, null, null));
        response.EnsureSuccessStatusCode();
        var property = await response.Content.ReadFromJsonAsync<PropertyDto>();
        Assert.NotNull(property);
        return property!.Id;
    }

    private async Task<ZaznamDto> CreateZaznamAsync(HttpClient client, Guid propertyId, string title, string status, int cost)
    {
        var response = await client.PostAsJsonAsync(
            "/zaznamy",
            new CreateZaznamRequest(propertyId, null, title, null, null, cost, status, null, null));
        response.EnsureSuccessStatusCode();
        var zaznam = await response.Content.ReadFromJsonAsync<ZaznamDto>();
        Assert.NotNull(zaznam);
        return zaznam!;
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
