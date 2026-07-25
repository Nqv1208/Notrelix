using System.Net;
using Notrelix.API.Tests.Contracts;

namespace Notrelix.API.Tests.Identity.Auth;

public class AuthenticatedAuthEndpointTests : IClassFixture<NotrelixApiFactory>
{
    private readonly NotrelixApiFactory _factory;

    public AuthenticatedAuthEndpointTests(NotrelixApiFactory factory)
    {
        _factory = factory;
    }

    // ── Auth Requirement Tests ───────────────────────────────
    [Fact]
    public async Task Logout_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/v1/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBootstrap_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/auth/bootstrap");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Success Cases ────────────────────────────────────────
    [Fact]
    public async Task Logout_WithAuth_ReturnsSuccess()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsync("/api/v1/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCurrentUser_WithAuth_ReturnsSuccess()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetBootstrap_WithAuth_ReturnsSuccess()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/auth/bootstrap");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
