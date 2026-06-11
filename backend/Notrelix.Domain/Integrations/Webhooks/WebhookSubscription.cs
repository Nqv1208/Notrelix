using Notrelix.Domain.Common;

namespace Notrelix.Domain.Integrations.Webhooks;

public class WebhookSubscription : AggregateRoot
{
    public Guid WorkspaceId { get; private set; }
    public string TargetUrl { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public WebhookSecretHash? SecretHash { get; private set; }

    private WebhookSubscription() : base() { }

    public static WebhookSubscription Create(Guid workspaceId, string targetUrl, Guid createdBy, DateTimeOffset createdAt, WebhookSecretHash? secretHash = null)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNullOrWhiteSpace(targetUrl);

        var subscription = new WebhookSubscription
        {
            WorkspaceId = workspaceId,
            TargetUrl = targetUrl.Trim(),
            IsActive = true,
            SecretHash = secretHash
        };

        subscription.SetAuditOnCreate(createdBy, createdAt);
        subscription.AddDomainEvent(new WebhookSubscriptionCreatedEvent(subscription.Id, workspaceId, subscription.TargetUrl, createdAt));

        return subscription;
    }
}
