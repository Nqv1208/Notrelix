using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Notrelix.API.Tests.Contracts;

/// <summary>
/// Runtime roles change which planes a process mounts. A realtime-only process maps the gateway and no REST
/// route; an api-only process maps the REST routes and no gateway. Health probes are mapped in every role.
/// The mapped route table is the assertion: an unmapped /api path answers 401 from auth middleware, not 404.
/// </summary>
public class RuntimeRoleEndpointTests : IClassFixture<NotrelixApiFactory>
{
    private readonly NotrelixApiFactory _factory;

    public RuntimeRoleEndpointTests(NotrelixApiFactory factory)
    {
        _factory = factory;
    }

    private WebApplicationFactory<Notrelix.API.Program> FactoryWithRoles(string roles) =>
        // Host configuration, not app configuration: Program reads the roles from builder.Configuration before the
        // factory's app-configuration callbacks run.
        _factory.WithWebHostBuilder(builder => builder.UseSetting("Runtime:Roles", roles));

    private static IReadOnlyList<string> MappedPatterns(WebApplicationFactory<Notrelix.API.Program> factory)
    {
        var source = factory.Services.GetRequiredService<EndpointDataSource>();
        return source.Endpoints
            .OfType<RouteEndpoint>()
            .Select(e => e.RoutePattern.RawText ?? string.Empty)
            .ToList();
    }

    [Fact]
    public void RealtimeOnly_DoesNotMapRestRoutes()
    {
        using var factory = FactoryWithRoles("realtime");

        var patterns = MappedPatterns(factory);

        patterns.Should().NotContain(p => p.StartsWith("/api/v1/auth", StringComparison.Ordinal));
    }

    [Fact]
    public void RealtimeOnly_MapsTheGatewayPath()
    {
        using var factory = FactoryWithRoles("realtime");

        MappedPatterns(factory).Should().Contain("/api/v1/realtime");
    }

    [Fact]
    public void ApiOnly_DoesNotMapTheGatewayPath()
    {
        using var factory = FactoryWithRoles("api");

        MappedPatterns(factory).Should().NotContain("/api/v1/realtime");
    }

    [Theory]
    [InlineData("api")]
    [InlineData("realtime")]
    [InlineData("worker")]
    public async Task EveryRole_ServesLiveness(string roles)
    {
        using var factory = FactoryWithRoles(roles);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetString().Should().Be("alive");
    }
}
