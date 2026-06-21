using System.Net;
using System.Net.Http.Json;

namespace Notrelix.API.Tests.Contracts;

public class HealthEndpointTests : IClassFixture<NotrelixApiFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(NotrelixApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().ContainKey("status");
    }
}
