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
    private readonly IMediator _mediator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly AsyncLocal<List<IDomainEvent>?> _syncEvents = new();

    public DomainEventInterceptor(IMediator mediator, IDateTimeProvider dateTimeProvider)
    {
        _mediator = mediator;
        _dateTimeProvider = dateTimeProvider;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        CaptureEvents(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CaptureEvents(eventData.Context);
        return new ValueTask<InterceptionResult<int>>(result);
    }

    public override int SavedChanges(
        SaveChangesCompletedEventData eventData,
        int result)
    {
        var events = _syncEvents.Value;
        _syncEvents.Value = null;
        if (events?.Count > 0)
        {
            PublishSyncEventsAsync(events, CancellationToken.None).GetAwaiter().GetResult();
        }
        return result;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var events = _syncEvents.Value;
        _syncEvents.Value = null;
        if (events?.Count > 0)
        {
            await PublishSyncEventsAsync(events, cancellationToken);
        }
        return result;
    }

    private void CaptureEvents(DbContext? context)
    {
        if (context is null) return;

        var domainEvents = context.ChangeTracker
            .Entries<Entity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        if (domainEvents.Count == 0) return;

        var outboxEvents = domainEvents.OfType<IOutboxEvent>().ToList();
        var now = _dateTimeProvider.UtcNow;
        foreach (var outboxEvent in outboxEvents)
        {
            var message = OutboxMessage.From((IDomainEvent)outboxEvent, now);
            context.Set<OutboxMessage>().Add(message);
        }

        _syncEvents.Value = domainEvents
            .Where(e => e is not IOutboxEvent)
            .ToList();

        foreach (var entry in context.ChangeTracker
            .Entries<Entity>()
            .Where(e => e.Entity.DomainEvents.Any()))
        {
            entry.Entity.ClearDomainEvents();
        }
    }

    private async Task PublishSyncEventsAsync(List<IDomainEvent> events, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in events)
        {
            var notification = CreateDomainEventNotification(domainEvent);
            await _mediator.Publish(notification, cancellationToken);
        }
    }

    private static object CreateDomainEventNotification(IDomainEvent domainEvent)
    {
        var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
        return Activator.CreateInstance(notificationType, domainEvent)!;
    }
}
