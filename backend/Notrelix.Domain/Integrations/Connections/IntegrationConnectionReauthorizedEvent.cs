using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Integrations.Connections;

public sealed record IntegrationConnectionReauthorizedEvent(
    Guid WorkspaceId,
    Guid ConnectionId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
