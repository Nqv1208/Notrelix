namespace Notrelix.Application.Events.WorkManagement;

/// <summary>
/// Someone answered a published form of a Board. Collaboration consumes it to tell the people who manage the
/// form (D78.2: "new submission on a form I manage"). It carries no answer: a response can hold respondent data.
/// </summary>
[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("work-management.form-submission-created", Version = 1)]
public sealed record FormSubmissionCreatedIntegrationEvent(
    Guid EventId,
    Guid AccountIdValue,
    Guid WorkspaceIdValue,
    Guid SubmissionId,
    Guid FormId,
    Guid BoardId,
    Guid? SubmitterUserId,
    Guid CorrelationId,
    Guid? SourceEventId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default)
    : IntegrationEvent(
        EventId,
        "work-management.form-submission-created",
        1,
        CorrelationId,
        SourceEventId,
        AccountIdValue,
        WorkspaceIdValue,
        SubmitterUserId,
        CausationId,
        OccurredAt);
