using Notrelix.Platform.Messaging.Runtime;

namespace Notrelix.Platform.Messaging.Consumers;

public sealed record ConsumerRegistration
{
    public string EventName { get; init; } = string.Empty;
    public int? EventVersion { get; init; }
    public Func<EventEnvelope, CancellationToken, Task> Handler { get; init; } = (_, _) => Task.CompletedTask;

    /// <summary>
    /// Service-provider-aware handler used by typed Application consumers
    /// (spec 3.4): each delivery receives the root provider so it can create its
    /// own DI scope. Takes precedence over <see cref="Handler"/> when set.
    /// </summary>
    public Func<IServiceProvider, EventEnvelope, CancellationToken, Task>? ScopedHandler { get; init; }

    public ConsumerOptions Options { get; init; } = new();
    public DateTimeOffset RegisteredAt { get; init; } = DateTimeOffset.UtcNow;
}
