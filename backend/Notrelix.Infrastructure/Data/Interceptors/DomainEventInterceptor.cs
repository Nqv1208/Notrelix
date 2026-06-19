using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Events;
using Notrelix.Domain.Common;
using Notrelix.Infrastructure.Data.Outbox;

namespace Notrelix.Infrastructure.Data.Interceptors;

public class DomainEventInterceptor : SaveChangesInterceptor
{
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IEventTypeRegistry _eventTypeRegistry;
    private readonly IIntegrationEventMapper _integrationEventMapper;
    private readonly IMediator _mediator;
    private readonly AsyncLocal<List<IDomainEvent>?> _capturedEvents = new();

    public DomainEventInterceptor(
        IDateTimeProvider dateTimeProvider,
        IEventTypeRegistry eventTypeRegistry,
        IIntegrationEventMapper integrationEventMapper,
        IMediator mediator)
    {
        _dateTimeProvider = dateTimeProvider;
        _eventTypeRegistry = eventTypeRegistry;
        _integrationEventMapper = integrationEventMapper;
        _mediator = mediator;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        CaptureAndPersist(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CaptureAndPersist(eventData.Context);
        return new ValueTask<InterceptionResult<int>>(result);
    }

    public override int SavedChanges(
        SaveChangesCompletedEventData eventData,
        int result)
    {
        var events = _capturedEvents.Value;
        _capturedEvents.Value = null;
        if (events?.Count > 0)
        {
            PublishInline(events);
        }
        return result;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var events = _capturedEvents.Value;
        _capturedEvents.Value = null;
        if (events?.Count > 0)
        {
            await PublishInlineAsync(events, cancellationToken);
        }
        return result;
    }

    private void CaptureAndPersist(DbContext? context)
    {
        if (context is null) return;

        var now = _dateTimeProvider.UtcNow;

        var domainEvents = context.ChangeTracker
            .Entries<Entity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        if (domainEvents.Count == 0) return;

        foreach (var domainEvent in domainEvents)
        {
            var mappings = _integrationEventMapper.Map(domainEvent);
            foreach (var mapping in mappings)
            {
                var message = OutboxMessage.From(mapping.IntegrationEvent, now);
                context.Set<OutboxMessage>().Add(message);
            }


        }

        _capturedEvents.Value = domainEvents;

        foreach (var entry in context.ChangeTracker
            .Entries<Entity>()
            .Where(e => e.Entity.DomainEvents.Any()))
        {
            entry.Entity.ClearDomainEvents();
        }
    }

    private void PublishInline(List<IDomainEvent> domainEvents)
    {
        foreach (var domainEvent in domainEvents)
        {
            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
            var notification = Activator.CreateInstance(notificationType, domainEvent);
            _mediator.Publish(notification!).GetAwaiter().GetResult();
        }
    }

    private async Task PublishInlineAsync(List<IDomainEvent> domainEvents, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in domainEvents)
        {
            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
            var notification = Activator.CreateInstance(notificationType, domainEvent);
            await _mediator.Publish(notification!, cancellationToken);
        }
    }
}
