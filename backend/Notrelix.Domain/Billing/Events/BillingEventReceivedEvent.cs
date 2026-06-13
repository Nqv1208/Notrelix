using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Billing.Events;

public sealed record BillingEventReceivedEvent(
    Guid WorkspaceId,
    Guid BillingEventId,
    string ProviderEventId,
    BillingEventType Type,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
