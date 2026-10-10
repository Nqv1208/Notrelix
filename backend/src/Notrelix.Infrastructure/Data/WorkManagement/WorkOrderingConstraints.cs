namespace Notrelix.Infrastructure.Data.WorkManagement;

/// <summary>
/// The named unique ordering constraints of the P3 scopes (WM-V3-ORDER-003). Only
/// a violation of one of these names is an ordering conflict; any other unique
/// violation keeps its existing mapping.
/// </summary>
public static class WorkOrderingConstraints
{
    public static readonly IReadOnlySet<string> Names = new HashSet<string>(StringComparer.Ordinal)
    {
        "ux_work_board_items_group_position_active",
        "ux_work_board_groups_board_position_active",
        "ux_work_board_fields_board_position_active",
        "ux_work_checklists_item_position_active",
        "ux_work_checklist_items_checklist_position",
        "ux_work_field_options_field_position",
    };

    public static bool IsOrderingViolation(Exception exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres
        && postgres.ConstraintName is not null
        && Names.Contains(postgres.ConstraintName);
}
