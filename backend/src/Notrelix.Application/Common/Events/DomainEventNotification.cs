using MediatR;

namespace Notrelix.Application.Common.Events;

public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification
    where TDomainEvent : DomainEvent;
