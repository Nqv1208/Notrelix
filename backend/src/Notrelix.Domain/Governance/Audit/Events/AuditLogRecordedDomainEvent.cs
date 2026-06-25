namespace Notrelix.Domain.Governance.Audit.Events;

public sealed record AuditLogRecordedDomainEvent(
    Guid AuditLogId,
    Guid WorkspaceId,
    string Action,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
