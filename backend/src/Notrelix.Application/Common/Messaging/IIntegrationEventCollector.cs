namespace Notrelix.Application.Common.Messaging;

/// <summary>
/// Reversible integration event collection.
/// Events are captured (not removed) during SavingChanges,
/// acknowledged (removed) after SavedChanges,
/// and restored on SaveChangesFailed.
/// </summary>
public interface IIntegrationEventCollector
{
    void Add(IIntegrationEvent integrationEvent);

    /// <summary>
    /// Captures all pending events into a reversible batch.
    /// Events remain in the collector until acknowledged.
    /// </summary>
    IntegrationEventBatch CapturePending();

    /// <summary>
    /// Removes the acknowledged events from the collector.
    /// Called only after SaveChanges succeeds.
    /// </summary>
    void Acknowledge(IntegrationEventBatch batch);

    /// <summary>
    /// Restores a captured batch back into the collector.
    /// Called when SaveChanges fails or is cancelled.
    /// </summary>
    void Restore(IntegrationEventBatch batch);
}

public sealed record IntegrationEventBatch(
    Guid BatchId,
    IReadOnlyList<IIntegrationEvent> Events);
