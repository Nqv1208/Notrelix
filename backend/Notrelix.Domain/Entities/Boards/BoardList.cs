using Notrelix.Domain.Common;
using Notrelix.Domain.Events.Board;

namespace Notrelix.Domain.Entities.Boards;

public class BoardList : AuditableEntity
{
    public const string DefaultColor = "#579bfc";

    public Guid BoardId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Color { get; private set; } = DefaultColor;
    public double Position { get; private set; }
    public bool IsArchived { get; private set; }
    public bool IsCollapsed { get; private set; }

    public Board Board { get; private set; } = null!;

    private readonly List<Card> _cards = new();
    public IReadOnlyCollection<Card> Cards => _cards.AsReadOnly();

    private BoardList() : base() { }

    public static BoardList Create(Guid boardId, string title, double position = 0, string? color = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Group title cannot be empty.", nameof(title));

        var list = new BoardList
        {
            BoardId = boardId,
            Title = title.Trim(),
            Color = NormalizeColor(color),
            Position = position
        };

        list.AddDomainEvent(new BoardGroupCreatedEvent(list.Id, boardId, list.Title));
        return list;
    }

    public void UpdateTitle(string title)
    {
        Rename(title, Guid.Empty);
    }

    public void Rename(string title, Guid updatedBy)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Group title cannot be empty.", nameof(title));

        var normalizedTitle = title.Trim();
        if (Title == normalizedTitle) return;

        Title = normalizedTitle;
        AddDomainEvent(new BoardGroupUpdatedEvent(Id, BoardId, updatedBy, Title));
    }

    public void UpdateColor(string? color) => ChangeColor(color, Guid.Empty);

    public void ChangeColor(string? color, Guid changedBy)
    {
        var normalizedColor = NormalizeColor(color);
        if (Color == normalizedColor) return;

        var oldColor = Color;
        Color = normalizedColor;
        AddDomainEvent(new BoardGroupColorChangedEvent(Id, BoardId, oldColor, Color, changedBy));
    }

    public void UpdatePosition(double position) => Move(position, Guid.Empty);

    public void Move(double position, Guid reorderedBy)
    {
        if (double.IsNaN(position) || double.IsInfinity(position))
            throw new ArgumentException("Position must be a finite number.", nameof(position));

        if (Position.Equals(position)) return;

        var oldPosition = Position;
        Position = position;
        AddDomainEvent(new BoardGroupReorderedEvent(Id, BoardId, oldPosition, Position, reorderedBy));
    }

    public void Collapse() => IsCollapsed = true;
    public void Expand() => IsCollapsed = false;

    public void Archive() => Archive(Guid.Empty);

    public void Archive(Guid archivedBy)
    {
        if (IsArchived) return;
        IsArchived = true;
        AddDomainEvent(new BoardGroupDeletedEvent(Id, BoardId, archivedBy));
    }

    public void Unarchive() => IsArchived = false;

    private static string NormalizeColor(string? color)
    {
        return string.IsNullOrWhiteSpace(color) ? DefaultColor : color.Trim();
    }
}
