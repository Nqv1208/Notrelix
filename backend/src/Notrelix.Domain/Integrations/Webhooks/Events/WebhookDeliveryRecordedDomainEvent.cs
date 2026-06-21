using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Integrations.Webhooks.Events;

public sealed record WebhookDeliveryRecordedDomainEvent(
    Guid WorkspaceId,
    Guid SubscriptionId,
    Guid DeliveryId,
    WebhookDeliveryStatus Status,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
