using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.BoardGroups;

public class BoardGroup : SoftDeletableEntity
{
    public Guid WorkspaceId { get; private set; }
    public Guid BoardId { get; private set; }
    public string Title { get; private set; } = null!;
    public Color Color { get; private set; } = null!;
    public FractionalIndex Position { get; private set; } = null!;
    public bool IsCollapsed { get; private set; }

    private BoardGroup() : base() { }

    public static BoardGroup Create(Guid workspaceId, Guid boardId, string title, Color color, FractionalIndex position, Guid createdBy, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(boardId);
        Guard.NotEmpty(createdBy);
        Guard.NotNullOrWhiteSpace(title);
        Guard.NotNull(color);
        Guard.NotNull(position);

        var group = new BoardGroup
        {
            WorkspaceId = workspaceId,
            BoardId = boardId,
            Title = title.Trim(),
            Color = color,
            Position = position,
            IsCollapsed = false
        };

        group.SetAuditOnCreate(createdBy, createdAt);
        group.AddDomainEvent(new BoardGroupCreatedEvent(workspaceId, boardId, group.Id, group.Title, createdBy, createdAt));
        return group;
    }

    public void Rename(string title, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(title);

        var oldTitle = Title;
        var normalizedTitle = title.Trim();
        if (Title == normalizedTitle) return;

        Title = normalizedTitle;
        SetAuditOnUpdate(updatedBy, updatedAt);
        AddDomainEvent(new BoardGroupRenamedEvent(WorkspaceId, Id, BoardId, oldTitle, Title, updatedBy, updatedAt));
    }

    public void UpdateColor(Color color, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(color);
        if (Color == color) return;

        var oldColor = Color;
        Color = color;
        SetAuditOnUpdate(updatedBy, updatedAt);
        AddDomainEvent(new BoardGroupColorChangedEvent(WorkspaceId, BoardId, Id, oldColor, color, updatedBy, updatedAt));
    }

    public void UpdatePosition(FractionalIndex newPosition, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(newPosition);
        if (Position == newPosition) return;

        Position = newPosition;
        SetAuditOnUpdate(updatedBy, updatedAt);
        AddDomainEvent(new BoardGroupReorderedEvent(WorkspaceId, Id, BoardId, newPosition.Value, updatedBy, updatedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        AddDomainEvent(new BoardGroupSoftDeletedEvent(WorkspaceId, BoardId, Id, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        base.Restore(restoredBy, restoredAt);
        AddDomainEvent(new BoardGroupRestoredEvent(WorkspaceId, BoardId, Id, restoredBy, restoredAt));
    }
}
