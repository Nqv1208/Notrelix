using Notrelix.Domain.Common;

namespace Notrelix.Domain.Entities.Board;

public class Label : BaseEntity
{
    public Guid BoardId { get; private set; }
    public string? Name { get; private set; }
    public string Color { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    public Board Board { get; private set; } = null!;

    private Label() : base() { }

    public static Label Create(Guid boardId, string color, string? name = null)
    {
        return new Label
        {
            BoardId = boardId,
            Name = name?.Trim(),
            Color = color.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateName(string? name) => Name = name?.Trim();
    public void UpdateColor(string color) => Color = color.Trim();
}
