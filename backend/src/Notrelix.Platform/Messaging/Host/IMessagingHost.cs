using Notrelix.Platform.Messaging.Observability;
using Notrelix.Platform.Messaging.Reliability;
using Notrelix.Platform.Messaging.Runtime;

namespace Notrelix.Platform.Messaging.Host;

public interface IMessagingHost
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task<MessagingResult> PublishAsync(EventPublication publication, CancellationToken cancellationToken = default);
    Task<DeliveryResult> DeliverAsync(EventEnvelope envelope, Func<Task> sendAsync, DeliveryOptions? options = null, CancellationToken cancellationToken = default);
    Task<MessagingHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default);
}
