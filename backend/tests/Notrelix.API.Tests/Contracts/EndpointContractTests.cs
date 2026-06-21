using System.Net;

namespace Notrelix.API.Tests.Contracts;

public class EndpointContractTests : IClassFixture<NotrelixApiFactory>
{
    private readonly HttpClient _client;

    public EndpointContractTests(NotrelixApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UnauthenticatedRequestToSecureEndpoint_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/workspaces");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task NonExistentEndpoint_Returns404()
    {
        var response = await _client.GetAsync("/api/v1/nonexistent");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
