using Microsoft.Extensions.Logging;

namespace Notrelix.Platform.Messaging.Transport;

public sealed class RabbitMqTransportAdapter : ITransportAdapter
{
    private readonly ITransportPolicy _transportPolicy;
    private readonly ILogger<RabbitMqTransportAdapter>? _logger;

    public string Name => "RabbitMQ";

    public bool IsConnected { get; private set; }

    public RabbitMqTransportAdapter(
        ITransportPolicy transportPolicy,
        ILogger<RabbitMqTransportAdapter>? logger = null)
    {
        _transportPolicy = transportPolicy;
        _logger = logger;
    }

    public Task<TransportSendResult> SendAsync(
        Runtime.EventEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("RabbitMQ transport not yet implemented");
    }

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        IsConnected = true;
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        IsConnected = false;
        return Task.CompletedTask;
    }
}
