namespace Notrelix.Domain.Common;

/// <summary>
/// Marker for events that need durable delivery via outbox, projection, cross-context handler,
/// or audit. These events are dispatched after commit.
/// </summary>
public interface IDurableDomainEvent : IDomainEvent { }
