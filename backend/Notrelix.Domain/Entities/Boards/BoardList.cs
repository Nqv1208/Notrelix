using Notrelix.Domain.Common;

namespace Notrelix.Domain.Entities.Boards;

public class BoardList : AuditableEntity
{
    public const string DefaultColor = "#579bfc";

    public Guid BoardId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Color { get; private set; } = DefaultColor;
    public double Position { get; private set; }
    public bool IsArchived { get; private set; }

    public Board Board { get; private set; } = null!;

    private readonly List<Card> _cards = new();
    public IReadOnlyCollection<Card> Cards => _cards.AsReadOnly();

    private BoardList() : base() { }

    public static BoardList Create(Guid boardId, string title, double position = 0, string? color = null)
    {
        return new BoardList
        {
            BoardId = boardId,
            Title = title.Trim(),
            Color = NormalizeColor(color),
            Position = position
        };
    }

    public void UpdateTitle(string title)
    {
        Title = string.IsNullOrWhiteSpace(title) ? Title : title.Trim();
    }

    public void UpdateColor(string? color) => Color = NormalizeColor(color);
    public void UpdatePosition(double position) => Position = position;
    public void Archive() => IsArchived = true;
    public void Unarchive() => IsArchived = false;

    private static string NormalizeColor(string? color)
    {
        return string.IsNullOrWhiteSpace(color) ? DefaultColor : color.Trim();
    }
}
