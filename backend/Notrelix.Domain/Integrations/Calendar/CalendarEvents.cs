using Notrelix.Domain.Common;

namespace Notrelix.Domain.Integrations.Calendar;

public record CalendarIntegrationConnectedEvent(Guid WorkspaceId, Guid ConnectionId) : DomainRecordEvent;
public record CalendarConflictDetectedEvent(Guid WorkspaceId, Guid IntegrationId) : DomainRecordEvent;
public record CalendarSyncedEvent(Guid WorkspaceId, Guid IntegrationId) : DomainRecordEvent;
