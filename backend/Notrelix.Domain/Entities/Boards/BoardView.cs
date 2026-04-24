
namespace Notrelix.Domain.Entities.Boardss;

/// <summary>
/// View preference per user per board (composite PK: BoardId + UserId)
/// User A chọn calendar view không ảnh hưởng đến User B đang xem kanban
/// </summary>
public class BoardView
{
    public Guid BoardId { get; private set; }
    public Guid UserId { get; private set; }
    public ViewMode ViewMode { get; private set; } = ViewMode.Kanban;
    public string Filters { get; private set; } = "{}";
    public DateTime UpdatedAt { get; private set; }

    // Navigation
    public Board Board { get; private set; } = null!;

    private BoardView() { }

    public static BoardView Create(Guid boardId, Guid userId, ViewMode viewMode = ViewMode.Kanban)
    {
        return new BoardView
        {
            BoardId = boardId,
            UserId = userId,
            ViewMode = viewMode,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateViewMode(ViewMode viewMode)
    {
        ViewMode = viewMode;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateFilters(string filters)
    {
        Filters = string.IsNullOrWhiteSpace(filters) ? "{}" : filters;
        UpdatedAt = DateTime.UtcNow;
    }
}
