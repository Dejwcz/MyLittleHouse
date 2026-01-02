using System.Net;
using System.Net.Http.Json;

namespace MujDomecek.Api.Tests;

public sealed class ApiSmokeTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiSmokeTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRoot_ReturnsApiBanner()
    {
        var response = await _client.GetAsync("/");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal("MujDomecek API", body);
    }

    [Fact]
    public async Task Login_WithUnknownUser_ReturnsUnauthorized()
    {
        var payload = new { email = "missing@example.com", password = "Passw0rd!" };

        var response = await _client.PostAsJsonAsync("/auth/login", payload);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
