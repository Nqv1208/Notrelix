using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Notrelix.API.Tests.Contracts;

namespace Notrelix.API.Tests.Identity.Profile;

public class AccountPreferencesEndpointTests : IClassFixture<NotrelixApiFactory>
{
    private readonly NotrelixApiFactory _factory;

    public AccountPreferencesEndpointTests(NotrelixApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Get_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/profile/preferences");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Patch_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _factory.CreateClient().PatchAsJsonAsync("/api/v1/profile/preferences", new { expectedVersion = 0, theme = "dark" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_WithAuth_ReturnsTheTypedPreferencesWithVersion()
    {
        var response = await _factory.CreateAuthenticatedClient().GetAsync("/api/v1/profile/preferences");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        json.GetProperty("locale").GetString().Should().Be("en");
        json.GetProperty("timezone").GetString().Should().Be("Asia/Tokyo");
        json.GetProperty("theme").GetString().Should().Be("dark");
        json.GetProperty("version").GetInt64().Should().Be(2);
    }

    [Fact]
    public async Task Patch_WithAuthAndVersion_ReturnsTheSavedPreferences()
    {
        var response = await _factory.CreateAuthenticatedClient()
            .PatchAsJsonAsync("/api/v1/profile/preferences", new { expectedVersion = 2, theme = "light" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        json.GetProperty("version").GetInt64().Should().Be(3);
    }

    [Fact]
    public async Task Patch_WithoutExpectedVersion_IsRejectedAsBadRequest()
    {
        var response = await _factory.CreateAuthenticatedClient()
            .PatchAsJsonAsync("/api/v1/profile/preferences", new { theme = "light" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Theory]
    [InlineData("{\"expectedVersion\":1,\"theme\":\"neon\"}")]
    [InlineData("{\"expectedVersion\":1,\"timezone\":\"Not/AZone\"}")]
    [InlineData("{\"expectedVersion\":1,\"locale\":\"english\"}")]
    [InlineData("{\"expectedVersion\":1}")]
    [InlineData("{\"expectedVersion\":-1,\"theme\":\"dark\"}")]
    public async Task Patch_WithInvalidValues_IsRejectedAsBadRequest(string body)
    {
        var response = await _factory.CreateAuthenticatedClient().PatchAsync(
            "/api/v1/profile/preferences",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
