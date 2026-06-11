using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.WorkManagement.Views;

public class BoardView : Entity
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

        view.AddDomainEvent(new BoardViewCreatedEvent(boardId, view.Id, view.Name, type, createdBy));
        
        return view;
    }

    public void UpdateConfig(BoardViewConfig config, Guid updatedBy)
    {
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
        AddDomainEvent(new BoardViewConfigUpdatedEvent(Id, BoardId, updatedBy));
    }
}
