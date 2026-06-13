using Notrelix.Domain.Common;

namespace Notrelix.Domain.Integrations.Connections.Events;

public sealed record IntegrationSecretRotatedEvent(
    Guid WorkspaceId,
    Guid ConnectionId,
    string Version,
    Guid RotatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, RotatedBy);
