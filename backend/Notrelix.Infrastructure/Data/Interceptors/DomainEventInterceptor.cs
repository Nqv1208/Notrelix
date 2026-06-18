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
    private readonly AsyncLocal<List<IDomainEvent>?> _capturedEvents = new();

    public DomainEventInterceptor(
        IDateTimeProvider dateTimeProvider,
        IEventTypeRegistry eventTypeRegistry,
        IIntegrationEventMapper integrationEventMapper)
    {
        _dateTimeProvider = dateTimeProvider;
        _eventTypeRegistry = eventTypeRegistry;
        _integrationEventMapper = integrationEventMapper;
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
        var events = _capturedEvents.Value;
        _capturedEvents.Value = null;
        if (events?.Count > 0)
        {
            PersistIntegrationEventOutbox(eventData.Context, events);
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
            await PersistIntegrationEventOutboxAsync(eventData.Context, events, cancellationToken);
        }
        return result;
    }

    private void CaptureEvents(DbContext? context)
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
            var messageName = _eventTypeRegistry.GetMessageName(domainEvent.GetType());
            var message = OutboxMessage.From(domainEvent, now, messageName);
            context.Set<OutboxMessage>().Add(message);
        }

        _capturedEvents.Value = domainEvents;

        foreach (var entry in context.ChangeTracker
            .Entries<Entity>()
            .Where(e => e.Entity.DomainEvents.Any()))
        {
            entry.Entity.ClearDomainEvents();
        }
    }

    private void PersistIntegrationEventOutbox(DbContext? context, List<IDomainEvent> domainEvents)
    {
        if (context is null) return;
        var now = _dateTimeProvider.UtcNow;

        foreach (var domainEvent in domainEvents)
        {
            var mappings = _integrationEventMapper.Map(domainEvent);
            foreach (var mapping in mappings)
            {
                var message = OutboxMessage.From(mapping.IntegrationEvent, now);
                context.Set<OutboxMessage>().Add(message);
            }
        }

        if (domainEvents.Any(e => _integrationEventMapper.Map(e).Count > 0))
        {
            context.SaveChanges();
        }
    }

    private async Task PersistIntegrationEventOutboxAsync(
        DbContext? context,
        List<IDomainEvent> domainEvents,
        CancellationToken cancellationToken)
    {
        if (context is null) return;
        var now = _dateTimeProvider.UtcNow;
        var hasMappings = false;

        foreach (var domainEvent in domainEvents)
        {
            var mappings = _integrationEventMapper.Map(domainEvent);
            foreach (var mapping in mappings)
            {
                hasMappings = true;
                var message = OutboxMessage.From(mapping.IntegrationEvent, now);
                context.Set<OutboxMessage>().Add(message);
            }
        }

        if (hasMappings)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
