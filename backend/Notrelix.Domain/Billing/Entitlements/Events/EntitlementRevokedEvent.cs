using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Billing.Entitlements.Events;

public sealed record EntitlementRevokedEvent(
    Guid WorkspaceId,
    string FeatureCode,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
