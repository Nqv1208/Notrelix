using Microsoft.Extensions.Logging;
using Notrelix.Platform.Messaging.Transport;

namespace Notrelix.Platform.Messaging.Observability;

public sealed class MessagingHealthCheck
{
    private readonly IConnectionManager _connectionManager;
    private readonly ILogger<MessagingHealthCheck>? _logger;

    public MessagingHealthCheck(
        IConnectionManager connectionManager,
        ILogger<MessagingHealthCheck>? logger = null)
    {
        _connectionManager = connectionManager;
        _logger = logger;
    }

    public Task<MessagingHealthResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var isConnected = _connectionManager.IsConnected;

            if (!isConnected)
            {
                _logger?.LogWarning("Messaging health check: transport not connected");
                return Task.FromResult(MessagingHealthResult.Degraded("Transport not connected"));
            }

            return Task.FromResult(MessagingHealthResult.Healthy());
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Messaging health check failed");
            return Task.FromResult(MessagingHealthResult.Unhealthy(ex.Message));
        }
    }
}

public sealed record MessagingHealthResult
{
    public bool IsHealthy { get; init; }
    public bool IsDegraded { get; init; }
    public bool IsUnhealthy => !IsHealthy && !IsDegraded;
    public string? Message { get; init; }

    public static MessagingHealthResult Healthy() =>
        new() { IsHealthy = true, Message = "Messaging is healthy" };

    public static MessagingHealthResult Degraded(string message) =>
        new() { IsDegraded = true, Message = message };

    public static MessagingHealthResult Unhealthy(string message) =>
        new() { IsHealthy = false, Message = message };
}
