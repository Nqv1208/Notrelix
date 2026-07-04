using Notrelix.Infrastructure.Data.Events;
using Notrelix.Infrastructure.Data.Messaging;

namespace Notrelix.Infrastructure.Data.Interceptors;

public class DomainEventInterceptor : SaveChangesInterceptor
{
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IEventTypeRegistry _eventTypeRegistry;
    private readonly IIntegrationEventMapper _integrationEventMapper;
    private readonly IMediator _mediator;
    private readonly IDomainEventDispatchPolicy _dispatchPolicy;
    private readonly AsyncLocal<List<IDomainEvent>?> _inlineEvents = new();

    public DomainEventInterceptor(
        IDateTimeProvider dateTimeProvider,
        IEventTypeRegistry eventTypeRegistry,
        IIntegrationEventMapper integrationEventMapper,
        IMediator mediator,
        IDomainEventDispatchPolicy dispatchPolicy)
    {
        _dateTimeProvider = dateTimeProvider;
        _eventTypeRegistry = eventTypeRegistry;
        _integrationEventMapper = integrationEventMapper;
        _mediator = mediator;
        _dispatchPolicy = dispatchPolicy;
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

    public override int SavedChanges(
        SaveChangesCompletedEventData eventData,
        int result)
    {
        var events = _inlineEvents.Value;
        _inlineEvents.Value = null;
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
        var events = _inlineEvents.Value;
        _inlineEvents.Value = null;
        if (events?.Count > 0)
        {
            await PublishInlineAsync(events, cancellationToken);
        }
        return result;
    }

    private void CaptureAndHandle(ApplicationDbContext? context)
    {
        if (context is null) return;

        var now = _dateTimeProvider.UtcNow;
        var inlineEvents = new List<IDomainEvent>();

        foreach (var entry in context.ChangeTracker
            .Entries<Entity>()
            .Where(e => e.Entity.DomainEvents.Any()))
        {
            foreach (var domainEvent in entry.Entity.DomainEvents)
            {
                switch (_dispatchPolicy.GetMode(domainEvent.GetType()))
                {
                    case DomainEventDispatchMode.Inline:
                        inlineEvents.Add(domainEvent);
                        break;

                    case DomainEventDispatchMode.Outbox:
                        var messageName = _eventTypeRegistry.GetMessageName(domainEvent.GetType());
                        WriteOutboxEntries(context, domainEvent, messageName, now);
                        break;

                    case DomainEventDispatchMode.Ignore:
                        break;
                }
            }
        }

        _inlineEvents.Value = inlineEvents;

        foreach (var entry in context.ChangeTracker
            .Entries<Entity>()
            .Where(e => e.Entity.DomainEvents.Any()))
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
