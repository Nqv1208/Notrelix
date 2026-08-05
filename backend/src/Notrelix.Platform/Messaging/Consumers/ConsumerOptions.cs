namespace Notrelix.Platform.Messaging.Consumers;

public sealed class ConsumerOptions
{
    public int ConcurrencyLimit { get; set; } = 1;
    public bool OrderingRequired { get; set; }
    public int PoisonThreshold { get; set; } = 10;
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// How long a dispatch waits for a concurrency slot before failing with
    /// <see cref="ConsumerBackpressureException"/>. Messages are never dropped.
    /// </summary>
    public TimeSpan QueueWaitTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// When set, envelopes whose <c>EventVersion</c> differs are rejected with a
    /// typed contract exception instead of being delivered.
    /// </summary>
    public int? ExpectedEventVersion { get; set; }
}
