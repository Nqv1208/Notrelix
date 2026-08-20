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

    [Fact]
    public async Task StartOAuthLink_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/auth/oauth/google/link/start");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CompleteOAuthLink_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/auth/oauth/google/link/callback?code=code&state=state");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UnlinkOAuth_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/v1/auth/oauth/google/unlink", null);

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

    // ── Invalid Provider Validation ──────────────────────────
    [Fact]
    public async Task StartOAuthLink_WithAuthAndInvalidProvider_ReturnsBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/auth/oauth/notaprovider/link/start");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CompleteOAuthLink_WithAuthAndInvalidProvider_RedirectsToFailureUrl()
    {
        var server = ((Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>)_factory).Server;
        var client = new HttpClient(server.CreateHandler()) { BaseAddress = new Uri("http://localhost") };
        client.DefaultRequestHeaders.Add("X-Test-Auth", "true");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        client.DefaultRequestHeaders.Add("X-Test-Auth", "true");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));

        var response = await client.GetAsync("/api/v1/auth/oauth/notaprovider/link/callback?code=code&state=state");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().Be("/login");
    }

    [Fact]
    public async Task UnlinkOAuth_WithAuthAndInvalidProvider_ReturnsBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsync("/api/v1/auth/oauth/notaprovider/unlink", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
