using System.Net.Sockets;
using Notrelix.Infrastructure.Messaging.Options;

namespace Notrelix.Infrastructure.Observability.HealthChecks;

/// <summary>
/// Broker reachability for readiness: a TCP connect to the configured RabbitMQ endpoint with a short timeout.
/// MassTransit reconnects on its own, so this check answers only "can this process reach the broker now".
/// Registered only for the worker role on the RabbitMQ transport.
/// </summary>
public sealed class RabbitMqBrokerHealthCheck : IHealthCheck
{
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(2);

    private readonly IOptions<RabbitMqOptions> _options;

    public RabbitMqBrokerHealthCheck(IOptions<RabbitMqOptions> options)
    {
        _options = options;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var rabbit = _options.Value;
        try
        {
            using var client = new TcpClient();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(ConnectTimeout);
            await client.ConnectAsync(rabbit.Host, rabbit.Port, timeout.Token);
            return HealthCheckResult.Healthy("RabbitMQ endpoint reachable");
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ endpoint unreachable", ex);
        }
    }
}
