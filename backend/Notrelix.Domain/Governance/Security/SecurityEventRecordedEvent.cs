using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Governance.Security;

public sealed record SecurityEventRecordedEvent(
    Guid SecurityEventId,
    Guid WorkspaceId,
    SecurityEventType Type,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
