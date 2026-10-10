using Notrelix.Infrastructure.Data.WorkManagement;

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CanonicalizeWorkOrdering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // WM-V3-MIG-ORDER-001: fail closed before any DDL; the migration transaction
            // aborts with WM_ORDERING_PREFLIGHT_FAILED and leaves the schema unchanged.
            migrationBuilder.Sql(WorkOrderingPreflight.Sql);

            migrationBuilder.DropIndex(
                name: "idx_checklist_items_checklist_position",
                schema: "work",
                table: "checklist_items");

            migrationBuilder.DropIndex(
                name: "idx_board_items_group_position",
                schema: "work",
                table: "board_items");

            migrationBuilder.DropIndex(
                name: "idx_board_groups_board_position",
                schema: "work",
                table: "board_groups");

            migrationBuilder.DropIndex(
                name: "idx_board_fields_board_position",
                schema: "work",
                table: "board_fields");

            migrationBuilder.AlterColumn<string>(
                name: "position",
                schema: "work",
                table: "field_options",
                type: "text",
                nullable: false,
                collation: "C",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "position",
                schema: "work",
                table: "checklists",
                type: "text",
                nullable: false,
                collation: "C",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "position",
                schema: "work",
                table: "checklist_items",
                type: "text",
                nullable: false,
                collation: "C",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "position",
                schema: "work",
                table: "board_items",
                type: "text",
                nullable: false,
                collation: "C",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "position",
                schema: "work",
                table: "board_groups",
                type: "text",
                nullable: false,
                collation: "C",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "position",
                schema: "work",
                table: "board_fields",
                type: "text",
                nullable: false,
                collation: "C",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "ux_work_field_options_field_position",
                schema: "work",
                table: "field_options",
                columns: new[] { "field_id", "position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_work_checklists_item_position_active",
                schema: "work",
                table: "checklists",
                columns: new[] { "item_id", "position" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_work_checklist_items_checklist_position",
                schema: "work",
                table: "checklist_items",
                columns: new[] { "checklist_id", "position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_work_board_items_group_position_active",
                schema: "work",
                table: "board_items",
                columns: new[] { "group_id", "position" },
                unique: true,
                filter: "deleted_at IS NULL AND parent_item_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_work_board_groups_board_position_active",
                schema: "work",
                table: "board_groups",
                columns: new[] { "board_id", "position" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_work_board_fields_board_position_active",
                schema: "work",
                table: "board_fields",
                columns: new[] { "board_id", "position" },
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_work_field_options_field_position",
                schema: "work",
                table: "field_options");

            migrationBuilder.DropIndex(
                name: "ux_work_checklists_item_position_active",
                schema: "work",
                table: "checklists");

            migrationBuilder.DropIndex(
                name: "ux_work_checklist_items_checklist_position",
                schema: "work",
                table: "checklist_items");

            migrationBuilder.DropIndex(
                name: "ux_work_board_items_group_position_active",
                schema: "work",
                table: "board_items");

            migrationBuilder.DropIndex(
                name: "ux_work_board_groups_board_position_active",
                schema: "work",
                table: "board_groups");

            migrationBuilder.DropIndex(
                name: "ux_work_board_fields_board_position_active",
                schema: "work",
                table: "board_fields");

            migrationBuilder.AlterColumn<string>(
                name: "position",
                schema: "work",
                table: "field_options",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "C");

            migrationBuilder.AlterColumn<string>(
                name: "position",
                schema: "work",
                table: "checklists",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "C");

            migrationBuilder.AlterColumn<string>(
                name: "position",
                schema: "work",
                table: "checklist_items",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "C");

            migrationBuilder.AlterColumn<string>(
                name: "position",
                schema: "work",
                table: "board_items",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "C");

            migrationBuilder.AlterColumn<string>(
                name: "position",
                schema: "work",
                table: "board_groups",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "C");

            migrationBuilder.AlterColumn<string>(
                name: "position",
                schema: "work",
                table: "board_fields",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "C");

            migrationBuilder.CreateIndex(
                name: "idx_checklist_items_checklist_position",
                schema: "work",
                table: "checklist_items",
                columns: new[] { "checklist_id", "position" });

            migrationBuilder.CreateIndex(
                name: "idx_board_items_group_position",
                schema: "work",
                table: "board_items",
                columns: new[] { "group_id", "position" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_board_groups_board_position",
                schema: "work",
                table: "board_groups",
                columns: new[] { "board_id", "position" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_board_fields_board_position",
                schema: "work",
                table: "board_fields",
                columns: new[] { "board_id", "position" });
        }
    }
}
