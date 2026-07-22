namespace Notrelix.Platform.Messaging.Transport;

public sealed record TransportSendResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    public static TransportSendResult Ok() => new() { Success = true };

    public static TransportSendResult Fail(string message) =>
        new() { Success = false, ErrorMessage = message };
}

public interface ITransportAdapter
{
    string Name { get; }
    Task<TransportSendResult> SendAsync(
        Runtime.EventEnvelope envelope,
        CancellationToken cancellationToken = default);
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    bool IsConnected { get; }
}
