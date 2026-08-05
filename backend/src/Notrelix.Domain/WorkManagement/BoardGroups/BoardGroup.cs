using Notrelix.Domain.WorkManagement.BoardGroups.Events;
namespace Notrelix.Domain.WorkManagement.BoardGroups;

public class BoardGroup : SoftDeletableAggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid BoardId { get; private set; }
    public string Title { get; private set; } = null!;
    public Color Color { get; private set; } = null!;
    public FractionalIndex Position { get; private set; } = null!;
    public bool IsCollapsed { get; private set; }
    public bool IsArchived { get; private set; }

    private BoardGroup() : base() { }

    public static BoardGroup Create(Guid accountId, Guid workspaceId, Guid boardId, string title, Color color, FractionalIndex position, Guid createdBy, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(boardId);
        Guard.NotEmpty(createdBy);
        Guard.NotNullOrWhiteSpace(title);
        Guard.MaxLength(title, 255);
        Guard.NotNull(color);
        Guard.NotNull(position);
        Guard.NotEmpty(accountId);

        var group = new BoardGroup
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            BoardId = boardId,
            Title = title.Trim(),
            Color = color,
            Position = position,
            IsCollapsed = false
        };

        group.SetAuditOnCreate(createdBy, createdAt);
        group.RaiseDomainEvent(new BoardGroupCreatedDomainEvent(accountId, workspaceId, boardId, group.Id, group.Title, createdBy, createdAt));
        return group;
    }

    public void Rename(string title, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        EnsureNotArchived();
        Guard.NotEmpty(updatedBy);
        Guard.NotNullOrWhiteSpace(title);
        Guard.MaxLength(title, 255);

        var normalizedTitle = title.Trim();
        if (Title == normalizedTitle) return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Title = normalizedTitle;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardGroupRenamedDomainEvent(AccountId, WorkspaceId, Id, BoardId, Title, normalizedTitle, updatedBy, updatedAt));
    }

    public void UpdateColor(Color color, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        EnsureNotArchived();
        Guard.NotEmpty(updatedBy);
        Guard.NotNull(color);
        if (Color == color) return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        var oldColor = Color;
        Color = color;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardGroupColorChangedDomainEvent(AccountId, WorkspaceId, BoardId, Id, oldColor, color, updatedBy, updatedAt));
    }

    public void UpdatePosition(FractionalIndex newPosition, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        EnsureNotArchived();
        Guard.NotEmpty(updatedBy);
        Guard.NotNull(newPosition);
        if (Position == newPosition) return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Position = newPosition;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardGroupReorderedDomainEvent(AccountId, WorkspaceId, Id, BoardId, newPosition.Value, updatedBy, updatedAt));
    }

    public void Delete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;
        var pendingDeletion = PrepareDeletion(deletedBy, deletedAt, reason);
        ApplyDeletion(pendingDeletion);
        IncrementVersion();
        RaiseDomainEvent(new BoardGroupDeletedDomainEvent(AccountId, WorkspaceId, BoardId, Id, deletedBy, deletedAt));
    }

    public void ValidateNotDefaultGroup(Guid? defaultGroupId)
    {
        if (defaultGroupId.HasValue && Id == defaultGroupId.Value)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Board_CannotDeleteDefaultGroup, "Cannot delete the board's default group.");
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;
        var pendingRestore = PrepareRestore(restoredBy, restoredAt);
        ApplyRestore(pendingRestore);
        IncrementVersion();
        RaiseDomainEvent(new BoardGroupRestoredDomainEvent(AccountId, WorkspaceId, BoardId, Id, restoredBy, restoredAt));
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
        RaiseDomainEvent(new BoardGroupArchivedDomainEvent(AccountId, WorkspaceId, Id, archivedBy, archivedAt));
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
        RaiseDomainEvent(new BoardGroupUnarchivedDomainEvent(AccountId, WorkspaceId, Id, unarchivedBy, unarchivedAt));
    }

    private void EnsureNotArchived()
    {
        if (IsArchived)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Board_CannotRenameArchived, "Cannot modify an archived board group.");
    }
}
