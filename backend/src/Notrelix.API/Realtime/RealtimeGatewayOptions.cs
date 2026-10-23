using Microsoft.Extensions.Options;

namespace Notrelix.API.Realtime;

/// <summary>
/// Realtime WebSocket gateway configuration (section <c>Realtime:Gateway</c>).
/// Every limit is bounded so a single client cannot hold unbounded server state.
/// </summary>
public sealed class RealtimeGatewayOptions
{
    public const string SectionName = "Realtime:Gateway";
    public const string DefaultPath = "/api/v1/realtime";

    public bool Enabled { get; set; } = true;

    public string Path { get; set; } = DefaultPath;

    /// <summary>
    /// Allowed browser origins. When empty the gateway falls back to
    /// <c>Cors:AllowedOrigins</c>. A wildcard is accepted in Development only.
    /// </summary>
    public string[] AllowedOrigins { get; set; } = [];

    public int MaxSubscriptionsPerConnection { get; set; } = 50;

    public int MaxConnectionsPerUser { get; set; } = 8;

    public int MaxInboundMessageBytes { get; set; } = 4096;

    public int MaxInboundMessagesPerSecond { get; set; } = 20;

    /// <summary>Outbound queue capacity; overflow closes a slow consumer (no replay buffer exists).</summary>
    public int SendQueueCapacity { get; set; } = 256;

    public TimeSpan PingInterval { get; set; } = TimeSpan.FromSeconds(25);

    /// <summary>Maximum silence from the client before the connection is closed.</summary>
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromSeconds(75);

    /// <summary>How often credentials and every subscription are re-authorized.</summary>
    public TimeSpan ReauthorizationInterval { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>Hard upper bound on one connection; the client reconnects and refetches.</summary>
    public TimeSpan MaxConnectionLifetime { get; set; } = TimeSpan.FromHours(1);

    public TimeSpan SendTimeout { get; set; } = TimeSpan.FromSeconds(10);
}

public sealed class RealtimeGatewayOptionsValidator : IValidateOptions<RealtimeGatewayOptions>
{
    public ValidateOptionsResult Validate(string? name, RealtimeGatewayOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Path) || !options.Path.StartsWith('/'))
        {
            failures.Add("Realtime:Gateway:Path must start with '/'.");
        }

        Positive(options.MaxSubscriptionsPerConnection, nameof(options.MaxSubscriptionsPerConnection), 1_000, failures);
        Positive(options.MaxConnectionsPerUser, nameof(options.MaxConnectionsPerUser), 1_000, failures);
        Positive(options.MaxInboundMessageBytes, nameof(options.MaxInboundMessageBytes), 65_536, failures);
        Positive(options.MaxInboundMessagesPerSecond, nameof(options.MaxInboundMessagesPerSecond), 1_000, failures);
        Positive(options.SendQueueCapacity, nameof(options.SendQueueCapacity), 100_000, failures);

        PositiveSpan(options.PingInterval, nameof(options.PingInterval), failures);
        PositiveSpan(options.IdleTimeout, nameof(options.IdleTimeout), failures);
        PositiveSpan(options.ReauthorizationInterval, nameof(options.ReauthorizationInterval), failures);
        PositiveSpan(options.MaxConnectionLifetime, nameof(options.MaxConnectionLifetime), failures);
        PositiveSpan(options.SendTimeout, nameof(options.SendTimeout), failures);

        if (options.IdleTimeout <= options.PingInterval)
        {
            failures.Add("Realtime:Gateway:IdleTimeout must be greater than PingInterval.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void Positive(int value, string name, int max, List<string> failures)
    {
        if (value < 1 || value > max)
        {
            failures.Add($"Realtime:Gateway:{name} must be between 1 and {max}.");
        }
    }

    private static void PositiveSpan(TimeSpan value, string name, List<string> failures)
    {
        if (value <= TimeSpan.Zero)
        {
            failures.Add($"Realtime:Gateway:{name} must be greater than zero.");
        }
    }
}
