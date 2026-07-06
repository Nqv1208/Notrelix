namespace Notrelix.Domain.Collaboration.Comments;

public class Comment : AggregateRoot, IWorkspaceScoped
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
        Guid? parentId = null,
        CommentAnchor? anchor = null,
        Func<Guid, ResourceRef?>? getParentTarget = null)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotNull(target);
        Guard.NotNullOrWhiteSpace(content);
        Guard.MaxLength(content, 10000);
        Guard.NotEmpty(createdBy);

        if (target.WorkspaceId.HasValue && target.WorkspaceId.Value != workspaceId)
            throw new WorkspaceMismatchException(workspaceId, target.WorkspaceId.Value);

        if (parentId.HasValue && getParentTarget != null)
        {
            Rules.CommentRules.EnsureParentSameTarget(target, parentId, getParentTarget);
        }

        var comment = new Comment
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            Target = target,
            ParentId = parentId,
            Content = content.Trim(),
            Anchor = anchor ?? CommentAnchor.None(),
            CommentStatus = CommentStatus.Active
        };

        comment.SetAuditOnCreate(createdBy, createdAt);
        comment.AddDomainEvent(new CommentCreatedDomainEvent(accountId, workspaceId, comment.Id, target, createdBy, createdAt));

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
        AddDomainEvent(new CommentUpdatedDomainEvent(AccountId, WorkspaceId, Id, updatedBy, updatedAt));
    }

    public void Resolve(Guid resolvedBy, DateTimeOffset resolvedAt)
    {
        EnsureNotDeleted();
        if (CommentStatus == CommentStatus.Resolved) return;

        CommentStatus = CommentStatus.Resolved;
        SetAuditOnUpdate(resolvedBy, resolvedAt);
        IncrementVersion();
        AddDomainEvent(new CommentResolvedDomainEvent(AccountId, WorkspaceId, Id, resolvedBy, resolvedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        CommentStatus = CommentStatus.SoftDeleted;
        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        AddDomainEvent(new CommentSoftDeletedDomainEvent(AccountId, WorkspaceId, Id, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        base.Restore(restoredBy, restoredAt);
        CommentStatus = CommentStatus.Active;
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        AddDomainEvent(new CommentRestoredDomainEvent(AccountId, WorkspaceId, Id, restoredBy, restoredAt));
    }
}
