using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGovernanceRefactoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_board_columns_boards_board_id",
                table: "board_columns");

            migrationBuilder.DropForeignKey(
                name: "FK_card_labels_cards_card_id",
                table: "card_labels");

            migrationBuilder.DropForeignKey(
                name: "FK_card_labels_labels_label_id",
                table: "card_labels");

            migrationBuilder.DropForeignKey(
                name: "FK_card_links_cards_source_card_id",
                table: "card_links");

            migrationBuilder.DropForeignKey(
                name: "FK_card_links_cards_target_card_id",
                table: "card_links");

            migrationBuilder.DropForeignKey(
                name: "FK_card_members_cards_card_id",
                table: "card_members");

            migrationBuilder.DropForeignKey(
                name: "FK_cards_lists_list_id",
                table: "cards");

            migrationBuilder.DropForeignKey(
                name: "FK_cards_pages_linked_page_id",
                table: "cards");

            migrationBuilder.DropForeignKey(
                name: "FK_checklists_cards_card_id",
                table: "checklists");

            migrationBuilder.DropForeignKey(
                name: "FK_lists_boards_board_id",
                table: "lists");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_lists",
                table: "lists");

            migrationBuilder.DropPrimaryKey(
                name: "PK_cards",
                table: "cards");

            migrationBuilder.DropPrimaryKey(
                name: "PK_card_members",
                table: "card_members");

            migrationBuilder.DropPrimaryKey(
                name: "PK_card_links",
                table: "card_links");

            migrationBuilder.DropPrimaryKey(
                name: "PK_card_labels",
                table: "card_labels");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_columns",
                table: "board_columns");

            migrationBuilder.RenameTable(
                name: "lists",
                newName: "board_groups");

            migrationBuilder.RenameTable(
                name: "cards",
                newName: "board_items");

            migrationBuilder.RenameTable(
                name: "card_members",
                newName: "board_item_members");

            migrationBuilder.RenameTable(
                name: "card_links",
                newName: "board_item_links");

            migrationBuilder.RenameTable(
                name: "card_labels",
                newName: "board_item_labels");

            migrationBuilder.RenameTable(
                name: "board_columns",
                newName: "board_fields");

            migrationBuilder.RenameColumn(
                name: "card_id",
                table: "checklists",
                newName: "board_item_id");

            migrationBuilder.RenameIndex(
                name: "IX_checklists_card_id",
                table: "checklists",
                newName: "IX_checklists_board_item_id");

            migrationBuilder.RenameIndex(
                name: "IX_lists_board_id",
                table: "board_groups",
                newName: "IX_board_groups_board_id");

            migrationBuilder.RenameColumn(
                name: "list_id",
                table: "board_items",
                newName: "group_id");

            migrationBuilder.RenameColumn(
                name: "field_values",
                table: "board_items",
                newName: "values_json");

            migrationBuilder.RenameIndex(
                name: "IX_cards_linked_page_id",
                table: "board_items",
                newName: "IX_board_items_linked_page_id");

            migrationBuilder.RenameIndex(
                name: "idx_cards_list_position",
                table: "board_items",
                newName: "idx_board_items_group_position");

            migrationBuilder.RenameColumn(
                name: "card_id",
                table: "board_item_members",
                newName: "board_item_id");

            migrationBuilder.RenameColumn(
                name: "target_card_id",
                table: "board_item_links",
                newName: "target_board_item_id");

            migrationBuilder.RenameColumn(
                name: "source_card_id",
                table: "board_item_links",
                newName: "source_board_item_id");

            migrationBuilder.RenameIndex(
                name: "IX_card_links_target_card_id",
                table: "board_item_links",
                newName: "IX_board_item_links_target_board_item_id");

            migrationBuilder.RenameIndex(
                name: "IX_card_links_source_card_id_target_card_id_link_type",
                table: "board_item_links",
                newName: "IX_board_item_links_source_board_item_id_target_board_item_id_~");

            migrationBuilder.RenameColumn(
                name: "card_id",
                table: "board_item_labels",
                newName: "board_item_id");

            migrationBuilder.RenameIndex(
                name: "IX_card_labels_label_id",
                table: "board_item_labels",
                newName: "IX_board_item_labels_label_id");

            migrationBuilder.RenameIndex(
                name: "idx_board_columns_board_position",
                table: "board_fields",
                newName: "idx_board_fields_board_position");

            migrationBuilder.AlterColumn<string>(
                name: "settings",
                table: "board_fields",
                type: "jsonb",
                nullable: false,
                defaultValue: "{\"options\":[]}",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldDefaultValue: "{}");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_groups",
                table: "board_groups",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_items",
                table: "board_items",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_item_members",
                table: "board_item_members",
                columns: new[] { "board_item_id", "user_id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_item_links",
                table: "board_item_links",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_item_labels",
                table: "board_item_labels",
                columns: new[] { "board_item_id", "label_id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_fields",
                table: "board_fields",
                column: "id");

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    resource_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: true),
                    before_json = table.Column<string>(type: "jsonb", nullable: true),
                    after_json = table.Column<string>(type: "jsonb", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_logs_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "resource_permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    granted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    granted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    revoked_by = table.Column<Guid>(type: "uuid", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource_permissions", x => x.id);
                    table.ForeignKey(
                        name: "FK_resource_permissions_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "share_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_share_links", x => x.id);
                    table.ForeignKey(
                        name: "FK_share_links_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_audit_logs_actor",
                table: "audit_logs",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "idx_audit_logs_resource",
                table: "audit_logs",
                columns: new[] { "resource_type", "resource_id" });

            migrationBuilder.CreateIndex(
                name: "idx_audit_logs_workspace_occurred",
                table: "audit_logs",
                columns: new[] { "workspace_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "idx_resource_permissions_resource",
                table: "resource_permissions",
                columns: new[] { "resource_type", "resource_id", "subject_type", "subject_id" });

            migrationBuilder.CreateIndex(
                name: "IX_resource_permissions_subject_type_subject_id",
                table: "resource_permissions",
                columns: new[] { "subject_type", "subject_id" });

            migrationBuilder.CreateIndex(
                name: "IX_resource_permissions_workspace_id_resource_type_resource_id~",
                table: "resource_permissions",
                columns: new[] { "workspace_id", "resource_type", "resource_id", "subject_type", "subject_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_share_links_lookup",
                table: "share_links",
                columns: new[] { "workspace_id", "resource_type", "resource_id" });

            migrationBuilder.CreateIndex(
                name: "ux_share_links_token_hash",
                table: "share_links",
                column: "token_hash",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_board_fields_boards_board_id",
                table: "board_fields",
                column: "board_id",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_board_groups_boards_board_id",
                table: "board_groups",
                column: "board_id",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_board_item_labels_board_items_board_item_id",
                table: "board_item_labels",
                column: "board_item_id",
                principalTable: "board_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_board_item_labels_labels_label_id",
                table: "board_item_labels",
                column: "label_id",
                principalTable: "labels",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_board_item_links_board_items_source_board_item_id",
                table: "board_item_links",
                column: "source_board_item_id",
                principalTable: "board_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_board_item_links_board_items_target_board_item_id",
                table: "board_item_links",
                column: "target_board_item_id",
                principalTable: "board_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_board_item_members_board_items_board_item_id",
                table: "board_item_members",
                column: "board_item_id",
                principalTable: "board_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_board_items_board_groups_group_id",
                table: "board_items",
                column: "group_id",
                principalTable: "board_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_board_items_pages_linked_page_id",
                table: "board_items",
                column: "linked_page_id",
                principalTable: "pages",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_checklists_board_items_board_item_id",
                table: "checklists",
                column: "board_item_id",
                principalTable: "board_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_board_fields_boards_board_id",
                table: "board_fields");

            migrationBuilder.DropForeignKey(
                name: "FK_board_groups_boards_board_id",
                table: "board_groups");

            migrationBuilder.DropForeignKey(
                name: "FK_board_item_labels_board_items_board_item_id",
                table: "board_item_labels");

            migrationBuilder.DropForeignKey(
                name: "FK_board_item_labels_labels_label_id",
                table: "board_item_labels");

            migrationBuilder.DropForeignKey(
                name: "FK_board_item_links_board_items_source_board_item_id",
                table: "board_item_links");

            migrationBuilder.DropForeignKey(
                name: "FK_board_item_links_board_items_target_board_item_id",
                table: "board_item_links");

            migrationBuilder.DropForeignKey(
                name: "FK_board_item_members_board_items_board_item_id",
                table: "board_item_members");

            migrationBuilder.DropForeignKey(
                name: "FK_board_items_board_groups_group_id",
                table: "board_items");

            migrationBuilder.DropForeignKey(
                name: "FK_board_items_pages_linked_page_id",
                table: "board_items");

            migrationBuilder.DropForeignKey(
                name: "FK_checklists_board_items_board_item_id",
                table: "checklists");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "resource_permissions");

            migrationBuilder.DropTable(
                name: "share_links");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_items",
                table: "board_items");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_item_members",
                table: "board_item_members");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_item_links",
                table: "board_item_links");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_item_labels",
                table: "board_item_labels");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_groups",
                table: "board_groups");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_fields",
                table: "board_fields");

            migrationBuilder.RenameTable(
                name: "board_items",
                newName: "cards");

            migrationBuilder.RenameTable(
                name: "board_item_members",
                newName: "card_members");

            migrationBuilder.RenameTable(
                name: "board_item_links",
                newName: "card_links");

            migrationBuilder.RenameTable(
                name: "board_item_labels",
                newName: "card_labels");

            migrationBuilder.RenameTable(
                name: "board_groups",
                newName: "lists");

            migrationBuilder.RenameTable(
                name: "board_fields",
                newName: "board_columns");

            migrationBuilder.RenameColumn(
                name: "board_item_id",
                table: "checklists",
                newName: "card_id");

            migrationBuilder.RenameIndex(
                name: "IX_checklists_board_item_id",
                table: "checklists",
                newName: "IX_checklists_card_id");

            migrationBuilder.RenameColumn(
                name: "group_id",
                table: "cards",
                newName: "list_id");

            migrationBuilder.RenameColumn(
                name: "values_json",
                table: "cards",
                newName: "field_values");

            migrationBuilder.RenameIndex(
                name: "IX_board_items_linked_page_id",
                table: "cards",
                newName: "IX_cards_linked_page_id");

            migrationBuilder.RenameIndex(
                name: "idx_board_items_group_position",
                table: "cards",
                newName: "idx_cards_list_position");

            migrationBuilder.RenameColumn(
                name: "board_item_id",
                table: "card_members",
                newName: "card_id");

            migrationBuilder.RenameColumn(
                name: "target_board_item_id",
                table: "card_links",
                newName: "target_card_id");

            migrationBuilder.RenameColumn(
                name: "source_board_item_id",
                table: "card_links",
                newName: "source_card_id");

            migrationBuilder.RenameIndex(
                name: "IX_board_item_links_target_board_item_id",
                table: "card_links",
                newName: "IX_card_links_target_card_id");

            migrationBuilder.RenameIndex(
                name: "IX_board_item_links_source_board_item_id_target_board_item_id_~",
                table: "card_links",
                newName: "IX_card_links_source_card_id_target_card_id_link_type");

            migrationBuilder.RenameColumn(
                name: "board_item_id",
                table: "card_labels",
                newName: "card_id");

            migrationBuilder.RenameIndex(
                name: "IX_board_item_labels_label_id",
                table: "card_labels",
                newName: "IX_card_labels_label_id");

            migrationBuilder.RenameIndex(
                name: "IX_board_groups_board_id",
                table: "lists",
                newName: "IX_lists_board_id");

            migrationBuilder.RenameIndex(
                name: "idx_board_fields_board_position",
                table: "board_columns",
                newName: "idx_board_columns_board_position");

            migrationBuilder.AlterColumn<string>(
                name: "settings",
                table: "board_columns",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldDefaultValue: "{\"options\":[]}");

            migrationBuilder.AddPrimaryKey(
                name: "PK_cards",
                table: "cards",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_card_members",
                table: "card_members",
                columns: new[] { "card_id", "user_id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_card_links",
                table: "card_links",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_card_labels",
                table: "card_labels",
                columns: new[] { "card_id", "label_id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_lists",
                table: "lists",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_columns",
                table: "board_columns",
                column: "id");

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    granted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.id);
                    table.ForeignKey(
                        name: "FK_permissions_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_permissions_resource",
                table: "permissions",
                columns: new[] { "resource_type", "resource_id", "subject_type", "subject_id" });

            migrationBuilder.CreateIndex(
                name: "IX_permissions_subject_type_subject_id",
                table: "permissions",
                columns: new[] { "subject_type", "subject_id" });

            migrationBuilder.CreateIndex(
                name: "IX_permissions_workspace_id_resource_type_resource_id_subject_~",
                table: "permissions",
                columns: new[] { "workspace_id", "resource_type", "resource_id", "subject_type", "subject_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_board_columns_boards_board_id",
                table: "board_columns",
                column: "board_id",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_card_labels_cards_card_id",
                table: "card_labels",
                column: "card_id",
                principalTable: "cards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_card_labels_labels_label_id",
                table: "card_labels",
                column: "label_id",
                principalTable: "labels",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_card_links_cards_source_card_id",
                table: "card_links",
                column: "source_card_id",
                principalTable: "cards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_card_links_cards_target_card_id",
                table: "card_links",
                column: "target_card_id",
                principalTable: "cards",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_card_members_cards_card_id",
                table: "card_members",
                column: "card_id",
                principalTable: "cards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_cards_lists_list_id",
                table: "cards",
                column: "list_id",
                principalTable: "lists",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_cards_pages_linked_page_id",
                table: "cards",
                column: "linked_page_id",
                principalTable: "pages",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_checklists_cards_card_id",
                table: "checklists",
                column: "card_id",
                principalTable: "cards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_lists_boards_board_id",
                table: "lists",
                column: "board_id",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
