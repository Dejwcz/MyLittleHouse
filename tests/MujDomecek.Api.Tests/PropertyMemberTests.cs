using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MujDomecek.Application.DTOs;
using MujDomecek.Domain.Aggregates.User;

namespace MujDomecek.Api.Tests;

public sealed class PropertyMemberTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PropertyMemberTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task InviteMember_Accept_AddsMember()
    {
        var owner = await RegisterLoginAsync();
        var ownerClient = CreateAuthenticatedClient(owner.Token);
        var projectId = await CreateProjectAsync(ownerClient);
        var propertyId = await CreatePropertyAsync(ownerClient, projectId);

        var recipient = await RegisterLoginAsync();
        var invite = await InvitePropertyMemberAsync(ownerClient, propertyId, recipient.Email);

        var recipientClient = CreateAuthenticatedClient(recipient.Token);
        var acceptResponse = await recipientClient.PostAsync($"/invitations/{invite.InvitationId}/accept", null);
        Assert.Equal(HttpStatusCode.NoContent, acceptResponse.StatusCode);

        var members = await GetPropertyMembersAsync(ownerClient, propertyId);
        Assert.Contains(members, m => m.UserId == recipient.UserId && m.Role == "viewer");
    }

    [Fact]
    public async Task LeaveProperty_RemovesMember()
    {
        var owner = await RegisterLoginAsync();
        var ownerClient = CreateAuthenticatedClient(owner.Token);
        var projectId = await CreateProjectAsync(ownerClient);
        var propertyId = await CreatePropertyAsync(ownerClient, projectId);

        var recipient = await RegisterLoginAsync();
        var invite = await InvitePropertyMemberAsync(ownerClient, propertyId, recipient.Email);
        var recipientClient = CreateAuthenticatedClient(recipient.Token);

        var acceptResponse = await recipientClient.PostAsync($"/invitations/{invite.InvitationId}/accept", null);
        Assert.Equal(HttpStatusCode.NoContent, acceptResponse.StatusCode);

        var leaveResponse = await recipientClient.PostAsync($"/properties/{propertyId}/leave", null);
        Assert.Equal(HttpStatusCode.NoContent, leaveResponse.StatusCode);

        var members = await GetPropertyMembersAsync(ownerClient, propertyId);
        Assert.DoesNotContain(members, m => m.UserId == recipient.UserId);
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

    private async Task<InvitationLinkResponse> InvitePropertyMemberAsync(HttpClient client, Guid propertyId, string email)
    {
        var response = await client.PostAsJsonAsync(
            $"/properties/{propertyId}/members",
            new AddMemberRequest(email, "viewer", null));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var invite = await response.Content.ReadFromJsonAsync<InvitationLinkResponse>();
        Assert.NotNull(invite);
        return invite!;
    }

    private async Task<List<MemberDto>> GetPropertyMembersAsync(HttpClient client, Guid propertyId)
    {
        var response = await client.GetAsync($"/properties/{propertyId}/members");
        response.EnsureSuccessStatusCode();
        var members = await response.Content.ReadFromJsonAsync<List<MemberDto>>();
        Assert.NotNull(members);
        return members!;
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
