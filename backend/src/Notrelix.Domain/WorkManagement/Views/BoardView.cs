using Notrelix.Domain.WorkManagement.Views.Events;
namespace Notrelix.Domain.WorkManagement.Views;

public class BoardView : SoftDeletableAggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid BoardId { get; private set; }
    public string Name { get; private set; } = null!;
    public ViewType Type { get; private set; }
    public BoardViewConfig Config { get; private set; } = null!;
    public bool IsDefault { get; private set; }
    public BoardViewVisibility Visibility { get; private set; }
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
        bool isDefault = false,
        BoardViewVisibility visibility = BoardViewVisibility.Board)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(boardId);
        Guard.NotEmpty(createdBy);
        Guard.NotNullOrWhiteSpace(name);
        Guard.MaxLength(name, 255);
        Guard.NotNull(config);
        Guard.NotEmpty(accountId);

        if (isDefault && visibility == BoardViewVisibility.Private)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_View_PrivateCannotBeDefault, "A private view cannot be the Board's default view.");

        var view = new BoardView
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            BoardId = boardId,
            Name = name.Trim(),
            Type = type,
            Config = config,
            IsDefault = isDefault,
            Visibility = visibility
        };

        view.SetAuditOnCreate(createdBy, createdAt);
        view.RaiseDomainEvent(new BoardViewCreatedDomainEvent(accountId, workspaceId, boardId, view.Id, view.Name, type, createdBy, createdAt));

        return view;
    }

    public void UpdateConfig(BoardViewConfig config, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        Guard.NotNull(config);

        // A typed config belongs to its own layout. A plain config is the stored document of any view.
        var configLayout = config switch
        {
            KanbanViewConfig => ViewType.Kanban,
            TableViewConfig => ViewType.Table,
            CalendarViewConfig => ViewType.Calendar,
            TimelineViewConfig => ViewType.Timeline,
            _ => (ViewType?)null,
        };
        if (configLayout is { } layout && layout != Type)
        {
            var code = Type switch
            {
                ViewType.Kanban => WorkManagementRuleCodes.WorkManagement_View_KanbanMustUseKanbanConfig,
                ViewType.Calendar => WorkManagementRuleCodes.WorkManagement_View_CalendarMustUseCalendarConfig,
                ViewType.Timeline => WorkManagementRuleCodes.WorkManagement_View_TimelineMustUseTimelineConfig,
                _ => WorkManagementRuleCodes.WorkManagement_View_TableMustUseTableConfig,
            };
            throw new BusinessRuleException(code, $"A {Type} view must use a {Type} config");
        }

        if (Config == config) return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Config = config;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardViewConfigUpdatedDomainEvent(AccountId, WorkspaceId, Id, BoardId, updatedBy, updatedAt));
    }

    public void Rename(string name, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        Guard.NotNullOrWhiteSpace(name);
        Guard.MaxLength(name, 255);

        var normalizedName = name.Trim();
        if (Name == normalizedName) return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Name = normalizedName;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardViewRenamedDomainEvent(AccountId, WorkspaceId, Id, Name, normalizedName, updatedBy, updatedAt));
    }

    /// <summary>Who may edit the view: a Board view by its Editors (the permission decides), a Private view by its creator only.</summary>
    public bool CanBeEditedBy(Guid userId) => Visibility == BoardViewVisibility.Board || CreatedBy == userId;

    /// <summary>Whom the view is shown to: every Board member for a Board view, its creator for a Private one.</summary>
    public bool IsVisibleTo(Guid userId) => CanBeEditedBy(userId);

    public void SetVisibility(BoardViewVisibility visibility, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        if (Visibility == visibility) return;
        if (visibility == BoardViewVisibility.Private && IsDefault)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_View_PrivateCannotBeDefault, "The default view of a Board is shared with its members.");
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Visibility = visibility;
        ApplyAuditUpdate(pending);
        IncrementVersion();
    }

    public void SetDefault(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        if (Visibility == BoardViewVisibility.Private)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_View_PrivateCannotBeDefault, "A private view cannot be the Board's default view.");
        if (IsDefault) return;
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        IsDefault = true;
        ApplyAuditUpdate(pending);
        IncrementVersion();
    }

    public void ClearDefault(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        if (!IsDefault) return;
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        IsDefault = false;
        ApplyAuditUpdate(pending);
        IncrementVersion();
    }

    public void Delete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;
        var pendingDeletion = PrepareDeletion(deletedBy, deletedAt, reason);
        ApplyDeletion(pendingDeletion);
        IncrementVersion();
        RaiseDomainEvent(new BoardViewDeletedDomainEvent(AccountId, WorkspaceId, Id, BoardId, deletedBy, deletedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;
        var pendingRestore = PrepareRestore(restoredBy, restoredAt);
        ApplyRestore(pendingRestore);
        IncrementVersion();
        RaiseDomainEvent(new BoardViewRestoredDomainEvent(AccountId, WorkspaceId, Id, BoardId, restoredBy, restoredAt));
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
        RaiseDomainEvent(new BoardViewArchivedDomainEvent(AccountId, WorkspaceId, Id, BoardId, archivedBy, archivedAt));
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
        RaiseDomainEvent(new BoardViewUnarchivedDomainEvent(AccountId, WorkspaceId, Id, BoardId, unarchivedBy, unarchivedAt));
    }
}
