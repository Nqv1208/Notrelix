using Notrelix.Domain.WorkManagement.Boards.Events;
namespace Notrelix.Domain.WorkManagement.Boards;

public class Board : SoftDeletableAggregateRoot, IWorkspaceScoped
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
        Guard.NotEmpty(updatedBy);
        Guard.NotNullOrWhiteSpace(title);
        Guard.MaxLength(title, 255);

        if (IsArchived)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Board_CannotRenameArchived, "Cannot rename an archived board.");

        var normalizedTitle = title.Trim();
        if (Title == normalizedTitle) return;

        var oldTitle = Title;
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Title = normalizedTitle;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardRenamedDomainEvent(AccountId, WorkspaceId, Id, oldTitle, Title, updatedBy, updatedAt));
    }

    public void UpdateDescription(string? description, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        Guard.MaxLength(description, 5000);

        if (IsArchived)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Board_CannotUpdateDescriptionArchived, "Cannot update description of an archived board.");
        var normalized = description?.Trim();
        if (Description == normalized) return;
        var oldDescription = Description;
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Description = normalized;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardDescriptionUpdatedDomainEvent(AccountId, WorkspaceId, Id, oldDescription, Description, updatedBy, updatedAt));
    }

    public void UpdateBackground(string background, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        if (IsArchived)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Board_CannotUpdateBackgroundArchived, "Cannot update background of an archived board.");
        if (string.IsNullOrWhiteSpace(background) || Background == background) return;
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        var oldBackground = Background;
        Background = background;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardBackgroundUpdatedDomainEvent(AccountId, WorkspaceId, Id, oldBackground, Background, updatedBy, updatedAt));
    }

    public void ChangeVisibility(BoardVisibility visibility, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);

        if (IsArchived)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Board_CannotChangeVisibilityArchived, "Cannot change visibility of an archived board.");
        if (Visibility == visibility) return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        var oldVisibility = Visibility;
        Visibility = visibility;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardVisibilityChangedDomainEvent(AccountId, WorkspaceId, Id, oldVisibility, Visibility, updatedBy, updatedAt));
    }

    public void Archive(Guid archivedBy, DateTimeOffset archivedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(archivedBy);
        if (IsArchived) return;
        var pending = PrepareAuditUpdate(archivedBy, archivedAt);
        IsArchived = true;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardArchivedDomainEvent(AccountId, WorkspaceId, Id, archivedBy, archivedAt));
    }

    public void Unarchive(Guid unarchivedBy, DateTimeOffset unarchivedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(unarchivedBy);
        if (!IsArchived) return;
        var pending = PrepareAuditUpdate(unarchivedBy, unarchivedAt);
        IsArchived = false;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardUnarchivedDomainEvent(AccountId, WorkspaceId, Id, unarchivedBy, unarchivedAt));
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;
        var pendingDeletion = PrepareDeletion(deletedBy, deletedAt, reason);
        ApplyDeletion(pendingDeletion);
        IncrementVersion();
        RaiseDomainEvent(new BoardSoftDeletedDomainEvent(AccountId, WorkspaceId, Id, deletedBy, deletedAt));
    }

    public void SetDefaultGroup(Guid groupId, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        if (IsArchived)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Board_CannotRenameArchived, "Cannot modify an archived board.");
        if (DefaultItemGroupId == groupId) return;
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        DefaultItemGroupId = groupId;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardDefaultGroupSetDomainEvent(AccountId, WorkspaceId, Id, groupId, updatedBy, updatedAt));
    }

    public (long Sequence, string Key) GenerateNextItemIdentity(Guid actorUserId, DateTimeOffset now)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(actorUserId);
        if (IsArchived)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Board_CannotGenerateIdentityArchived, "Cannot generate item identity for an archived board.");
        var pending = PrepareAuditUpdate(actorUserId, now);
        ItemSequence++;
        var key = string.IsNullOrWhiteSpace(ItemKeyPrefix)
            ? ItemSequence.ToString()
            : $"{ItemKeyPrefix}-{ItemSequence}";
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardItemIdentityGeneratedDomainEvent(AccountId, WorkspaceId, Id, ItemSequence, key, actorUserId, now));
        return (ItemSequence, key);
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;
        var pendingRestore = PrepareRestore(restoredBy, restoredAt);
        ApplyRestore(pendingRestore);
        IncrementVersion();
        RaiseDomainEvent(new BoardRestoredDomainEvent(AccountId, WorkspaceId, Id, restoredBy, restoredAt));
    }
}
