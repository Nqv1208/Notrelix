using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Notrelix.Infrastructure.Messaging.Options;
using Notrelix.Infrastructure.Observability.HealthChecks;

namespace Notrelix.Infrastructure.Tests.Observability;

public class RabbitMqBrokerHealthCheckTests
{
    private static RabbitMqBrokerHealthCheck Check(string host, int port) =>
        new(Microsoft.Extensions.Options.Options.Create(new RabbitMqOptions { Host = host, Port = port }));

    [Fact]
    public async Task ReachableEndpoint_IsHealthy()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var result = await Check("127.0.0.1", port).CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task ClosedPort_IsUnhealthy()
    {
        int port;
        using (var probe = new TcpListener(IPAddress.Loopback, 0))
        {
            probe.Start();
            port = ((IPEndPoint)probe.LocalEndpoint).Port;
        }

        var result = await Check("127.0.0.1", port).CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }
}
