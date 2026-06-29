namespace Notrelix.Domain.Common;

/// <summary>
/// Marker for events that are processed inline within the same process.
/// No outbox, no durable delivery. Used for internal signals, projections, runtime state.
/// </summary>
public interface ILocalDomainEvent : IDomainEvent { }
