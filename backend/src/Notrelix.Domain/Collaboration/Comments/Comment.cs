namespace Notrelix.Domain.Collaboration.Comments;

public class Comment : AggregateRoot, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public ResourceRef Target { get; private set; } = null!;
    public Guid? ParentId { get; private set; }
    public string Content { get; private set; } = null!;
    public CommentAnchor Anchor { get; private set; } = null!;
    public CommentStatus CommentStatus { get; private set; }

    private Comment() : base() { }

    public static Comment Create(
        Guid workspaceId,
        ResourceRef target,
        string content,
        Guid createdBy,
        DateTimeOffset createdAt,
        Guid? parentId = null,
        CommentAnchor? anchor = null)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNull(target);
        Guard.NotNullOrWhiteSpace(content);
        Guard.NotEmpty(createdBy);

        if (target.WorkspaceId.HasValue && target.WorkspaceId.Value != workspaceId)
            throw new WorkspaceMismatchException(workspaceId, target.WorkspaceId.Value);

        var comment = new Comment
        {
            WorkspaceId = workspaceId,
            Target = target,
            ParentId = parentId,
            Content = content.Trim(),
            Anchor = anchor ?? CommentAnchor.None(),
            CommentStatus = CommentStatus.Active
        };

        comment.SetAuditOnCreate(createdBy, createdAt);
        comment.AddDomainEvent(new CommentCreatedDomainEvent(workspaceId, comment.Id, target, createdBy, createdAt));

        return comment;
    }

    public void UpdateContent(string newContent, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(newContent);

        if (Content == newContent.Trim()) return;

        Content = newContent.Trim();
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new CommentUpdatedDomainEvent(WorkspaceId, Id, updatedBy, updatedAt));
    }

    public void Resolve(Guid resolvedBy, DateTimeOffset resolvedAt)
    {
        EnsureNotDeleted();
        if (CommentStatus == CommentStatus.Resolved) return;

        CommentStatus = CommentStatus.Resolved;
        SetAuditOnUpdate(resolvedBy, resolvedAt);
        IncrementVersion();
        AddDomainEvent(new CommentResolvedDomainEvent(WorkspaceId, Id, resolvedBy, resolvedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        CommentStatus = CommentStatus.SoftDeleted;
        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        AddDomainEvent(new CommentSoftDeletedDomainEvent(WorkspaceId, Id, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        base.Restore(restoredBy, restoredAt);
        CommentStatus = CommentStatus.Active;
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        AddDomainEvent(new CommentRestoredDomainEvent(WorkspaceId, Id, restoredBy, restoredAt));
    }
}
