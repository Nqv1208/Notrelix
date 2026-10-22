using System.Net;
using System.Text.Json;
using Notrelix.API.Tests.Contracts;

namespace Notrelix.API.Tests.Identity.Auth;

public class SecuritySummaryEndpointTests : IClassFixture<NotrelixApiFactory>
{
    private readonly NotrelixApiFactory _factory;

    public SecuritySummaryEndpointTests(NotrelixApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Get_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/auth/security/summary");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_WithAuth_ReturnsTheTypedSummaryWithoutSecrets()
    {
        var response = await _factory.CreateAuthenticatedClient().GetAsync("/api/v1/auth/security/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var text = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(text).RootElement;
        json.GetProperty("hasPasswordCredential").GetBoolean().Should().BeTrue();
        json.GetProperty("activeSessionCount").GetInt32().Should().Be(1);
        json.GetProperty("mfa").GetProperty("isEnabled").GetBoolean().Should().BeFalse();
        json.GetProperty("linkedProviders").EnumerateArray().Select(e => e.GetString()).Should().Equal("Google");
        json.EnumerateObject().Select(p => p.Name.ToLowerInvariant())
            .Should().NotContain(n => n.Contains("hash") || n.Contains("secret") || n.Contains("token"));
    }
}
