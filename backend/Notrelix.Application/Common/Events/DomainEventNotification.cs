using MediatR;
using Notrelix.Domain.Common;

namespace Notrelix.Application.Common.Events;

public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification
    where TDomainEvent : BaseEvent;
