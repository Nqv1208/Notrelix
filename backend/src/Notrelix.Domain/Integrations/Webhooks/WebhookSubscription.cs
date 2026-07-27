using Notrelix.Domain.Integrations.Webhooks.Events;
namespace Notrelix.Domain.Integrations.Webhooks;

public class WebhookSubscription : SoftDeletableAggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Url TargetUrl { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public WebhookSecretHash? SecretHash { get; private set; }

    private WebhookSubscription() : base() { }

    public static WebhookSubscription Create(Guid accountId, Guid workspaceId, Url targetUrl, Guid createdBy, DateTimeOffset createdAt, WebhookSecretHash? secretHash = null)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotNull(targetUrl);
        Guard.NotEmpty(createdBy);

        var subscription = new WebhookSubscription
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            TargetUrl = targetUrl,
            IsActive = true,
            SecretHash = secretHash
        };

        subscription.SetAuditOnCreate(createdBy, createdAt);
        subscription.RaiseDomainEvent(new WebhookSubscriptionCreatedDomainEvent(accountId, subscription.Id, workspaceId, subscription.TargetUrl.Value, createdAt));

        return subscription;
    }

    public void Enable(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (IsActive) return;

        IsActive = true;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new WebhookSubscriptionEnabledDomainEvent(AccountId, Id, WorkspaceId, updatedAt));
    }

    public void Disable(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (!IsActive) return;

        IsActive = false;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new WebhookSubscriptionDisabledDomainEvent(AccountId, Id, WorkspaceId, updatedAt));
    }

    public void RotateSecret(WebhookSecretHash newHash, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(newHash);

        SecretHash = newHash;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new WebhookSubscriptionSecretRotatedDomainEvent(AccountId, Id, WorkspaceId, updatedAt));
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        IsActive = false;
        if (!MarkDeleted(deletedBy, deletedAt, reason)) return;
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
    }
}
