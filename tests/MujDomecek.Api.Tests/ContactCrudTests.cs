using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MujDomecek.Application.DTOs;
using MujDomecek.Domain.Aggregates.User;

namespace MujDomecek.Api.Tests;

public sealed class ContactCrudTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ContactCrudTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateContact_ListsAndGetsDetail_WithRegisteredMatch()
    {
        var owner = await RegisterLoginAsync();
        var ownerClient = CreateAuthenticatedClient(owner.Token);

        var recipient = await RegisterLoginAsync();
        var createResponse = await ownerClient.PostAsJsonAsync(
            "/contacts",
            new CreateContactRequest(recipient.Email, "Recipient"));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<ContactDto>();
        Assert.NotNull(created);
        Assert.True(created!.IsRegistered);
        Assert.Equal(recipient.UserId, created.UserId);

        var listResponse = await ownerClient.GetAsync("/contacts");
        listResponse.EnsureSuccessStatusCode();
        var list = await listResponse.Content.ReadFromJsonAsync<List<ContactDto>>();
        Assert.NotNull(list);
        Assert.Contains(list!, c => c.Id == created.Id);

        var detailResponse = await ownerClient.GetAsync($"/contacts/{created.Id}");
        detailResponse.EnsureSuccessStatusCode();
        var detail = await detailResponse.Content.ReadFromJsonAsync<ContactDto>();
        Assert.NotNull(detail);
        Assert.Equal(created.Id, detail!.Id);
        Assert.Equal("Recipient", detail.DisplayName);
    }

    [Fact]
    public async Task UpdateContact_UpdatesDisplayName()
    {
        var owner = await RegisterLoginAsync();
        var ownerClient = CreateAuthenticatedClient(owner.Token);

        var created = await CreateContactAsync(ownerClient, NewEmail(), "Original");

        var updateResponse = await ownerClient.PutAsJsonAsync(
            $"/contacts/{created.Id}",
            new UpdateContactRequest("Updated"));

        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<ContactDto>();
        Assert.NotNull(updated);
        Assert.Equal("Updated", updated!.DisplayName);
    }

    [Fact]
    public async Task DeleteContact_RemovesContact()
    {
        var owner = await RegisterLoginAsync();
        var ownerClient = CreateAuthenticatedClient(owner.Token);

        var created = await CreateContactAsync(ownerClient, NewEmail(), null);

        var deleteResponse = await ownerClient.DeleteAsync($"/contacts/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await ownerClient.GetAsync($"/contacts/{created.Id}");
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
