using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MujDomecek.Application.DTOs;
using MujDomecek.Domain.Aggregates.User;

namespace MujDomecek.Api.Tests;

public sealed class PropertyActivityTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PropertyActivityTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetActivity_ReturnsItemsWithTargets()
    {
        var owner = await RegisterLoginAsync();
        var ownerClient = CreateAuthenticatedClient(owner.Token);
        var projectId = await CreateProjectAsync(ownerClient);
        var propertyId = await CreatePropertyAsync(ownerClient, projectId);
        var zaznam = await CreateZaznamAsync(ownerClient, propertyId);
        var comment = await CreateCommentAsync(ownerClient, zaznam.Id);

        var response = await ownerClient.GetAsync($"/properties/{propertyId}/activity");
        response.EnsureSuccessStatusCode();

        var activity = await response.Content.ReadFromJsonAsync<ActivityListResponse>();
        Assert.NotNull(activity);
        Assert.Equal(2, activity!.Total);
        Assert.Equal(2, activity.Items.Count);

        var zaznamActivity = activity.Items.FirstOrDefault(item => item.Type == "zaznam_created");
        Assert.NotNull(zaznamActivity);
        Assert.Equal("zaznam", zaznamActivity!.TargetType);
        Assert.Equal(zaznam.Id, zaznamActivity.TargetId);
        Assert.Equal(owner.UserId, zaznamActivity.Actor.Id);

        var commentActivity = activity.Items.FirstOrDefault(item => item.Type == "comment_added");
        Assert.NotNull(commentActivity);
        Assert.Equal("comment", commentActivity!.TargetType);
        Assert.Equal(comment.Id, commentActivity.TargetId);
        Assert.Equal(owner.UserId, commentActivity.Actor.Id);
        Assert.NotNull(commentActivity.Metadata);
        Assert.True(commentActivity.Metadata!.TryGetValue("zaznamId", out var metadataValue));
        var jsonValue = Assert.IsType<JsonElement>(metadataValue);
        Assert.Equal(zaznam.Id, jsonValue.GetGuid());
    }

    [Fact]
    public async Task GetActivity_PageSizeLimitsItems()
    {
        var owner = await RegisterLoginAsync();
        var ownerClient = CreateAuthenticatedClient(owner.Token);
        var projectId = await CreateProjectAsync(ownerClient);
        var propertyId = await CreatePropertyAsync(ownerClient, projectId);
        var zaznam = await CreateZaznamAsync(ownerClient, propertyId);
        await CreateCommentAsync(ownerClient, zaznam.Id);

        var response = await ownerClient.GetAsync($"/properties/{propertyId}/activity?pageSize=1");
        response.EnsureSuccessStatusCode();

        var activity = await response.Content.ReadFromJsonAsync<ActivityListResponse>();
        Assert.NotNull(activity);
        Assert.Equal(2, activity!.Total);
        Assert.Single(activity.Items);
    }

    [Fact]
    public async Task GetActivity_AsNonMember_ReturnsForbidden()
    {
        var owner = await RegisterLoginAsync();
        var ownerClient = CreateAuthenticatedClient(owner.Token);
        var projectId = await CreateProjectAsync(ownerClient);
        var propertyId = await CreatePropertyAsync(ownerClient, projectId);

        var other = await RegisterLoginAsync();
        var otherClient = CreateAuthenticatedClient(other.Token);

        var response = await otherClient.GetAsync($"/properties/{propertyId}/activity");
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

    private async Task<ZaznamDto> CreateZaznamAsync(HttpClient client, Guid propertyId)
    {
        var response = await client.PostAsJsonAsync(
            "/zaznamy",
            new CreateZaznamRequest(propertyId, null, "Repair", null, null, 150, "complete", null, null));
        response.EnsureSuccessStatusCode();
        var zaznam = await response.Content.ReadFromJsonAsync<ZaznamDto>();
        Assert.NotNull(zaznam);
        return zaznam!;
    }

    private async Task<CommentDto> CreateCommentAsync(HttpClient client, Guid zaznamId)
    {
        var response = await client.PostAsJsonAsync(
            $"/zaznamy/{zaznamId}/comments",
            new CreateCommentRequest("Hello"));
        response.EnsureSuccessStatusCode();
        var comment = await response.Content.ReadFromJsonAsync<CommentDto>();
        Assert.NotNull(comment);
        return comment!;
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
