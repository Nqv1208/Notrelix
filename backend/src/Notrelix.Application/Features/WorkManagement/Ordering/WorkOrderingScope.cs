namespace Notrelix.Application.Features.WorkManagement.Ordering;

/// <summary>
/// The P3 ordering scopes (SPEC WMREQ098). Each scope is a sibling collection
/// under one parent; positions are unique and serialized per (kind, parent).
/// </summary>
public enum WorkOrderingScopeKind
{
    BoardGroups,
    BoardFields,
    GroupItems,
    ChildItems,
    FieldOptions,
    ItemChecklists,
    ChecklistItems,
}

/// <summary>One ordering scope instance: a scope kind and its parent identity.</summary>
public readonly record struct WorkOrderingScope(WorkOrderingScopeKind Kind, Guid ParentId)
{
    /// <summary>
    /// Stable scope name used for lock identity. It is spelled out rather than
    /// derived from the enum name so renaming a member cannot change lock keys.
    /// </summary>
    public string KindName => Kind switch
    {
        WorkOrderingScopeKind.BoardGroups => "board-groups",
        WorkOrderingScopeKind.BoardFields => "board-fields",
        WorkOrderingScopeKind.GroupItems => "group-items",
        WorkOrderingScopeKind.ChildItems => "child-items",
        WorkOrderingScopeKind.FieldOptions => "field-options",
        WorkOrderingScopeKind.ItemChecklists => "item-checklists",
        WorkOrderingScopeKind.ChecklistItems => "checklist-items",
        _ => throw new ArgumentOutOfRangeException(nameof(Kind), Kind, "Unknown ordering scope kind."),
    };
}
