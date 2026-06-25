namespace Notrelix.Domain.Integrations.Webhooks;

public class WebhookSubscription : AggregateRoot, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public Url TargetUrl { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public WebhookSecretHash? SecretHash { get; private set; }

    private WebhookSubscription() : base() { }

    public static WebhookSubscription Create(Guid workspaceId, Url targetUrl, Guid createdBy, DateTimeOffset createdAt, WebhookSecretHash? secretHash = null)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNull(targetUrl);
        Guard.NotEmpty(createdBy);

        var subscription = new WebhookSubscription
        {
            WorkspaceId = workspaceId,
            TargetUrl = targetUrl,
            IsActive = true,
            SecretHash = secretHash
        };

        subscription.SetAuditOnCreate(createdBy, createdAt);
        subscription.AddDomainEvent(new WebhookSubscriptionCreatedDomainEvent(subscription.Id, workspaceId, subscription.TargetUrl.Value, createdAt));

        return subscription;
    }

    public void Enable(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (IsActive) return;

        IsActive = true;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
    }

    public void Disable(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (!IsActive) return;

        IsActive = false;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
    }

    public void RotateSecret(WebhookSecretHash newHash, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(newHash);

        SecretHash = newHash;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        IsActive = false;
        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
    }
}
