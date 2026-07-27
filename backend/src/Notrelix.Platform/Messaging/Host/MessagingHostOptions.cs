namespace Notrelix.Platform.Messaging.Host;

public sealed class MessagingHostOptions
{
    public bool AutoConnect { get; set; } = true;
    public int ConnectRetryCount { get; set; } = 3;
    public TimeSpan ConnectRetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromSeconds(30);
}
