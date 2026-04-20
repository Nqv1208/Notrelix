using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;

namespace Notrelix.Domain.Entities.Board;

public class Board : AuditableEntity
{
    public Guid WorkspaceId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public string Background { get; private set; } = "{\"type\":\"color\",\"value\":\"#0079BF\"}";
    public BoardVisibility Visibility { get; private set; } = BoardVisibility.Workspace;
    public bool IsArchived { get; private set; }

    // Navigation
    public Workspace.Workspace Workspace { get; private set; } = null!;

    private readonly List<BoardMember> _members = new();
    public IReadOnlyCollection<BoardMember> Members => _members.AsReadOnly();

    private readonly List<BoardList> _lists = new();
    public IReadOnlyCollection<BoardList> Lists => _lists.AsReadOnly();

    private Board() : base() { }

    public static Board Create(Guid workspaceId, Guid createdBy, string title, BoardVisibility visibility = BoardVisibility.Workspace)
    {
        return new Board
        {
            WorkspaceId = workspaceId,
            CreatedByUserId = createdBy,
            Title = title.Trim(),
            Visibility = visibility
        };
    }

    public void UpdateTitle(string title)
    {
        Title = string.IsNullOrWhiteSpace(title) ? Title : title.Trim();
    }

    public void UpdateDescription(string? description)
    {
        Description = description?.Trim();
    }

    public void UpdateBackground(string background)
    {
        Background = string.IsNullOrWhiteSpace(background) ? Background : background;
    }

    public void UpdateVisibility(BoardVisibility visibility)
    {
        Visibility = visibility;
    }

    public void Archive() => IsArchived = true;
    public void Unarchive() => IsArchived = false;
}
