using Notrelix.Application.Events.WorkManagement;
using Notrelix.Domain.WorkManagement.Forms.Events;

namespace Notrelix.Application.EventMappers.WorkManagement;

/// <summary>
/// Maps the Work-owned "a form was answered" Domain fact to its producer-owned outward contract; Collaboration
/// consumes it to tell the people who manage the form in their Inbox.
/// </summary>
public sealed class FormSubmissionEventMapper
    : IntegrationEventMapperBase<FormSubmissionCreatedDomainEvent, FormSubmissionCreatedIntegrationEvent>
{
    public override FormSubmissionCreatedIntegrationEvent Map(FormSubmissionCreatedDomainEvent domainEvent) =>
        new(
            EventId: Guid.CreateVersion7(),
            AccountIdValue: domainEvent.AccountId,
            WorkspaceIdValue: domainEvent.WorkspaceId,
            SubmissionId: domainEvent.SubmissionId,
            FormId: domainEvent.FormId,
            BoardId: domainEvent.BoardId,
            SubmitterUserId: domainEvent.SubmitterUserId,
            CorrelationId: domainEvent.EventId,
            SourceEventId: domainEvent.EventId,
            OccurredAt: domainEvent.OccurredAt);
}
