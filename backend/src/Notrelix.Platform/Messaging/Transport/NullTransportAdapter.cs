namespace Notrelix.Platform.Messaging.Transport;

public sealed class NullTransportAdapter : ITransportAdapter
{
    private bool _development;

    public string Name => "Null";

    public bool IsConnected { get; private set; }

    public NullTransportAdapter(bool development = true)
    {
        _development = development;
    }

    public Task<TransportSendResult> SendAsync(
        Runtime.EventEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        if (!_development)
            throw new InvalidOperationException(
                "NullTransportAdapter cannot be used outside development environment");

        return Task.FromResult(TransportSendResult.Ok());
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
