namespace Notrelix.Domain.WorkManagement.Approvals.Events;

[EventName("work-management.approval-request-rejected")]
public sealed record ApprovalRequestRejectedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RequestId,
    Guid DecidedBy,
    string? Note,
    DateTimeOffset OccurredAt,
    // Additive: who asked and what the request is about, so the requester can be told.
    Guid? RequestedBy = null,
    ResourceRef? Target = null
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
