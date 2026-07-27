using Notrelix.Platform.Messaging.Runtime;

namespace Notrelix.Platform.Messaging.Consumers;

public sealed record ConsumerRegistration
{
    public string EventName { get; init; } = string.Empty;
    public int? EventVersion { get; init; }
    public Func<EventEnvelope, CancellationToken, Task> Handler { get; init; } = (_, _) => Task.CompletedTask;
    public ConsumerOptions Options { get; init; } = new();
    public DateTimeOffset RegisteredAt { get; init; } = DateTimeOffset.UtcNow;
}
