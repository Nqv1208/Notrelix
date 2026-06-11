using Notrelix.Domain.Common;

namespace Notrelix.Domain.Governance.Audit;

public record AuditLogRecordedEvent(Guid AuditLogId, Guid WorkspaceId, string Action) : DomainRecordEvent;
