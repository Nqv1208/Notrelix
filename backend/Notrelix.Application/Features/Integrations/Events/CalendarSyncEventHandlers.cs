// Calendar sync event handlers are temporarily disabled as the domain events
// BoardItemDueDateChangedEvent and PageDeadlineSetEvent do not exist in the current Domain model.
// Handlers will be re-implemented when calendar sync integration is built out.

// using MediatR;
// using Notrelix.Application.Common.Events;
// using Notrelix.Application.Common.Abstractions;
// using Notrelix.Application.Features.Integrations.Jobs;
// using Notrelix.Domain.WorkManagement.Items;
// using Notrelix.Domain.Documents.Events;
// 
// namespace Notrelix.Application.Features.Integrations.Events;
// 
// public class CardDueDateCalendarHandler : INotificationHandler<DomainEventNotification<BoardItemDueDateChangedEvent>>
// {
//     private readonly IJobQueue _jobQueue;
//     ...
// }
