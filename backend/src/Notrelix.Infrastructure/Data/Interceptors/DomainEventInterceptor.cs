using Microsoft.EntityFrameworkCore.Diagnostics;
using Notrelix.Infrastructure.Data.Events;
using Notrelix.Infrastructure.Data.Messaging;

namespace Notrelix.Infrastructure.Data.Interceptors;

public class DomainEventInterceptor : SaveChangesInterceptor
{
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IEventTypeRegistry _eventTypeRegistry;
    private readonly IClassificationPolicy _classificationPolicy;
    private readonly IDeliveryPolicy _deliveryPolicy;
    private readonly IIntegrationEventMapper _integrationEventMapper;
    private readonly IIntegrationEventCollector _integrationEventCollector;

    private readonly List<IHasDomainEvents> _pendingClear = [];
    private readonly List<object> _generatedEntries = [];

    public DomainEventInterceptor(
        IDateTimeProvider dateTimeProvider,
        IEventTypeRegistry eventTypeRegistry,
        IClassificationPolicy classificationPolicy,
        IDeliveryPolicy deliveryPolicy,
        IIntegrationEventMapper integrationEventMapper,
        IIntegrationEventCollector integrationEventCollector)
    {
        _dateTimeProvider = dateTimeProvider;
        _eventTypeRegistry = eventTypeRegistry;
        _classificationPolicy = classificationPolicy;
        _deliveryPolicy = deliveryPolicy;
        _integrationEventMapper = integrationEventMapper;
        _integrationEventCollector = integrationEventCollector;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        CaptureAndWriteOutbox(eventData.Context as ApplicationDbContext);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CaptureAndWriteOutbox(eventData.Context as ApplicationDbContext);
        return new ValueTask<InterceptionResult<int>>(result);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        ClearDomainEvents();
        return result;
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        ClearDomainEvents();
        return new ValueTask<int>(result);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        DetachGeneratedEntries(eventData.Context);
    }

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        DetachGeneratedEntries(eventData.Context);
        return Task.CompletedTask;
    }

    private void CaptureAndWriteOutbox(ApplicationDbContext? context)
    {
        if (context is null) return;

        _pendingClear.Clear();
        _generatedEntries.Clear();

        var now = _dateTimeProvider.UtcNow;

        var entries = context.ChangeTracker
            .Entries<Entity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .ToList();

        foreach (var entry in entries)
        {
            foreach (var domainEvent in entry.Entity.DomainEvents)
            {
                var messageName = _eventTypeRegistry.GetMessageName(domainEvent.GetType());
                WriteOutboxEntries(context, (DomainEvent)domainEvent, messageName, now);
            }

            if (entry.Entity is IHasDomainEvents hasDomainEvents)
                _pendingClear.Add(hasDomainEvents);
        }

        var pendingIntegrationEvents = _integrationEventCollector.DequeueAll() ?? [];
        foreach (var integrationEvent in pendingIntegrationEvents)
        {
            WriteIntegrationEventOutboxEntry(context, integrationEvent, now);
        }
    }

    private void ClearDomainEvents()
    {
        foreach (var entity in _pendingClear)
            entity.ClearDomainEvents();

        _pendingClear.Clear();
        _generatedEntries.Clear();
    }

    private void DetachGeneratedEntries(DbContext? context)
    {
        if (context is null) return;

        foreach (var entry in _generatedEntries)
        {
            var tracked = context.Entry(entry);
            if (tracked.State != Microsoft.EntityFrameworkCore.EntityState.Detached)
                tracked.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        }

        _generatedEntries.Clear();
        _pendingClear.Clear();
    }

    private void WriteOutboxEntries(ApplicationDbContext context, DomainEvent domainEvent, string messageName, DateTimeOffset now)
    {
        var eventLog = DomainEventLog.FromDomainEvent(domainEvent, messageName, now);
        context.Set<DomainEventLog>().Add(eventLog);
        _generatedEntries.Add(eventLog);

        var mappings = _integrationEventMapper.Map(domainEvent);
        foreach (var mapping in mappings)
        {
            var outboxMsg = MessagingOutboxMessage.FromIntegrationEvent(mapping.IntegrationEvent, domainEvent, now);
            context.Set<MessagingOutboxMessage>().Add(outboxMsg);
            _generatedEntries.Add(outboxMsg);
        }
    }

    private void WriteIntegrationEventOutboxEntry(
        ApplicationDbContext context,
        IIntegrationEvent integrationEvent,
        DateTimeOffset now)
    {
        var outboxMsg = MessagingOutboxMessage.FromIntegrationEvent(integrationEvent, now);
        context.Set<MessagingOutboxMessage>().Add(outboxMsg);
        _generatedEntries.Add(outboxMsg);
    }
}
