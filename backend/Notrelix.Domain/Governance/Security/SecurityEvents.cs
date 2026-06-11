using Notrelix.Domain.Common;

namespace Notrelix.Domain.Governance.Security;

public record SecurityEventRecordedEvent(Guid SecurityEventId, Guid WorkspaceId, SecurityEventType Type) : DomainRecordEvent;
