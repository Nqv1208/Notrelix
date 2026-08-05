namespace Notrelix.Domain.WorkManagement.Forms.Events;

[EventName("work-management.form-details-updated")]
public sealed record FormDetailsUpdatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid FormId,
    Guid BoardId,
    string Name,
    string SettingsJson,
    string SubmitterPolicyJson,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
