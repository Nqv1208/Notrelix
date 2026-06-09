using Notrelix.Domain.Common;
using Notrelix.Domain.Entities.Workspaces;

namespace Notrelix.Domain.Entities.Extensibility;

public class WebhookSubscription : AuditableEntity
{
    public Guid WorkspaceId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public string TargetUrl { get; private set; } = string.Empty;
    public string SecretHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    public Workspace Workspace { get; private set; } = null!;

    private WebhookSubscription() { }

    public static WebhookSubscription Create(
        Guid workspaceId,
        Guid createdByUserId,
        string name,
        string eventType,
        string targetUrl,
        string secretHash)
    {
        return new WebhookSubscription
        {
            WorkspaceId = workspaceId,
            CreatedByUserId = createdByUserId,
            Name = string.IsNullOrWhiteSpace(name) ? "Webhook" : name.Trim(),
            EventType = string.IsNullOrWhiteSpace(eventType) ? throw new ArgumentException("Event type is required.", nameof(eventType)) : eventType.Trim().ToLowerInvariant(),
            TargetUrl = string.IsNullOrWhiteSpace(targetUrl) ? throw new ArgumentException("Target URL is required.", nameof(targetUrl)) : targetUrl.Trim(),
            SecretHash = string.IsNullOrWhiteSpace(secretHash) ? throw new ArgumentException("Secret hash is required.", nameof(secretHash)) : secretHash,
            CreatedBy = createdByUserId
        };
    }

    public void Activate(Guid updatedBy)
    {
        IsActive = true;
        UpdatedBy = updatedBy;
    }

    public void Deactivate(Guid updatedBy)
    {
        IsActive = false;
        UpdatedBy = updatedBy;
    }
}
