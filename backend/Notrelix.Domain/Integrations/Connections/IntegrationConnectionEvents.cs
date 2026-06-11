using Notrelix.Domain.Common;

namespace Notrelix.Domain.Integrations.Connections;

public record IntegrationConnectionCreatedEvent(Guid WorkspaceId, Guid ConnectionId, IntegrationProvider Provider, Guid CreatedBy) : DomainRecordEvent;
public record IntegrationConnectionRevokedEvent(Guid ConnectionId, Guid RevokedBy) : DomainRecordEvent;
public record IntegrationConnectionReauthorizedEvent(Guid ConnectionId, Guid UpdatedBy) : DomainRecordEvent;
