using System.Net;
using System.Net.Http.Json;
using Notrelix.API.Tests.Contracts;

namespace Notrelix.API.Tests.Identity.Profile;

public class ProfileEndpointTests : IClassFixture<NotrelixApiFactory>
{
    private readonly NotrelixApiFactory _factory;

    public ProfileEndpointTests(NotrelixApiFactory factory)
    {
        _factory = factory;
    }

    // ── Auth Requirement ────────────────────────────────────
    [Fact]
    public async Task UpdateProfile_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PatchAsync("/api/v1/profile/", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Success Cases ────────────────────────────────────────
    [Fact]
    public async Task UpdateProfile_WithAuth_ReturnsSuccess()
    {
        var client = _factory.CreateAuthenticatedClient();
        var body = new { Name = "Updated Name", Bio = "New bio" };

        var response = await client.PatchAsJsonAsync("/api/v1/profile/", body);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Validation ──────────────────────────────────────────
    [Fact]
    public async Task UpdateProfile_WithEmptyName_ReturnsBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient();
        var body = new { Name = "", Bio = "Some bio" };

        var response = await client.PatchAsJsonAsync("/api/v1/profile/", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
