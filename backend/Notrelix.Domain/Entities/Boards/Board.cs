using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Enums;
using Notrelix.Domain.Events.Board;
using Notrelix.Domain.Entities.Workspaces;

namespace Notrelix.Domain.Entities.Boards;

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
    public Workspace Workspace { get; private set; } = null!;

    private readonly List<BoardMember> _members = new();
    public IReadOnlyCollection<BoardMember> Members => _members.AsReadOnly();

    private readonly List<BoardList> _lists = new();
    public IReadOnlyCollection<BoardList> Lists => _lists.AsReadOnly();

    private Board() : base() { }

    public static Board Create(Guid workspaceId, Guid createdBy, string title, string? description, BoardVisibility visibility = BoardVisibility.Workspace)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Board title cannot be empty.", nameof(title));

        var board = new Board
        {
            WorkspaceId = workspaceId,
            CreatedByUserId = createdBy,
            Title = title.Trim(),
            Description = description?.Trim(),
            Visibility = visibility
        };

        board.AddDomainEvent(new BoardCreatedEvent(board.Id, workspaceId, createdBy, board.Title));
        return board;
    }

    public void UpdateTitle(string title)
    {
        Rename(title, Guid.Empty);
    }

    public void Rename(string title, Guid updatedBy)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Board title cannot be empty.", nameof(title));

        var normalizedTitle = title.Trim();
        if (Title == normalizedTitle) return;

        Title = normalizedTitle;
        AddDomainEvent(new BoardUpdatedEvent(Id, WorkspaceId, updatedBy, Title));
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

    public BoardList AddList(string title, double position)
    {
        var list = BoardList.Create(this.Id, title, position);
        _lists.Add(list);
        
        AddDomainEvent(new ListCreatedEvent(list.Id, this.Id, title));
        return list;
    }

    public void AddMember(Guid userId, BoardRole role = BoardRole.Member)
    {
        if (_members.Any(m => m.UserId == userId))
            throw new DomainException("User is already a member of this board.");

        var member = BoardMember.Create(this.Id, userId, role);
        _members.Add(member);
        
        AddDomainEvent(new BoardMemberAddedEvent(this.Id, userId, role));
    }

    public void RemoveMember(Guid userId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member == null)
            return;

        _members.Remove(member);
        
        AddDomainEvent(new BoardMemberRemovedEvent(this.Id, userId));
    }

    public void Archive()
    {
        Archive(Guid.Empty);
    }

    public void Archive(Guid archivedBy)
    {
        if (IsArchived) return;
        IsArchived = true;
        AddDomainEvent(new BoardArchivedEvent(Id, WorkspaceId, archivedBy));
    }

    public void Unarchive() => IsArchived = false;
}
