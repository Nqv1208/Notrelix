using System.Net;
using Notrelix.API.Tests.Contracts;

namespace Notrelix.API.Tests.Admin;

public class AdminEndpointAuthorizationTests : IClassFixture<NotrelixApiFactory>
{
    private readonly NotrelixApiFactory _factory;

    public AdminEndpointAuthorizationTests(NotrelixApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task OutboxStats_Unauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/admin/outbox/stats");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task OutboxStats_NormalUser_ReturnsForbidden()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/admin/outbox/stats");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task OutboxStats_SystemAdmin_ReturnsSuccess()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Auth", "true");
        client.DefaultRequestHeaders.Add("X-Test-Roles", "SystemAdmin");

        var response = await client.GetAsync("/admin/outbox/stats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OutboxPending_SystemAdmin_ReturnsSuccess()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Auth", "true");
        client.DefaultRequestHeaders.Add("X-Test-Roles", "SystemAdmin");

        var response = await client.GetAsync("/admin/outbox/pending");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OutboxFailed_SystemAdmin_ReturnsSuccess()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Auth", "true");
        client.DefaultRequestHeaders.Add("X-Test-Roles", "SystemAdmin");

        var response = await client.GetAsync("/admin/outbox/failed");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
