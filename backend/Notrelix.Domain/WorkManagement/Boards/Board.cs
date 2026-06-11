using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.WorkManagement.Boards;

public class Board : AggregateRoot
{
    public Guid WorkspaceId { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public string Background { get; private set; } = "{\"type\":\"color\",\"value\":\"#0079BF\"}";
    public BoardVisibility Visibility { get; private set; } = BoardVisibility.Workspace;
    public bool IsArchived { get; private set; }

    private Board() : base() { }

    public static Board Create(Guid workspaceId, Guid createdBy, string title, string? description, BoardVisibility visibility = BoardVisibility.Workspace)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(createdBy);
        Guard.NotNullOrWhiteSpace(title);

        var board = new Board
        {
            WorkspaceId = workspaceId,
            Title = title.Trim(),
            Description = description?.Trim(),
            Visibility = visibility,
            IsArchived = false
        };

        board.SetAuditOnCreate(createdBy);
        board.AddDomainEvent(new BoardCreatedEvent(workspaceId, board.Id, board.Title, createdBy));
        return board;
    }

    public void Rename(string title, Guid updatedBy)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(title);

        var oldTitle = Title;
        var normalizedTitle = title.Trim();
        if (Title == normalizedTitle) return;

        Title = normalizedTitle;
        SetAuditOnUpdate(updatedBy);
        AddDomainEvent(new BoardRenamedEvent(Id, oldTitle, Title, updatedBy));
    }

    public void UpdateDescription(string? description, Guid updatedBy)
    {
        EnsureNotDeleted();
        Description = description?.Trim();
        SetAuditOnUpdate(updatedBy);
    }

    public void UpdateBackground(string background, Guid updatedBy)
    {
        EnsureNotDeleted();
        Background = string.IsNullOrWhiteSpace(background) ? Background : background;
        SetAuditOnUpdate(updatedBy);
    }

    public void ChangeVisibility(BoardVisibility visibility, Guid updatedBy)
    {
        EnsureNotDeleted();
        var oldVisibility = Visibility;
        if (Visibility == visibility) return;

        Visibility = visibility;
        SetAuditOnUpdate(updatedBy);
        AddDomainEvent(new BoardVisibilityChangedEvent(Id, oldVisibility, Visibility, updatedBy));
    }

    public void Archive(Guid archivedBy)
    {
        EnsureNotDeleted();
        if (IsArchived) return;
        IsArchived = true;
        SetAuditOnUpdate(archivedBy);
        AddDomainEvent(new BoardArchivedEvent(Id, archivedBy));
    }

    public void Unarchive(Guid unarchivedBy)
    {
        EnsureNotDeleted();
        if (!IsArchived) return;
        IsArchived = false;
        SetAuditOnUpdate(unarchivedBy);
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        AddDomainEvent(new BoardSoftDeletedEvent(Id, deletedBy));
    }
}
