using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.WorkManagement.Boards;

public class Board : AggregateRoot, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public Guid? SpaceId { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public string Background { get; private set; } = "{\"type\":\"color\",\"value\":\"#0079BF\"}";
    public BoardVisibility Visibility { get; private set; } = BoardVisibility.Workspace;
    public BoardType BoardType { get; private set; } = BoardType.WorkManagement;
    public BoardFamily BoardFamily { get; private set; } = BoardFamily.Core;
    public string? ItemKeyPrefix { get; private set; }
    public long ItemSequence { get; private set; }
    public Guid? DefaultItemGroupId { get; private set; }
    public bool IsArchived { get; private set; }

    private Board() : base() { }

    public static Board Create(
        Guid workspaceId, 
        Guid createdBy, 
        string title, 
        string? description, 
        DateTimeOffset createdAt, 
        BoardVisibility visibility = BoardVisibility.Workspace,
        BoardType type = BoardType.WorkManagement,
        BoardFamily family = BoardFamily.Core,
        string? itemKeyPrefix = null,
        Guid? spaceId = null)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(createdBy);
        Guard.NotNullOrWhiteSpace(title);

        var board = new Board
        {
            WorkspaceId = workspaceId,
            SpaceId = spaceId,
            Title = title.Trim(),
            Description = description?.Trim(),
            Visibility = visibility,
            BoardType = type,
            BoardFamily = family,
            ItemKeyPrefix = itemKeyPrefix,
            ItemSequence = 0,
            IsArchived = false
        };

        board.SetAuditOnCreate(createdBy, createdAt);
        board.AddDomainEvent(new BoardCreatedEvent(workspaceId, board.Id, board.Title, createdBy, createdAt));
        return board;
    }

    public static Board Create(
        Guid workspaceId,
        Guid createdBy,
        string title,
        string? description,
        BoardVisibility visibility = BoardVisibility.Workspace)
    {
        return Create(workspaceId, createdBy, title, description, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), visibility);
    }

    public void Rename(string title, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(title);

        var oldTitle = Title;
        var normalizedTitle = title.Trim();
        if (Title == normalizedTitle) return;

        Title = normalizedTitle;
        SetAuditOnUpdate(updatedBy, updatedAt);
        AddDomainEvent(new BoardRenamedEvent(WorkspaceId, Id, oldTitle, Title, updatedBy, updatedAt));
    }

    public void UpdateDescription(string? description, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Description = description?.Trim();
        SetAuditOnUpdate(updatedBy, updatedAt);
    }

    public void UpdateBackground(string background, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Background = string.IsNullOrWhiteSpace(background) ? Background : background;
        SetAuditOnUpdate(updatedBy, updatedAt);
    }

    public void ChangeVisibility(BoardVisibility visibility, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        var oldVisibility = Visibility;
        if (Visibility == visibility) return;

        Visibility = visibility;
        SetAuditOnUpdate(updatedBy, updatedAt);
        AddDomainEvent(new BoardVisibilityChangedEvent(WorkspaceId, Id, oldVisibility, Visibility, updatedBy, updatedAt));
    }

    public void Archive(Guid archivedBy, DateTimeOffset archivedAt)
    {
        EnsureNotDeleted();
        if (IsArchived) return;
        IsArchived = true;
        SetAuditOnUpdate(archivedBy, archivedAt);
        AddDomainEvent(new BoardArchivedEvent(WorkspaceId, Id, archivedBy, archivedAt));
    }

    public void Unarchive(Guid unarchivedBy, DateTimeOffset unarchivedAt)
    {
        EnsureNotDeleted();
        if (!IsArchived) return;
        IsArchived = false;
        SetAuditOnUpdate(unarchivedBy, unarchivedAt);
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        AddDomainEvent(new BoardSoftDeletedEvent(WorkspaceId, Id, deletedBy, deletedAt));
    }

    public void SetDefaultGroup(Guid groupId, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        DefaultItemGroupId = groupId;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
    }

    public long GenerateNextItemSequence()
    {
        EnsureNotDeleted();
        ItemSequence++;
        IncrementVersion();
        return ItemSequence;
    }

    public string GenerateItemKey()
    {
        if (string.IsNullOrWhiteSpace(ItemKeyPrefix))
            return ItemSequence.ToString();
        return $"{ItemKeyPrefix}-{ItemSequence}";
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        base.Restore(restoredBy, restoredAt);
        AddDomainEvent(new BoardRestoredEvent(WorkspaceId, Id, restoredBy, restoredAt));
    }
}
