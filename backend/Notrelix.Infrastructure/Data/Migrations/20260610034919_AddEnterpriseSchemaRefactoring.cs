using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterpriseSchemaRefactoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_board_item_labels_board_items_board_item_id",
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
                name: "FK_checklists_board_items_board_item_id",
                table: "checklists");

            migrationBuilder.DropIndex(
                name: "idx_board_items_group_position",
                table: "board_items");

            migrationBuilder.RenameColumn(
                name: "token",
                table: "workspace_invitations",
                newName: "token_hash");

            migrationBuilder.RenameIndex(
                name: "IX_workspace_invitations_token",
                table: "workspace_invitations",
                newName: "IX_workspace_invitations_token_hash");

            migrationBuilder.RenameColumn(
                name: "avatar",
                table: "users",
                newName: "avatar_url");

            migrationBuilder.RenameColumn(
                name: "refresh_token",
                table: "sessions",
                newName: "refresh_token_hash");

            migrationBuilder.RenameIndex(
                name: "IX_sessions_refresh_token",
                table: "sessions",
                newName: "IX_sessions_refresh_token_hash");

            migrationBuilder.RenameColumn(
                name: "provider_id",
                table: "oauth_accounts",
                newName: "provider_account_id");

            migrationBuilder.RenameIndex(
                name: "IX_oauth_accounts_provider_provider_id",
                table: "oauth_accounts",
                newName: "IX_oauth_accounts_provider_provider_account_id");

            migrationBuilder.RenameColumn(
                name: "board_item_id",
                table: "checklists",
                newName: "item_id");

            migrationBuilder.RenameIndex(
                name: "IX_checklists_board_item_id",
                table: "checklists",
                newName: "IX_checklists_item_id");

            migrationBuilder.RenameColumn(
                name: "values_json",
                table: "board_items",
                newName: "values");

            migrationBuilder.RenameColumn(
                name: "title",
                table: "board_items",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "board_items",
                newName: "created_by_user_id");

            migrationBuilder.RenameColumn(
                name: "board_item_id",
                table: "board_item_members",
                newName: "item_id");

            migrationBuilder.RenameColumn(
                name: "target_board_item_id",
                table: "board_item_links",
                newName: "target_item_id");

            migrationBuilder.RenameColumn(
                name: "source_board_item_id",
                table: "board_item_links",
                newName: "source_item_id");

            migrationBuilder.RenameIndex(
                name: "IX_board_item_links_target_board_item_id",
                table: "board_item_links",
                newName: "IX_board_item_links_target_item_id");

            migrationBuilder.RenameIndex(
                name: "IX_board_item_links_source_board_item_id_target_board_item_id_~",
                table: "board_item_links",
                newName: "IX_board_item_links_source_item_id_target_item_id_link_type");

            migrationBuilder.RenameColumn(
                name: "board_item_id",
                table: "board_item_labels",
                newName: "item_id");

            migrationBuilder.RenameColumn(
                name: "title",
                table: "board_groups",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "type",
                table: "blocks",
                newName: "block_type");

            migrationBuilder.RenameColumn(
                name: "properties",
                table: "blocks",
                newName: "content_json");

            migrationBuilder.AddColumn<string>(
                name: "normalized_email",
                table: "users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "board_id",
                table: "board_items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "board_items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "properties_json",
                table: "blocks",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.CreateIndex(
                name: "IX_users_normalized_email",
                table: "users",
                column: "normalized_email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_board_items_board_group_position",
                table: "board_items",
                columns: new[] { "board_id", "group_id", "position" },
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "IX_board_items_group_id",
                table: "board_items",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_board_items_workspace_id",
                table: "board_items",
                column: "workspace_id");

            migrationBuilder.AddForeignKey(
                name: "FK_board_item_labels_board_items_item_id",
                table: "board_item_labels",
                column: "item_id",
                principalTable: "board_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_board_item_links_board_items_source_item_id",
                table: "board_item_links",
                column: "source_item_id",
                principalTable: "board_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_board_item_links_board_items_target_item_id",
                table: "board_item_links",
                column: "target_item_id",
                principalTable: "board_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_board_item_members_board_items_item_id",
                table: "board_item_members",
                column: "item_id",
                principalTable: "board_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_board_items_boards_board_id",
                table: "board_items",
                column: "board_id",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_board_items_workspaces_workspace_id",
                table: "board_items",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_checklists_board_items_item_id",
                table: "checklists",
                column: "item_id",
                principalTable: "board_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_board_item_labels_board_items_item_id",
                table: "board_item_labels");

            migrationBuilder.DropForeignKey(
                name: "FK_board_item_links_board_items_source_item_id",
                table: "board_item_links");

            migrationBuilder.DropForeignKey(
                name: "FK_board_item_links_board_items_target_item_id",
                table: "board_item_links");

            migrationBuilder.DropForeignKey(
                name: "FK_board_item_members_board_items_item_id",
                table: "board_item_members");

            migrationBuilder.DropForeignKey(
                name: "FK_board_items_boards_board_id",
                table: "board_items");

            migrationBuilder.DropForeignKey(
                name: "FK_board_items_workspaces_workspace_id",
                table: "board_items");

            migrationBuilder.DropForeignKey(
                name: "FK_checklists_board_items_item_id",
                table: "checklists");

            migrationBuilder.DropIndex(
                name: "IX_users_normalized_email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_board_items_board_group_position",
                table: "board_items");

            migrationBuilder.DropIndex(
                name: "IX_board_items_group_id",
                table: "board_items");

            migrationBuilder.DropIndex(
                name: "IX_board_items_workspace_id",
                table: "board_items");

            migrationBuilder.DropColumn(
                name: "normalized_email",
                table: "users");

            migrationBuilder.DropColumn(
                name: "board_id",
                table: "board_items");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "board_items");

            migrationBuilder.DropColumn(
                name: "properties_json",
                table: "blocks");

            migrationBuilder.RenameColumn(
                name: "token_hash",
                table: "workspace_invitations",
                newName: "token");

            migrationBuilder.RenameIndex(
                name: "IX_workspace_invitations_token_hash",
                table: "workspace_invitations",
                newName: "IX_workspace_invitations_token");

            migrationBuilder.RenameColumn(
                name: "avatar_url",
                table: "users",
                newName: "avatar");

            migrationBuilder.RenameColumn(
                name: "refresh_token_hash",
                table: "sessions",
                newName: "refresh_token");

            migrationBuilder.RenameIndex(
                name: "IX_sessions_refresh_token_hash",
                table: "sessions",
                newName: "IX_sessions_refresh_token");

            migrationBuilder.RenameColumn(
                name: "provider_account_id",
                table: "oauth_accounts",
                newName: "provider_id");

            migrationBuilder.RenameIndex(
                name: "IX_oauth_accounts_provider_provider_account_id",
                table: "oauth_accounts",
                newName: "IX_oauth_accounts_provider_provider_id");

            migrationBuilder.RenameColumn(
                name: "item_id",
                table: "checklists",
                newName: "board_item_id");

            migrationBuilder.RenameIndex(
                name: "IX_checklists_item_id",
                table: "checklists",
                newName: "IX_checklists_board_item_id");

            migrationBuilder.RenameColumn(
                name: "values",
                table: "board_items",
                newName: "values_json");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "board_items",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "created_by_user_id",
                table: "board_items",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "item_id",
                table: "board_item_members",
                newName: "board_item_id");

            migrationBuilder.RenameColumn(
                name: "target_item_id",
                table: "board_item_links",
                newName: "target_board_item_id");

            migrationBuilder.RenameColumn(
                name: "source_item_id",
                table: "board_item_links",
                newName: "source_board_item_id");

            migrationBuilder.RenameIndex(
                name: "IX_board_item_links_target_item_id",
                table: "board_item_links",
                newName: "IX_board_item_links_target_board_item_id");

            migrationBuilder.RenameIndex(
                name: "IX_board_item_links_source_item_id_target_item_id_link_type",
                table: "board_item_links",
                newName: "IX_board_item_links_source_board_item_id_target_board_item_id_~");

            migrationBuilder.RenameColumn(
                name: "item_id",
                table: "board_item_labels",
                newName: "board_item_id");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "board_groups",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "content_json",
                table: "blocks",
                newName: "properties");

            migrationBuilder.RenameColumn(
                name: "block_type",
                table: "blocks",
                newName: "type");

            migrationBuilder.CreateIndex(
                name: "idx_board_items_group_position",
                table: "board_items",
                columns: new[] { "group_id", "position" },
                filter: "is_deleted = false");

            migrationBuilder.AddForeignKey(
                name: "FK_board_item_labels_board_items_board_item_id",
                table: "board_item_labels",
                column: "board_item_id",
                principalTable: "board_items",
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
                name: "FK_checklists_board_items_board_item_id",
                table: "checklists",
                column: "board_item_id",
                principalTable: "board_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
