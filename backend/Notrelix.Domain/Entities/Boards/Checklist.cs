using Notrelix.Domain.Common;

namespace Notrelix.Domain.Entities.Boardss;

public class Checklist : BaseEntity
{
    public Guid CardId { get; private set; }
    public string Title { get; private set; } = "Checklist";
    public double Position { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Card Card { get; private set; } = null!;

    private readonly List<ChecklistItem> _items = new();
    public IReadOnlyCollection<ChecklistItem> Items => _items.AsReadOnly();

    private Checklist() : base() { }

    public static Checklist Create(Guid cardId, string title = "Checklist", double position = 0)
    {
        return new Checklist
        {
            CardId = cardId,
            Title = string.IsNullOrWhiteSpace(title) ? "Checklist" : title.Trim(),
            Position = position,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateTitle(string title) => Title = title.Trim();
    public void UpdatePosition(double position) => Position = position;
}
