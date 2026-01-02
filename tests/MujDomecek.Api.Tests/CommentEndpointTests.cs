using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MujDomecek.Application.DTOs;
using MujDomecek.Domain.Aggregates.User;

namespace MujDomecek.Api.Tests;

public sealed class CommentEndpointTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CommentEndpointTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateComment_ListsAndGetsComment()
    {
        var owner = await RegisterLoginAsync();
        var ownerClient = CreateAuthenticatedClient(owner.Token);
        var projectId = await CreateProjectAsync(ownerClient);
        var propertyId = await CreatePropertyAsync(ownerClient, projectId);
        var zaznam = await CreateZaznamAsync(ownerClient, propertyId);

        var createResponse = await ownerClient.PostAsJsonAsync(
            $"/zaznamy/{zaznam.Id}/comments",
            new CreateCommentRequest("First"));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CommentDto>();
        Assert.NotNull(created);

        var listResponse = await ownerClient.GetAsync($"/zaznamy/{zaznam.Id}/comments");
        listResponse.EnsureSuccessStatusCode();
        var list = await listResponse.Content.ReadFromJsonAsync<List<CommentDto>>();
        Assert.NotNull(list);
        Assert.Contains(list!, c => c.Id == created!.Id);
    }

    [Fact]
    public async Task UpdateComment_UpdatesContent()
    {
        var owner = await RegisterLoginAsync();
        var ownerClient = CreateAuthenticatedClient(owner.Token);
        var projectId = await CreateProjectAsync(ownerClient);
        var propertyId = await CreatePropertyAsync(ownerClient, projectId);
        var zaznam = await CreateZaznamAsync(ownerClient, propertyId);
        var comment = await CreateCommentAsync(ownerClient, zaznam.Id, "Original");

        var updateResponse = await ownerClient.PutAsJsonAsync(
            $"/comments/{comment.Id}",
            new UpdateCommentRequest("Updated"));

        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var listResponse = await ownerClient.GetAsync($"/zaznamy/{zaznam.Id}/comments");
        listResponse.EnsureSuccessStatusCode();
        var list = await listResponse.Content.ReadFromJsonAsync<List<CommentDto>>();
        Assert.NotNull(list);
        var updated = list!.Single(c => c.Id == comment.Id);
        Assert.Equal("Updated", updated.Content);
        Assert.True(updated.IsEdited);
    }

    [Fact]
    public async Task DeleteComment_RemovesComment()
    {
        var owner = await RegisterLoginAsync();
        var ownerClient = CreateAuthenticatedClient(owner.Token);
        var projectId = await CreateProjectAsync(ownerClient);
        var propertyId = await CreatePropertyAsync(ownerClient, projectId);
        var zaznam = await CreateZaznamAsync(ownerClient, propertyId);
        var comment = await CreateCommentAsync(ownerClient, zaznam.Id, "Delete me");

        var deleteResponse = await ownerClient.DeleteAsync($"/comments/{comment.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var listResponse = await ownerClient.GetAsync($"/zaznamy/{zaznam.Id}/comments");
        listResponse.EnsureSuccessStatusCode();
        var list = await listResponse.Content.ReadFromJsonAsync<List<CommentDto>>();
        Assert.NotNull(list);
        Assert.DoesNotContain(list!, c => c.Id == comment.Id);
    }

    [Fact]
    public async Task UpdateComment_AsDifferentUser_ReturnsForbidden()
    {
        var owner = await RegisterLoginAsync();
        var ownerClient = CreateAuthenticatedClient(owner.Token);
        var projectId = await CreateProjectAsync(ownerClient);
        var propertyId = await CreatePropertyAsync(ownerClient, projectId);
        var zaznam = await CreateZaznamAsync(ownerClient, propertyId);
        var comment = await CreateCommentAsync(ownerClient, zaznam.Id, "Owned");

        var other = await RegisterLoginAsync();
        var otherClient = CreateAuthenticatedClient(other.Token);

        var updateResponse = await otherClient.PutAsJsonAsync(
            $"/comments/{comment.Id}",
            new UpdateCommentRequest("Nope"));

        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
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

    private async Task<ZaznamDto> CreateZaznamAsync(HttpClient client, Guid propertyId)
    {
        var response = await client.PostAsJsonAsync(
            "/zaznamy",
            new CreateZaznamRequest(propertyId, null, "Repair", "Fixing", null, 100, "complete", null, null));
        response.EnsureSuccessStatusCode();
        var zaznam = await response.Content.ReadFromJsonAsync<ZaznamDto>();
        Assert.NotNull(zaznam);
        return zaznam!;
    }

    private async Task<CommentDto> CreateCommentAsync(HttpClient client, Guid zaznamId, string content)
    {
        var response = await client.PostAsJsonAsync(
            $"/zaznamy/{zaznamId}/comments",
            new CreateCommentRequest(content));
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
