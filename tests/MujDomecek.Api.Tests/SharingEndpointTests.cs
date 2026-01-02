using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MujDomecek.Application.DTOs;
using MujDomecek.Domain.Aggregates.User;

namespace MujDomecek.Api.Tests;

public sealed class SharingEndpointTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SharingEndpointTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task MyShares_IncludesProjectWithPendingInvite()
    {
        var owner = await RegisterLoginAsync();
        var ownerClient = CreateAuthenticatedClient(owner.Token);
        var projectId = await CreateProjectAsync(ownerClient);

        var recipient = await RegisterLoginAsync();
        await InviteProjectMemberAsync(ownerClient, projectId, recipient.Email);

        var response = await ownerClient.GetAsync("/sharing/my-shares");
        response.EnsureSuccessStatusCode();
        var shares = await response.Content.ReadFromJsonAsync<MySharesResponse>();
        Assert.NotNull(shares);
        Assert.Contains(shares!.Items, item => item.Type == "project" && item.Id == projectId);
    }

    [Fact]
    public async Task SharedWithMe_IncludesAcceptedProject()
    {
        var owner = await RegisterLoginAsync();
        var ownerClient = CreateAuthenticatedClient(owner.Token);
        var projectId = await CreateProjectAsync(ownerClient);

        var recipient = await RegisterLoginAsync();
        var invite = await InviteProjectMemberAsync(ownerClient, projectId, recipient.Email);
        var recipientClient = CreateAuthenticatedClient(recipient.Token);

        var acceptResponse = await recipientClient.PostAsync($"/invitations/{invite.InvitationId}/accept", null);
        Assert.Equal(HttpStatusCode.NoContent, acceptResponse.StatusCode);

        var response = await recipientClient.GetAsync("/sharing/shared-with-me");
        response.EnsureSuccessStatusCode();
        var shares = await response.Content.ReadFromJsonAsync<SharedWithMeResponse>();
        Assert.NotNull(shares);
        Assert.Contains(shares!.Items, item => item.Type == "project" && item.Id == projectId);
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

    private async Task<InvitationLinkResponse> InviteProjectMemberAsync(HttpClient client, Guid projectId, string email)
    {
        var response = await client.PostAsJsonAsync(
            $"/projects/{projectId}/members",
            new AddMemberRequest(email, "editor", null));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var invite = await response.Content.ReadFromJsonAsync<InvitationLinkResponse>();
        Assert.NotNull(invite);
        return invite!;
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
