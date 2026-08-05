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
    private IntegrationEventBatch? _capturedBatch;

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
        CommitCapture();
        return result;
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        CommitCapture();
        return new ValueTask<int>(result);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        RollbackCapture(eventData.Context);
    }

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        RollbackCapture(eventData.Context);
        return Task.CompletedTask;
    }

    private void CaptureAndWriteOutbox(ApplicationDbContext? context)
    {
        if (context is null) return;

        _pendingClear.Clear();
        _generatedEntries.Clear();

        var now = _dateTimeProvider.UtcNow;

        // Phase 1: Capture Domain Events without clearing
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

        // Phase 2: Capture Integration Events reversibly (not destructive)
        _capturedBatch = _integrationEventCollector.CapturePending();
        foreach (var integrationEvent in _capturedBatch.Events)
        {
            WriteIntegrationEventOutboxEntry(context, integrationEvent, now);
        }
    }

    /// <summary>
    /// Called after SaveChanges succeeds: clear Domain Events, acknowledge Integration Events.
    /// </summary>
    private void CommitCapture()
    {
        foreach (var entity in _pendingClear)
            entity.ClearDomainEvents();

        if (_capturedBatch is not null)
        {
            _integrationEventCollector.Acknowledge(_capturedBatch);
            _capturedBatch = null;
        }

        _pendingClear.Clear();
        _generatedEntries.Clear();
    }

    /// <summary>
    /// Called when SaveChanges fails: restore Integration Events, detach generated entries.
    /// Domain Events are NOT cleared — they remain available for retry.
    /// </summary>
    private void RollbackCapture(DbContext? context)
    {
        if (_capturedBatch is not null)
        {
            _integrationEventCollector.Restore(_capturedBatch);
            _capturedBatch = null;
        }

        if (context is not null)
        {
            foreach (var entry in _generatedEntries)
            {
                var tracked = context.Entry(entry);
                if (tracked.State != Microsoft.EntityFrameworkCore.EntityState.Detached)
                    tracked.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
            }
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
