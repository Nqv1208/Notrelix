namespace Notrelix.Domain.WorkManagement.Views;

public class BoardView : AggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid BoardId { get; private set; }
    public string Name { get; private set; } = null!;
    public ViewType Type { get; private set; }
    public BoardViewConfig Config { get; private set; } = null!;
    public bool IsDefault { get; private set; }
    public bool IsArchived { get; private set; }

    private BoardView() : base() { }

    public static BoardView Create(
        Guid accountId,
        Guid workspaceId,
        Guid boardId,
        string name,
        ViewType type,
        BoardViewConfig config,
        Guid createdBy,
        DateTimeOffset createdAt,
        bool isDefault = false)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(boardId);
        Guard.NotEmpty(createdBy);
        Guard.NotNullOrWhiteSpace(name);
        Guard.MaxLength(name, 255);
        Guard.NotNull(config);
        Guard.NotEmpty(accountId);

        var view = new BoardView
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            BoardId = boardId,
            Name = name.Trim(),
            Type = type,
            Config = config,
            IsDefault = isDefault
        };

        view.SetAuditOnCreate(createdBy, createdAt);
        view.AddDomainEvent(new BoardViewCreatedDomainEvent(accountId, workspaceId, boardId, view.Id, view.Name, type, createdBy, createdAt));

        return view;
    }

    public void UpdateConfig(BoardViewConfig config, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(config);

        // Ensure the config type matches the view type
        if (Type == ViewType.Kanban && config is not KanbanViewConfig)
            throw new BusinessRuleException("Kanban view must use KanbanViewConfig");
        if (Type == ViewType.Table && config is not TableViewConfig)
            throw new BusinessRuleException("Table view must use TableViewConfig");
        if (Type == ViewType.Calendar && config is not CalendarViewConfig)
            throw new BusinessRuleException("Calendar view must use CalendarViewConfig");
        if (Type == ViewType.Timeline && config is not TimelineViewConfig)
            throw new BusinessRuleException("Timeline view must use TimelineViewConfig");

        if (Config == config) return;

        Config = config;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new BoardViewConfigUpdatedDomainEvent(AccountId, WorkspaceId, Id, BoardId, updatedBy, updatedAt));
    }

    public void Rename(string name, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(name);
        Guard.MaxLength(name, 255);

        var oldName = Name;
        var normalizedName = name.Trim();
        if (Name == normalizedName) return;

        Name = normalizedName;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new BoardViewRenamedDomainEvent(AccountId, WorkspaceId, Id, oldName, Name, updatedBy, updatedAt));
    }

    public void SetDefault(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (IsDefault) return;
        IsDefault = true;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
    }

    public void ClearDefault(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (!IsDefault) return;
        IsDefault = false;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;

        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        AddDomainEvent(new BoardViewDeletedDomainEvent(AccountId, WorkspaceId, Id, BoardId, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        base.Restore(restoredBy, restoredAt);
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        AddDomainEvent(new BoardViewRestoredDomainEvent(AccountId, WorkspaceId, Id, BoardId, restoredBy, restoredAt));
    }

    public void Archive(Guid archivedBy, DateTimeOffset archivedAt)
    {
        EnsureNotDeleted();
        if (IsArchived) return;
        IsArchived = true;
        SetAuditOnUpdate(archivedBy, archivedAt);
        IncrementVersion();
        AddDomainEvent(new BoardViewArchivedDomainEvent(AccountId, WorkspaceId, Id, BoardId, archivedBy, archivedAt));
    }

    public void Unarchive(Guid unarchivedBy, DateTimeOffset unarchivedAt)
    {
        EnsureNotDeleted();
        if (!IsArchived) return;
        IsArchived = false;
        SetAuditOnUpdate(unarchivedBy, unarchivedAt);
        IncrementVersion();
        AddDomainEvent(new BoardViewUnarchivedDomainEvent(AccountId, WorkspaceId, Id, BoardId, unarchivedBy, unarchivedAt));
    }
}
