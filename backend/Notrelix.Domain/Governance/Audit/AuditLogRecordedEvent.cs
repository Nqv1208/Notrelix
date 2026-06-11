using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Governance.Audit;

public sealed record AuditLogRecordedEvent(
    Guid AuditLogId,
    Guid WorkspaceId,
    string Action,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
