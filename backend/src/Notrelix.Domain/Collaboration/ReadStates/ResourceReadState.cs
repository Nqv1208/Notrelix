using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Collaboration.ReadStates;

public class ResourceReadState : Entity, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public string ResourceType { get; private set; } = null!;
    public Guid ResourceId { get; private set; }
    public DateTimeOffset? LastReadAt { get; private set; }
    public Guid? LastReadCommentId { get; private set; }
    public int UnreadCount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private ResourceReadState() : base() { }

    public static ResourceReadState Create(
        Guid accountId,
        Guid workspaceId,
        Guid userId,
        ResourceRef target,
        DateTimeOffset createdAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(userId);

        return new ResourceReadState
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            UserId = userId,
            ResourceType = target.ResourceType.ToString(),
            ResourceId = target.ResourceId,
            UnreadCount = 0,
            UpdatedAt = createdAt,
            CreatedAt = createdAt
        };
    }

    public void MarkAsRead(DateTimeOffset readAt, Guid? lastCommentId = null)
    {
        LastReadAt = readAt;
        LastReadCommentId = lastCommentId;
        UnreadCount = 0;
        UpdatedAt = readAt;
    }

    public void IncrementUnread(DateTimeOffset now)
    {
        UnreadCount++;
        UpdatedAt = now;
    }
}
