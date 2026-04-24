using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;

namespace Notrelix.Domain.Entities.Shared;

// Entity đại diện cho comment trên resource
public class Comment : AuditableEntity
{
    public Guid WorkspaceId { get; private set; }
    public ResourceType ResourceType { get; private set; }
    public Guid ResourceId { get; private set; }
    public Guid UserId { get; private set; }
    public string ContentMd { get; private set; } = null!;
    public Guid? ParentCommentId { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public Guid? ResolvedBy { get; private set; }
    public bool IsEdited { get; private set; }
    public DateTime? EditedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    // Navigation
    public Comment? ParentComment { get; private set; }

    private readonly List<Comment> _replies = new();
    public IReadOnlyCollection<Comment> Replies => _replies.AsReadOnly();

    private Comment() : base() { }

    public static Comment Create(
        Guid workspaceId,
        ResourceType resourceType,
        Guid resourceId,
        Guid userId,
        string contentMd)
    {
        if (string.IsNullOrWhiteSpace(contentMd))
            throw new ArgumentException("Nội dung comment không được để trống", nameof(contentMd));

        return new Comment
        {
            WorkspaceId = workspaceId,
            ResourceType = resourceType,
            ResourceId = resourceId,
            UserId = userId,
            ContentMd = contentMd.Trim(),
            IsEdited = false,
            IsDeleted = false
        };
    }

    public Comment Reply(Guid userId, string contentMd)
    {
        if (string.IsNullOrWhiteSpace(contentMd))
            throw new ArgumentException("Nội dung reply không được để trống", nameof(contentMd));

        var reply = new Comment
        {
            WorkspaceId = WorkspaceId,
            ResourceType = ResourceType,
            ResourceId = ResourceId,
            UserId = userId,
            ContentMd = contentMd.Trim(),
            ParentCommentId = Id,
            IsEdited = false,
            IsDeleted = false
        };

        _replies.Add(reply);
        return reply;
    }

    public void Edit(string newContent)
    {
        if (string.IsNullOrWhiteSpace(newContent))
            throw new ArgumentException("Nội dung comment không được để trống", nameof(newContent));

        ContentMd = newContent.Trim();
        IsEdited = true;
        EditedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        ContentMd = "[Đã xóa]";
    }

    public void Resolve(Guid resolvedBy)
    {
        ResolvedBy = resolvedBy;
        ResolvedAt = DateTime.UtcNow;
    }
}
