using Notrelix.Domain.Collaboration.Comments.Events;
namespace Notrelix.Domain.Collaboration.Comments;

public class Comment : SoftDeletableAggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public ResourceRef Target { get; private set; } = null!;
    public Guid? ParentId { get; private set; }
    public string Content { get; private set; } = null!;
    public CommentAnchor Anchor { get; private set; } = null!;
    public CommentStatus CommentStatus { get; private set; }

    private Comment() : base() { }

    public static Comment Create(
        Guid accountId,
        Guid workspaceId,
        ResourceRef target,
        string content,
        Guid createdBy,
        DateTimeOffset createdAt,
        CommentAnchor? anchor = null)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotNull(target);
        Guard.NotNullOrWhiteSpace(content);
        Guard.MaxLength(content, 10000);
        Guard.NotEmpty(createdBy);

        if (target.WorkspaceId.HasValue && target.WorkspaceId.Value != workspaceId)
            throw new BusinessRuleException(CommonRuleCodes.Common_WorkspaceScopeMismatch, $"Workspace scope mismatch. Expected '{workspaceId}', got '{target.WorkspaceId.Value}'.");

        var comment = new Comment
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            Target = target,
            ParentId = null,
            Content = content.Trim(),
            Anchor = anchor ?? CommentAnchor.None(),
            CommentStatus = CommentStatus.Active
        };

        comment.SetAuditOnCreate(createdBy, createdAt);
        comment.RaiseDomainEvent(new CommentCreatedDomainEvent(accountId, workspaceId, comment.Id, target, createdBy, createdAt));

        return comment;
    }

    public static Comment CreateReply(
        Guid accountId,
        Guid workspaceId,
        ResourceRef target,
        string content,
        Guid createdBy,
        DateTimeOffset createdAt,
        ParentCommentContext parentContext,
        CommentAnchor? anchor = null)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotNull(target);
        Guard.NotNullOrWhiteSpace(content);
        Guard.MaxLength(content, 10000);
        Guard.NotEmpty(createdBy);
        Guard.NotNull(parentContext);

        if (target.WorkspaceId.HasValue && target.WorkspaceId.Value != workspaceId)
            throw new BusinessRuleException(CommonRuleCodes.Common_WorkspaceScopeMismatch, $"Workspace scope mismatch. Expected '{workspaceId}', got '{target.WorkspaceId.Value}'.");

        // Validate tenant scope match
        if (parentContext.AccountId != accountId)
            throw new BusinessRuleException(CollaborationRuleCodes.Collaboration_Comment_ParentScopeMismatch, "Parent comment must belong to the same account.");
        if (parentContext.WorkspaceId != workspaceId)
            throw new BusinessRuleException(CollaborationRuleCodes.Collaboration_Comment_ParentScopeMismatch, "Parent comment must belong to the same workspace.");

        if (parentContext.ParentTarget.ResourceType != target.ResourceType || parentContext.ParentTarget.ResourceId != target.ResourceId)
            throw new BusinessRuleException(CollaborationRuleCodes.Collaboration_Comment_ParentMustBeInSameTarget, "Parent comment must belong to the same target resource.");

        // Validate parent is not deleted
        if (parentContext.IsDeleted)
            throw new BusinessRuleException(CollaborationRuleCodes.Collaboration_Comment_CannotReplyToDeleted, "Cannot reply to a deleted comment.");

        var comment = new Comment
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            Target = target,
            ParentId = parentContext.ParentCommentId,
            Content = content.Trim(),
            Anchor = anchor ?? CommentAnchor.None(),
            CommentStatus = CommentStatus.Active
        };

        comment.SetAuditOnCreate(createdBy, createdAt);
        comment.RaiseDomainEvent(new CommentReplyCreatedDomainEvent(
            accountId, workspaceId, comment.Id, parentContext.ParentCommentId, target, createdBy, createdAt));

        return comment;
    }

    public void UpdateContent(string newContent, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(newContent);
        Guard.MaxLength(newContent, 10000);

        if (Content == newContent.Trim()) return;

        Content = newContent.Trim();
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new CommentUpdatedDomainEvent(AccountId, WorkspaceId, Id, updatedBy, updatedAt));
    }

    public void Resolve(Guid resolvedBy, DateTimeOffset resolvedAt)
    {
        EnsureNotDeleted();
        if (CommentStatus == CommentStatus.Resolved) return;

        CommentStatus = CommentStatus.Resolved;
        SetAuditOnUpdate(resolvedBy, resolvedAt);
        IncrementVersion();
        RaiseDomainEvent(new CommentResolvedDomainEvent(AccountId, WorkspaceId, Id, resolvedBy, resolvedAt));
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        CommentStatus = CommentStatus.SoftDeleted;
        if (!MarkDeleted(deletedBy, deletedAt, reason)) return;
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        RaiseDomainEvent(new CommentSoftDeletedDomainEvent(AccountId, WorkspaceId, Id, deletedBy, deletedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        if (!MarkRestored(restoredBy, restoredAt)) return;
        CommentStatus = CommentStatus.Active;
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        RaiseDomainEvent(new CommentRestoredDomainEvent(AccountId, WorkspaceId, Id, restoredBy, restoredAt));
    }
}
