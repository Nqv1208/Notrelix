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
        CaptureAndHandle(eventData.Context as ApplicationDbContext);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CaptureAndHandle(eventData.Context as ApplicationDbContext);
        return new ValueTask<InterceptionResult<int>>(result);
    }

    private void CaptureAndHandle(ApplicationDbContext? context)
    {
        if (context is null) return;

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
        }

        foreach (var entry in entries)
        {
            entry.Entity.ClearDomainEvents();
        }

        // Persist use-case integration events (collected at Application layer)
        var pendingIntegrationEvents = _integrationEventCollector.DequeueAll() ?? [];
        foreach (var integrationEvent in pendingIntegrationEvents)
        {
            WriteIntegrationEventOutboxEntry(context, integrationEvent, now);
        }
    }

    private void WriteOutboxEntries(ApplicationDbContext context, DomainEvent domainEvent, string messageName, DateTimeOffset now)
    {
        var eventLog = DomainEventLog.FromDomainEvent(domainEvent, messageName, now);
        context.Set<DomainEventLog>().Add(eventLog);

        var mappings = _integrationEventMapper.Map(domainEvent);
        foreach (var mapping in mappings)
        {
            var outboxMsg = MessagingOutboxMessage.FromIntegrationEvent(mapping.IntegrationEvent, domainEvent, now);
            context.Set<MessagingOutboxMessage>().Add(outboxMsg);
        }
    }

    private void WriteIntegrationEventOutboxEntry(
        ApplicationDbContext context,
        IIntegrationEvent integrationEvent,
        DateTimeOffset now)
    {
        var outboxMsg = MessagingOutboxMessage.FromIntegrationEvent(integrationEvent, now);
        context.Set<MessagingOutboxMessage>().Add(outboxMsg);
    }
}
