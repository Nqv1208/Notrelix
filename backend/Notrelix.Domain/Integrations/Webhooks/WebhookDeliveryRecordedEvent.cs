using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Integrations.Webhooks;

public sealed record WebhookDeliveryRecordedEvent(
    Guid WorkspaceId,
    Guid SubscriptionId,
    Guid DeliveryId,
    WebhookDeliveryStatus Status,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
