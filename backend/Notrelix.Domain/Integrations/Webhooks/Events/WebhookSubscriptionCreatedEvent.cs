using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Integrations.Webhooks.Events;

public sealed record WebhookSubscriptionCreatedEvent(
    Guid SubscriptionId,
    Guid WorkspaceId,
    string TargetUrl,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
