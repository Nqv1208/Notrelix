using System.Net;

namespace Notrelix.API.Tests.Contracts;

public class HealthEndpointTests : IClassFixture<NotrelixApiFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(NotrelixApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
