using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.WorkManagement.Views;

public class BoardView : AggregateRoot, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public Guid BoardId { get; private set; }
    public string Name { get; private set; } = null!;
    public ViewType Type { get; private set; }
    public BoardViewConfig Config { get; private set; } = null!;
    public bool IsDefault { get; private set; }

    private BoardView() : base() { }

    public static BoardView Create(
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
        Guard.NotNull(config);

        var view = new BoardView
        {
            WorkspaceId = workspaceId,
            BoardId = boardId,
            Name = name.Trim(),
            Type = type,
            Config = config,
            IsDefault = isDefault
        };

        view.SetAuditOnCreate(createdBy, createdAt);
        view.AddDomainEvent(new BoardViewCreatedEvent(workspaceId, boardId, view.Id, view.Name, type, createdBy, createdAt));
        
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
        AddDomainEvent(new BoardViewConfigUpdatedEvent(WorkspaceId, Id, BoardId, updatedBy, updatedAt));
    }

    public void Rename(string name, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(name);

        var oldName = Name;
        var normalizedName = name.Trim();
        if (Name == normalizedName) return;

        Name = normalizedName;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new BoardViewRenamedEvent(WorkspaceId, Id, oldName, Name, updatedBy, updatedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;

        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        AddDomainEvent(new BoardViewDeletedEvent(WorkspaceId, Id, BoardId, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        base.Restore(restoredBy, restoredAt);
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        AddDomainEvent(new BoardViewRestoredEvent(WorkspaceId, Id, BoardId, restoredBy, restoredAt));
    }
}
