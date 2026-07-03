using Notrelix.Application.Common.Events;

namespace Notrelix.Application.Common.Messaging;

/// <summary>
/// Application-level consumer for integration events.
/// Runs inside <c>ConsumerPipelineExecutor</c> with RLS + transaction + idempotency.
/// </summary>
public interface IIntegrationEventConsumer<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent message, CancellationToken cancellationToken);
}
