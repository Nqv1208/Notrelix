namespace Notrelix.Platform.Messaging.Transport;

public interface IConnectionManager
{
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    bool IsConnected { get; }
    string TransportName { get; }
}
