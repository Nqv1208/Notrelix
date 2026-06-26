namespace Notrelix.Domain.WorkManagement.Forms.Events;

public sealed record FormDetailsUpdatedDomainEvent(
    Guid WorkspaceId,
    Guid FormId,
    Guid BoardId,
    string Name,
    string SettingsJson,
    string SubmitterPolicyJson,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
