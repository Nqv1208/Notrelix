using Notrelix.Domain.Common;

namespace Notrelix.Domain.Integrations.Webhooks;

public record WebhookSubscriptionCreatedEvent(Guid WorkspaceId, Guid SubscriptionId, string TargetUrl) : DomainRecordEvent;
