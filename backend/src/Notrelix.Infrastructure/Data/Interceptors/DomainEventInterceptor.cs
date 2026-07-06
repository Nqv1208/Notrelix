using Notrelix.Infrastructure.Data.Events;
using Notrelix.Infrastructure.Data.Messaging;

namespace Notrelix.Infrastructure.Data.Interceptors;

public class DomainEventInterceptor : SaveChangesInterceptor
{
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IEventTypeRegistry _eventTypeRegistry;
    private readonly IIntegrationEventMapper _integrationEventMapper;

    public DomainEventInterceptor(
        IDateTimeProvider dateTimeProvider,
        IEventTypeRegistry eventTypeRegistry,
        IIntegrationEventMapper integrationEventMapper,
        IDomainEventDispatchPolicy dispatchPolicy)
    {
        _dateTimeProvider = dateTimeProvider;
        _eventTypeRegistry = eventTypeRegistry;
        _integrationEventMapper = integrationEventMapper;

        var inlineTypes = dispatchPolicy.GetInlineTypes();
        if (inlineTypes.Count > 0)
        {
            throw new InvalidOperationException(
                $"Inline domain event dispatch is no longer supported. " +
                $"The following {inlineTypes.Count} event type(s) are registered as Inline: " +
                $"{string.Join(", ", inlineTypes.Select(t => t.FullName))}. " +
                "All domain events must use Outbox dispatch.");
        }
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
                WriteOutboxEntries(context, domainEvent, messageName, now);
            }
        }

        foreach (var entry in entries)
        {
            entry.Entity.ClearDomainEvents();
        }
    }

    private void WriteOutboxEntries(ApplicationDbContext context, IDomainEvent domainEvent, string messageName, DateTimeOffset now)
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
}
