using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MujDomecek.Application.DTOs;
using MujDomecek.Domain.Aggregates.User;

namespace MujDomecek.Api.Tests;

public sealed class ContactGroupTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ContactGroupTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateGroup_WithContacts_UpdatesAndGetsGroup()
    {
        var owner = await RegisterLoginAsync();
        var ownerClient = CreateAuthenticatedClient(owner.Token);

        var contactA = await CreateContactAsync(ownerClient, NewEmail(), "Alice");
        var contactB = await CreateContactAsync(ownerClient, NewEmail(), "Bob");

        var createResponse = await ownerClient.PostAsJsonAsync(
            "/contact-groups",
            new CreateContactGroupRequest("Friends", new[] { contactA.Id, contactB.Id }));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<ContactGroupDto>();
        Assert.NotNull(created);
        Assert.Equal(2, created!.MemberCount);
        Assert.Contains(created.Members, c => c.Id == contactA.Id);
        Assert.Contains(created.Members, c => c.Id == contactB.Id);

        var getResponse = await ownerClient.GetAsync($"/contact-groups/{created.Id}");
        getResponse.EnsureSuccessStatusCode();
        var detail = await getResponse.Content.ReadFromJsonAsync<ContactGroupDto>();
        Assert.NotNull(detail);
        Assert.Equal("Friends", detail!.Name);

        var updateResponse = await ownerClient.PutAsJsonAsync(
            $"/contact-groups/{created.Id}",
            new UpdateContactGroupRequest("Family"));

        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<ContactGroupDto>();
        Assert.NotNull(updated);
        Assert.Equal("Family", updated!.Name);
        Assert.Equal(2, updated.MemberCount);
    }

    [Fact]
    public async Task AddAndRemoveMember_UpdatesMembers()
    {
        var owner = await RegisterLoginAsync();
        var ownerClient = CreateAuthenticatedClient(owner.Token);

        var contact = await CreateContactAsync(ownerClient, NewEmail(), "Member");
        var group = await CreateGroupAsync(ownerClient, "Group");

        var addResponse = await ownerClient.PostAsJsonAsync(
            $"/contact-groups/{group.Id}/members",
            new AddGroupMemberRequest(contact.Id));

        Assert.Equal(HttpStatusCode.NoContent, addResponse.StatusCode);

        var afterAdd = await GetGroupAsync(ownerClient, group.Id);
        Assert.Equal(1, afterAdd.MemberCount);
        Assert.Contains(afterAdd.Members, c => c.Id == contact.Id);

        var removeResponse = await ownerClient.DeleteAsync($"/contact-groups/{group.Id}/members/{contact.Id}");
        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);

        var afterRemove = await GetGroupAsync(ownerClient, group.Id);
        Assert.Equal(0, afterRemove.MemberCount);
    }

    [Fact]
    public async Task DeleteGroup_RemovesGroup()
    {
        var owner = await RegisterLoginAsync();
        var ownerClient = CreateAuthenticatedClient(owner.Token);

        var group = await CreateGroupAsync(ownerClient, "ToDelete");

        var deleteResponse = await ownerClient.DeleteAsync($"/contact-groups/{group.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await ownerClient.GetAsync($"/contact-groups/{group.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private HttpClient CreateAuthenticatedClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<ContactDto> CreateContactAsync(HttpClient client, string email, string? displayName)
    {
        var response = await client.PostAsJsonAsync("/contacts", new CreateContactRequest(email, displayName));
        response.EnsureSuccessStatusCode();
        var contact = await response.Content.ReadFromJsonAsync<ContactDto>();
        Assert.NotNull(contact);
        return contact!;
    }

    private async Task<ContactGroupDto> CreateGroupAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync(
            "/contact-groups",
            new CreateContactGroupRequest(name, null));

        response.EnsureSuccessStatusCode();
        var group = await response.Content.ReadFromJsonAsync<ContactGroupDto>();
        Assert.NotNull(group);
        return group!;
    }

    private async Task<ContactGroupDto> GetGroupAsync(HttpClient client, Guid groupId)
    {
        var response = await client.GetAsync($"/contact-groups/{groupId}");
        response.EnsureSuccessStatusCode();
        var group = await response.Content.ReadFromJsonAsync<ContactGroupDto>();
        Assert.NotNull(group);
        return group!;
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
