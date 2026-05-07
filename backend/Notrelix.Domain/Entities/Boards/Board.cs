
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

    public static Board Create(Guid workspaceId, Guid createdBy, string title, string description, BoardVisibility visibility = BoardVisibility.Workspace)
    {
        return new Board
        {
            WorkspaceId = workspaceId,
            CreatedByUserId = createdBy,
            Title = title.Trim(),
            Description = description,
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

    public BoardList AddList(string title, double position)
    {
        var list = BoardList.Create(this.Id, title, position);
        _lists.Add(list);
        
        AddDomainEvent(new Events.Board.ListCreatedEvent(list.Id, this.Id, title));
        return list;
    }

    public void AddMember(Guid userId, BoardRole role = BoardRole.Member)
    {
        if (_members.Any(m => m.UserId == userId))
            throw new DomainException("User is already a member of this board.");

        var member = BoardMember.Create(this.Id, userId, role);
        _members.Add(member);
        
        AddDomainEvent(new Events.Board.BoardMemberAddedEvent(this.Id, userId, role));
    }

    public void RemoveMember(Guid userId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member == null)
            return;

        _members.Remove(member);
        
        AddDomainEvent(new Events.Board.BoardMemberRemovedEvent(this.Id, userId));
    }

    public void Archive() => IsArchived = true;
    public void Unarchive() => IsArchived = false;
}
