using MediatR;
using Notrelix.Application.Common.Events;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Application.Features.Calendar.Jobs;
using Notrelix.Domain.Enums;
using Notrelix.Domain.Events.Board;
using Notrelix.Domain.Events.Document;

namespace Notrelix.Application.Features.Calendar.Events;

public class CardDueDateCalendarHandler : INotificationHandler<DomainEventNotification<CardDueDateChangedEvent>>
{
    private readonly IJobQueue _jobQueue;

    public CardDueDateCalendarHandler(IJobQueue jobQueue)
    {
        _jobQueue = jobQueue;
    }

    public Task Handle(DomainEventNotification<CardDueDateChangedEvent> notification, CancellationToken cancellationToken)
    {
        return _jobQueue.EnqueueAsync(
            new CalendarSyncJob(ResourceType.Card, notification.DomainEvent.CardId),
            cancellationToken);
    }
}

public class PageDeadlineCalendarHandler : INotificationHandler<DomainEventNotification<PageDeadlineSetEvent>>
{
    private readonly IJobQueue _jobQueue;

    public PageDeadlineCalendarHandler(IJobQueue jobQueue)
    {
        _jobQueue = jobQueue;
    }

    public Task Handle(DomainEventNotification<PageDeadlineSetEvent> notification, CancellationToken cancellationToken)
    {
        return _jobQueue.EnqueueAsync(
            new CalendarSyncJob(ResourceType.Page, notification.DomainEvent.PageId),
            cancellationToken);
    }
}
