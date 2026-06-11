using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEnterpriseFieldsAndViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_board_views_user_mode",
                table: "board_views");

            migrationBuilder.DropIndex(
                name: "idx_board_fields_board_position",
                table: "board_fields");

            migrationBuilder.RenameColumn(
                name: "view_mode",
                table: "board_views",
                newName: "view_type");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "board_views",
                newName: "owner_user_id");

            migrationBuilder.RenameIndex(
                name: "idx_board_views_board_position",
                table: "board_views",
                newName: "ix_board_views_board_position");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "workspace_invitations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "workspace_invitations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "workspace_invitations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "page_mentions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "page_mentions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "page_mentions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "board_views",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "Main table",
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120,
                oldDefaultValue: "Main table");

            migrationBuilder.AlterColumn<string>(
                name: "view_type",
                table: "board_views",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Kanban",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Kanban");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "board_views",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "board_views",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_private",
                table: "board_views",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "board_fields",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "default_value",
                table: "board_fields",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "board_fields",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_required",
                table: "board_fields",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_system",
                table: "board_fields",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "key",
                table: "board_fields",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "board_fields",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_board_views_board_type",
                table: "board_views",
                columns: new[] { "board_id", "view_type" });

            migrationBuilder.CreateIndex(
                name: "idx_board_fields_board_position",
                table: "board_fields",
                columns: new[] { "board_id", "position" },
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ux_board_fields_board_key",
                table: "board_fields",
                columns: new[] { "board_id", "key" },
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_board_views_board_type",
                table: "board_views");

            migrationBuilder.DropIndex(
                name: "idx_board_fields_board_position",
                table: "board_fields");

            migrationBuilder.DropIndex(
                name: "ux_board_fields_board_key",
                table: "board_fields");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "workspace_invitations");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "workspace_invitations");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "workspace_invitations");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "page_mentions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "page_mentions");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "page_mentions");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "board_views");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "board_views");

            migrationBuilder.DropColumn(
                name: "is_private",
                table: "board_views");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "board_fields");

            migrationBuilder.DropColumn(
                name: "default_value",
                table: "board_fields");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "board_fields");

            migrationBuilder.DropColumn(
                name: "is_required",
                table: "board_fields");

            migrationBuilder.DropColumn(
                name: "is_system",
                table: "board_fields");

            migrationBuilder.DropColumn(
                name: "key",
                table: "board_fields");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "board_fields");

            migrationBuilder.RenameColumn(
                name: "view_type",
                table: "board_views",
                newName: "view_mode");

            migrationBuilder.RenameColumn(
                name: "owner_user_id",
                table: "board_views",
                newName: "user_id");

            migrationBuilder.RenameIndex(
                name: "ix_board_views_board_position",
                table: "board_views",
                newName: "idx_board_views_board_position");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "board_views",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "Main table",
                oldClrType: typeof(string),
                oldType: "character varying(160)",
                oldMaxLength: 160,
                oldDefaultValue: "Main table");

            migrationBuilder.AlterColumn<string>(
                name: "view_mode",
                table: "board_views",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Kanban",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldDefaultValue: "Kanban");

            migrationBuilder.CreateIndex(
                name: "idx_board_views_user_mode",
                table: "board_views",
                columns: new[] { "board_id", "user_id", "view_mode" });

            migrationBuilder.CreateIndex(
                name: "idx_board_fields_board_position",
                table: "board_fields",
                columns: new[] { "board_id", "position" });
        }
    }
}
