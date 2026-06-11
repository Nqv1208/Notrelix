using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.Collaboration.Comments;

public class Comment : SoftDeletableEntity
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
        Guid? parentId = null,
        CommentAnchor? anchor = null)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNull(target);
        Guard.NotNullOrWhiteSpace(content);
        Guard.NotEmpty(createdBy);

        var comment = new Comment
        {
            WorkspaceId = workspaceId,
            Target = target,
            ParentId = parentId,
            Content = content.Trim(),
            Anchor = anchor ?? CommentAnchor.None(),
            CommentStatus = CommentStatus.Active
        };

        comment.SetAuditOnCreate(createdBy);
        comment.AddDomainEvent(new CommentCreatedEvent(comment.Id, target, createdBy));

        return comment;
    }

    public void UpdateContent(string newContent, Guid updatedBy)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(newContent);

        if (Content == newContent.Trim()) return;

        Content = newContent.Trim();
        SetAuditOnUpdate(updatedBy);
        AddDomainEvent(new CommentUpdatedEvent(Id, updatedBy));
    }

    public void Resolve(Guid resolvedBy)
    {
        EnsureNotDeleted();
        if (CommentStatus == CommentStatus.Resolved) return;

        CommentStatus = CommentStatus.Resolved;
        SetAuditOnUpdate(resolvedBy);
        AddDomainEvent(new CommentResolvedEvent(Id, resolvedBy));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        CommentStatus = CommentStatus.SoftDeleted;
        base.SoftDelete(deletedBy, deletedAt, reason);
        AddDomainEvent(new CommentDeletedEvent(Id, deletedBy));
    }
}
