namespace Notrelix.Application.Features.Collaboration.Activity;

/// <summary>
/// The stable types of the Item history rows Collaboration records from Work Management facts. A client
/// words each type in its own language; the type, not the English phrase, is the contract.
/// </summary>
public static class ItemActivityTypes
{
    public const string ResourceKind = "work-management.board-item";
    public const string SubjectType = "BoardItem";

    public const string Created = "work.item-created";
    public const string Renamed = "work.item-renamed";
    public const string StatusChanged = "work.item-status-changed";
    public const string FieldChanged = "work.item-field-changed";
    public const string Assigned = "work.item-assigned";
    public const string Unassigned = "work.item-unassigned";
    public const string LabelAdded = "work.item-label-added";
    public const string LabelRemoved = "work.item-label-removed";
    public const string ChecklistAdded = "work.item-checklist-added";
    public const string ChecklistRenamed = "work.item-checklist-renamed";
    public const string ChecklistDeleted = "work.item-checklist-deleted";
    public const string ChecklistItemAdded = "work.item-checklist-item-added";
    public const string ChecklistItemCompleted = "work.item-checklist-item-completed";
    public const string ChecklistItemReopened = "work.item-checklist-item-reopened";
    public const string ChecklistItemRemoved = "work.item-checklist-item-removed";
    public const string MovedToGroup = "work.item-moved";
    public const string MovedToBoard = "work.item-moved-to-board";
    public const string Archived = "work.item-archived";
    public const string Unarchived = "work.item-unarchived";
    public const string Deleted = "work.item-deleted";
    public const string Restored = "work.item-restored";
}
