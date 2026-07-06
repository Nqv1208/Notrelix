namespace Notrelix.Domain.WorkManagement.BoardGroups;

public class BoardGroup : SoftDeletableEntity, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid BoardId { get; private set; }
    public string Title { get; private set; } = null!;
    public Color Color { get; private set; } = null!;
    public FractionalIndex Position { get; private set; } = null!;
    public bool IsCollapsed { get; private set; }

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
        group.AddDomainEvent(new BoardGroupCreatedDomainEvent(accountId, workspaceId, boardId, group.Id, group.Title, createdBy, createdAt));
        return group;
    }

    public void Rename(string title, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(title);
        Guard.MaxLength(title, 255);

        var oldTitle = Title;
        var normalizedTitle = title.Trim();
        if (Title == normalizedTitle) return;

        Title = normalizedTitle;
        SetAuditOnUpdate(updatedBy, updatedAt);
        AddDomainEvent(new BoardGroupRenamedDomainEvent(AccountId, WorkspaceId, Id, BoardId, oldTitle, Title, updatedBy, updatedAt));
    }

    public void UpdateColor(Color color, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(color);
        if (Color == color) return;

        var oldColor = Color;
        Color = color;
        SetAuditOnUpdate(updatedBy, updatedAt);
        AddDomainEvent(new BoardGroupColorChangedDomainEvent(AccountId, WorkspaceId, BoardId, Id, oldColor, color, updatedBy, updatedAt));
    }

    public void UpdatePosition(FractionalIndex newPosition, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(newPosition);
        if (Position == newPosition) return;

        Position = newPosition;
        SetAuditOnUpdate(updatedBy, updatedAt);
        AddDomainEvent(new BoardGroupReorderedDomainEvent(AccountId, WorkspaceId, Id, BoardId, newPosition.Value, updatedBy, updatedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        AddDomainEvent(new BoardGroupSoftDeletedDomainEvent(AccountId, WorkspaceId, BoardId, Id, deletedBy, deletedAt));
    }

    public void ValidateNotDefaultGroup(Guid? defaultGroupId)
    {
        if (defaultGroupId.HasValue && Id == defaultGroupId.Value)
            throw new BusinessRuleException("Cannot delete the board's default group.");
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        base.Restore(restoredBy, restoredAt);
        AddDomainEvent(new BoardGroupRestoredDomainEvent(AccountId, WorkspaceId, BoardId, Id, restoredBy, restoredAt));
    }
}
