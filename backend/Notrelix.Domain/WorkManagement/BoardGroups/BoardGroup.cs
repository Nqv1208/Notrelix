using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.BoardGroups;

public class BoardGroup : AuditableEntity
{
    public Guid BoardId { get; private set; }
    public string Title { get; private set; } = null!;
    public Color Color { get; private set; } = null!;
    public FractionalIndex Position { get; private set; } = null!;
    public bool IsCollapsed { get; private set; }

    private BoardGroup() : base() { }

    public static BoardGroup Create(Guid boardId, string title, Color color, FractionalIndex position, Guid createdBy)
    {
        Guard.NotEmpty(boardId);
        Guard.NotEmpty(createdBy);
        Guard.NotNullOrWhiteSpace(title);
        Guard.NotNull(color);
        Guard.NotNull(position);

        var group = new BoardGroup
        {
            BoardId = boardId,
            Title = title.Trim(),
            Color = color,
            Position = position,
            IsCollapsed = false
        };

        group.SetAuditOnCreate(createdBy);
        group.AddDomainEvent(new BoardGroupCreatedEvent(boardId, group.Id, group.Title, createdBy));
        return group;
    }

    public void Rename(string title, Guid updatedBy)
    {
        Guard.NotNullOrWhiteSpace(title);

        var oldTitle = Title;
        var normalizedTitle = title.Trim();
        if (Title == normalizedTitle) return;

        Title = normalizedTitle;
        SetAuditOnUpdate(updatedBy);
        AddDomainEvent(new BoardGroupRenamedEvent(Id, BoardId, oldTitle, Title, updatedBy));
    }

    public void UpdateColor(Color color, Guid updatedBy)
    {
        Guard.NotNull(color);
        if (Color == color) return;

        Color = color;
        SetAuditOnUpdate(updatedBy);
    }

    public void UpdatePosition(FractionalIndex newPosition, Guid updatedBy)
    {
        Guard.NotNull(newPosition);
        if (Position == newPosition) return;

        Position = newPosition;
        SetAuditOnUpdate(updatedBy);
        AddDomainEvent(new BoardGroupReorderedEvent(Id, BoardId, newPosition.Value, updatedBy));
    }
}
