using Notrelix.Platform.Messaging.Runtime;

namespace Notrelix.Platform.Messaging.Consumers;

public interface IConsumerHost
{
    void Register(string eventName, Func<EventEnvelope, CancellationToken, Task> handler, Action<ConsumerOptions>? configure = null);
    Task DispatchAsync(EventEnvelope envelope, CancellationToken cancellationToken = default);
    IReadOnlyList<ConsumerRegistration> GetRegistrations();
}
