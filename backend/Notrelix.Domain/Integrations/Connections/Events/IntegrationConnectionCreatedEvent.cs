using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Integrations.Connections.Events;

public sealed record IntegrationConnectionCreatedEvent(
    Guid WorkspaceId,
    Guid ConnectionId,
    IntegrationProvider Provider,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
