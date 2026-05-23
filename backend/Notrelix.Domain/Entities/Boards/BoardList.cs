using Notrelix.Domain.Common;

namespace Notrelix.Domain.Entities.Boards;

public class BoardList : AuditableEntity
{
    public Guid BoardId { get; private set; }
    public string Title { get; private set; } = null!;
    public double Position { get; private set; }
    public bool IsArchived { get; private set; }

    public Board Board { get; private set; } = null!;

    private readonly List<Card> _cards = new();
    public IReadOnlyCollection<Card> Cards => _cards.AsReadOnly();

    private BoardList() : base() { }

    public static BoardList Create(Guid boardId, string title, double position = 0)
    {
        return new BoardList
        {
            BoardId = boardId,
            Title = title.Trim(),
            Position = position
        };
    }

    public void UpdateTitle(string title)
    {
        Title = string.IsNullOrWhiteSpace(title) ? Title : title.Trim();
    }

    public void UpdatePosition(double position) => Position = position;
    public void Archive() => IsArchived = true;
    public void Unarchive() => IsArchived = false;
}
