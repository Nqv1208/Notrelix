using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Billing.Events;

public sealed record BillingEventProcessedEvent(
    Guid WorkspaceId,
    Guid BillingEventId,
    string ProviderEventId,
    BillingEventStatus Status,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
