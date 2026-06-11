using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Integrations.Connections;

public sealed record IntegrationConnectionRevokedEvent(
    Guid WorkspaceId,
    Guid ConnectionId,
    Guid RevokedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
