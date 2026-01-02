using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MujDomecek.Application.DTOs;
using MujDomecek.Domain.Aggregates.User;

namespace MujDomecek.Api.Tests;

public sealed class DocumentEndpointTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DocumentEndpointTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AddDocument_ListsAndGetsDetail()
    {
        var owner = await RegisterLoginAsync();
        var ownerClient = CreateAuthenticatedClient(owner.Token);
        var projectId = await CreateProjectAsync(ownerClient);
        var propertyId = await CreatePropertyAsync(ownerClient, projectId);
        var zaznam = await CreateZaznamAsync(ownerClient, propertyId);

        var created = await AddDocumentAsync(ownerClient, zaznam.Id, "docs/file1.pdf");

        var listResponse = await ownerClient.GetAsync($"/zaznamy/{zaznam.Id}/documents");
        listResponse.EnsureSuccessStatusCode();
        var list = await listResponse.Content.ReadFromJsonAsync<List<DocumentDto>>();
        Assert.NotNull(list);
        Assert.Contains(list!, d => d.Id == created.Id);

        var detailResponse = await ownerClient.GetAsync($"/documents/{created.Id}");
        detailResponse.EnsureSuccessStatusCode();
        var detail = await detailResponse.Content.ReadFromJsonAsync<DocumentDto>();
        Assert.NotNull(detail);
        Assert.Equal(created.Id, detail!.Id);
        Assert.Equal("document", detail.Type);
    }

    [Fact]
    public async Task UpdateDocument_UpdatesDescription()
    {
        var owner = await RegisterLoginAsync();
        var ownerClient = CreateAuthenticatedClient(owner.Token);
        var projectId = await CreateProjectAsync(ownerClient);
        var propertyId = await CreatePropertyAsync(ownerClient, projectId);
        var zaznam = await CreateZaznamAsync(ownerClient, propertyId);

        var created = await AddDocumentAsync(ownerClient, zaznam.Id, "docs/file2.pdf");

        var updateResponse = await ownerClient.PutAsJsonAsync(
            $"/documents/{created.Id}",
            new UpdateDocumentRequest("Updated"));

        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<DocumentDto>();
        Assert.NotNull(updated);
        Assert.Equal("Updated", updated!.Description);
    }

    [Fact]
    public async Task DeleteDocument_RemovesDocument()
    {
        var owner = await RegisterLoginAsync();
        var ownerClient = CreateAuthenticatedClient(owner.Token);
        var projectId = await CreateProjectAsync(ownerClient);
        var propertyId = await CreatePropertyAsync(ownerClient, projectId);
        var zaznam = await CreateZaznamAsync(ownerClient, propertyId);

        var created = await AddDocumentAsync(ownerClient, zaznam.Id, "docs/file3.pdf");

        var deleteResponse = await ownerClient.DeleteAsync($"/documents/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await ownerClient.GetAsync($"/documents/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetDocumentUrl_ReturnsUrl()
    {
        var owner = await RegisterLoginAsync();
        var ownerClient = CreateAuthenticatedClient(owner.Token);
        var projectId = await CreateProjectAsync(ownerClient);
        var propertyId = await CreatePropertyAsync(ownerClient, projectId);
        var zaznam = await CreateZaznamAsync(ownerClient, propertyId);

        var created = await AddDocumentAsync(ownerClient, zaznam.Id, "docs/file4.pdf");

        var urlResponse = await ownerClient.GetAsync($"/documents/{created.Id}/url");
        urlResponse.EnsureSuccessStatusCode();
        var url = await urlResponse.Content.ReadFromJsonAsync<DocumentUrlResponse>();
        Assert.NotNull(url);
        Assert.False(string.IsNullOrWhiteSpace(url!.Url));
        Assert.True(url.ExpiresAt > DateTime.UtcNow);
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

    private async Task<DocumentDto> AddDocumentAsync(HttpClient client, Guid zaznamId, string storageKey)
    {
        var response = await client.PostAsJsonAsync(
            $"/zaznamy/{zaznamId}/documents",
            new AddDocumentRequest(storageKey, "document", "file.pdf", "application/pdf", 1234, "Doc"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var document = await response.Content.ReadFromJsonAsync<DocumentDto>();
        Assert.NotNull(document);
        return document!;
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
