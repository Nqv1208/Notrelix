using Notrelix.Domain.Billing.BillingEvents;
using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Billing.BillingEvents.Events;

public sealed record BillingEventProcessedDomainEvent(
    Guid WorkspaceId,
    Guid BillingEventId,
    string ProviderEventId,
    BillingEventStatus Status,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
