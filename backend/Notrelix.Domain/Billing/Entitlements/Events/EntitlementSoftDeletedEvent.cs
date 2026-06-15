using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Entitlements.Events;

public sealed record EntitlementSoftDeletedEvent(
    Guid WorkspaceId,
    Guid EntitlementId,
    string FeatureCode,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, DeletedBy);
