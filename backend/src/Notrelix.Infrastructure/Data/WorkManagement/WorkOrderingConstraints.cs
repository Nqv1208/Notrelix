namespace Notrelix.Infrastructure.Data.WorkManagement;

/// <summary>
/// The named unique ordering constraints of the P3 scopes (WM-V3-ORDER-003,
/// installed by WM-V3-DATA-002). Only a violation of one of these names is an
/// ordering conflict; any other unique violation keeps its existing mapping.
/// </summary>
public static class WorkOrderingConstraints
{
    /// <summary>Ordinal collation of every P3 position column (WM-V3-DATA-001).</summary>
    public const string OrdinalCollation = "C";

    public const string GroupItems = "ux_work_board_items_group_position_active";
    public const string BoardGroups = "ux_work_board_groups_board_position_active";
    public const string BoardFields = "ux_work_board_fields_board_position_active";
    public const string ItemChecklists = "ux_work_checklists_item_position_active";
    public const string ChecklistItems = "ux_work_checklist_items_checklist_position";
    public const string FieldOptions = "ux_work_field_options_field_position";

    public static readonly IReadOnlySet<string> Names = new HashSet<string>(StringComparer.Ordinal)
    {
        GroupItems, BoardGroups, BoardFields, ItemChecklists, ChecklistItems, FieldOptions,
    };

    public static bool IsOrderingViolation(Exception exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres
        && postgres.ConstraintName is not null
        && Names.Contains(postgres.ConstraintName);
}
