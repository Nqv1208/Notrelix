using Notrelix.Domain.Common;

namespace Notrelix.Domain.Integrations.Sync;

public record IntegrationSyncedEvent(Guid ConnectionId, string ResourceType) : DomainRecordEvent;
public record IntegrationSyncFailedEvent(Guid ConnectionId, string ResourceType, string Error) : DomainRecordEvent;
