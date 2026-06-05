using System.Text.Json;
using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;
using Notrelix.Domain.Events.Board;

namespace Notrelix.Domain.Entities.Boards;

/// <summary>
/// Saved board view. Existing per-user preference flow still uses UserId.
/// </summary>
public class BoardView : BaseEntity
{
    public Guid BoardId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string Name { get; private set; } = "Main table";
    public ViewMode ViewMode { get; private set; } = ViewMode.Kanban;
    public string Filters { get; private set; } = "{}";
    public string Config { get; private set; } = "{}";
    public double Position { get; private set; }
    public bool IsDefault { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation
    public Board Board { get; private set; } = null!;

    private BoardView() : base() { }

    public static BoardView Create(Guid boardId, Guid userId, ViewMode viewMode = ViewMode.Kanban)
    {
        return CreateSaved(
            boardId,
            userId,
            viewMode.ToString(),
            viewMode,
            1024,
            isDefault: viewMode == ViewMode.Kanban);
    }

    public static BoardView CreateSaved(
        Guid boardId,
        Guid createdByUserId,
        string name,
        ViewMode viewMode = ViewMode.Kanban,
        double position = 1024,
        bool isDefault = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Board view name cannot be empty.", nameof(name));

        var view = new BoardView
        {
            BoardId = boardId,
            UserId = createdByUserId,
            CreatedByUserId = createdByUserId,
            Name = name.Trim(),
            ViewMode = viewMode,
            Position = position,
            IsDefault = isDefault,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        view.AddDomainEvent(new BoardViewCreatedEvent(view.Id, boardId, createdByUserId, view.Name, viewMode));
        return view;
    }

    public void UpdateViewMode(ViewMode viewMode)
    {
        ViewMode = viewMode;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateFilters(string filters)
    {
        ValidateJson(filters);
        Filters = string.IsNullOrWhiteSpace(filters) ? "{}" : filters;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateConfig(string config)
    {
        ValidateJson(config);
        Config = string.IsNullOrWhiteSpace(config) ? "{}" : config;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Rename(string name, Guid updatedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Board view name cannot be empty.", nameof(name));

        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDefault(Guid changedBy)
    {
        IsDefault = true;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new BoardViewDefaultChangedEvent(Id, BoardId, changedBy));
    }

    public void ClearDefault()
    {
        IsDefault = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Move(double position, Guid reorderedBy)
    {
        if (double.IsNaN(position) || double.IsInfinity(position))
            throw new ArgumentException("Position must be a finite number.", nameof(position));

        if (Position.Equals(position)) return;

        var oldPosition = Position;
        Position = position;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new BoardViewReorderedEvent(Id, BoardId, oldPosition, Position, reorderedBy));
    }

    private static void ValidateJson(string value)
    {
        JsonDocument.Parse(string.IsNullOrWhiteSpace(value) ? "{}" : value);
    }
}
