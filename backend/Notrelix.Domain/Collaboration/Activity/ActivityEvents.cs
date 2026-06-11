using Notrelix.Domain.Common;

namespace Notrelix.Domain.Collaboration.Activity;

public record ActivityLoggedEvent(Guid LogId, Guid WorkspaceId, ActivityType Type) : DomainRecordEvent;
