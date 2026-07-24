using Notrelix.Domain.WorkManagement.Boards.Events;
namespace Notrelix.Domain.WorkManagement.Boards;

public class Board : AggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
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
        Guid accountId,
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
        Guard.MaxLength(title, 255);
        Guard.MaxLength(description, 5000);
        Guard.MaxLength(itemKeyPrefix, 10);
        Guard.NotEmpty(accountId);

        var board = new Board
        {
            AccountId = accountId,
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
        board.RaiseDomainEvent(new BoardCreatedDomainEvent(accountId, workspaceId, board.Id, board.Title, createdBy, createdAt));
        return board;
    }

    public void Rename(string title, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(title);
        Guard.MaxLength(title, 255);

        if (IsArchived)
            throw new BusinessRuleException(BusinessRuleCodes.WorkManagement_Board_CannotRenameArchived, "Cannot rename an archived board.");

        var oldTitle = Title;
        var normalizedTitle = title.Trim();
        if (Title == normalizedTitle) return;

        Title = normalizedTitle;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new BoardRenamedDomainEvent(AccountId, WorkspaceId, Id, oldTitle, Title, updatedBy, updatedAt));
    }

    public void UpdateDescription(string? description, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.MaxLength(description, 5000);

        if (IsArchived)
            throw new BusinessRuleException(BusinessRuleCodes.WorkManagement_Board_CannotUpdateDescriptionArchived, "Cannot update description of an archived board.");
        var normalized = description?.Trim();
        if (Description == normalized) return;
        var oldDescription = Description;
        Description = normalized;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new BoardDescriptionUpdatedDomainEvent(AccountId, WorkspaceId, Id, oldDescription, Description, updatedBy, updatedAt));
    }

    public void UpdateBackground(string background, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (IsArchived)
            throw new BusinessRuleException(BusinessRuleCodes.WorkManagement_Board_CannotUpdateBackgroundArchived, "Cannot update background of an archived board.");
        if (string.IsNullOrWhiteSpace(background) || Background == background) return;
        var oldBackground = Background;
        Background = background;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new BoardBackgroundUpdatedDomainEvent(AccountId, WorkspaceId, Id, oldBackground, Background, updatedBy, updatedAt));
    }

    public void ChangeVisibility(BoardVisibility visibility, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();

        if (IsArchived)
            throw new BusinessRuleException(BusinessRuleCodes.WorkManagement_Board_CannotChangeVisibilityArchived, "Cannot change visibility of an archived board.");
        var oldVisibility = Visibility;
        if (Visibility == visibility) return;

        Visibility = visibility;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new BoardVisibilityChangedDomainEvent(AccountId, WorkspaceId, Id, oldVisibility, Visibility, updatedBy, updatedAt));
    }

    public void Archive(Guid archivedBy, DateTimeOffset archivedAt)
    {
        EnsureNotDeleted();
        if (IsArchived) return;
        IsArchived = true;
        SetAuditOnUpdate(archivedBy, archivedAt);
        IncrementVersion();
        RaiseDomainEvent(new BoardArchivedDomainEvent(AccountId, WorkspaceId, Id, archivedBy, archivedAt));
    }

    public void Unarchive(Guid unarchivedBy, DateTimeOffset unarchivedAt)
    {
        EnsureNotDeleted();
        if (!IsArchived) return;
        IsArchived = false;
        SetAuditOnUpdate(unarchivedBy, unarchivedAt);
        IncrementVersion();
        RaiseDomainEvent(new BoardUnarchivedDomainEvent(AccountId, WorkspaceId, Id, unarchivedBy, unarchivedAt));
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        if (!MarkDeleted(deletedBy, deletedAt, reason)) return;
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        RaiseDomainEvent(new BoardSoftDeletedDomainEvent(AccountId, WorkspaceId, Id, deletedBy, deletedAt));
    }

    public void SetDefaultGroup(Guid groupId, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (IsArchived)
            throw new BusinessRuleException(BusinessRuleCodes.WorkManagement_Board_CannotRenameArchived, "Cannot modify an archived board.");
        if (DefaultItemGroupId == groupId) return;
        DefaultItemGroupId = groupId;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new BoardDefaultGroupSetDomainEvent(AccountId, WorkspaceId, Id, groupId, updatedBy, updatedAt));
    }

    public (long Sequence, string Key) GenerateNextItemIdentity(Guid actorUserId, DateTimeOffset now)
    {
        EnsureNotDeleted();
        if (IsArchived)
            throw new BusinessRuleException(BusinessRuleCodes.WorkManagement_Board_CannotGenerateIdentityArchived, "Cannot generate item identity for an archived board.");
        ItemSequence++;
        var key = string.IsNullOrWhiteSpace(ItemKeyPrefix)
            ? ItemSequence.ToString()
            : $"{ItemKeyPrefix}-{ItemSequence}";
        SetAuditOnUpdate(actorUserId, now);
        IncrementVersion();
        RaiseDomainEvent(new BoardItemIdentityGeneratedDomainEvent(AccountId, WorkspaceId, Id, ItemSequence, key, actorUserId, now));
        return (ItemSequence, key);
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        if (!MarkRestored(restoredBy, restoredAt)) return;
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        RaiseDomainEvent(new BoardRestoredDomainEvent(AccountId, WorkspaceId, Id, restoredBy, restoredAt));
    }
}
