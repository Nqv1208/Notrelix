using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDomainBusinessLogicSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_board_views_boards_BoardId",
                table: "board_views");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_views",
                table: "board_views");

            migrationBuilder.RenameColumn(
                name: "Filters",
                table: "board_views",
                newName: "filters");

            migrationBuilder.RenameColumn(
                name: "ViewMode",
                table: "board_views",
                newName: "view_mode");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "board_views",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "board_views",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "BoardId",
                table: "board_views",
                newName: "board_id");

            migrationBuilder.AddColumn<bool>(
                name: "is_collapsed",
                table: "lists",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "cards",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "id",
                table: "board_views",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "config",
                table: "board_views",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "board_views",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_user_id",
                table: "board_views",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_default",
                table: "board_views",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "board_views",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "Main table");

            migrationBuilder.AddColumn<double>(
                name: "position",
                table: "board_views",
                type: "float8",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.Sql("""
                UPDATE board_views
                SET id = (
                    substr(md5(board_id::text || user_id::text || view_mode::text), 1, 8) || '-' ||
                    substr(md5(board_id::text || user_id::text || view_mode::text), 9, 4) || '-' ||
                    substr(md5(board_id::text || user_id::text || view_mode::text), 13, 4) || '-' ||
                    substr(md5(board_id::text || user_id::text || view_mode::text), 17, 4) || '-' ||
                    substr(md5(board_id::text || user_id::text || view_mode::text), 21, 12)
                )::uuid
                WHERE id IS NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE board_views
                SET created_by_user_id = user_id
                WHERE created_by_user_id IS NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE board_views
                SET created_at = updated_at
                WHERE created_at IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "board_views",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "board_views",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by_user_id",
                table: "board_views",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_views",
                table: "board_views",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "idx_board_views_board_position",
                table: "board_views",
                columns: new[] { "board_id", "position" });

            migrationBuilder.CreateIndex(
                name: "idx_board_views_user_mode",
                table: "board_views",
                columns: new[] { "board_id", "user_id", "view_mode" });

            migrationBuilder.AddForeignKey(
                name: "FK_board_views_boards_board_id",
                table: "board_views",
                column: "board_id",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_board_views_boards_board_id",
                table: "board_views");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_views",
                table: "board_views");

            migrationBuilder.DropIndex(
                name: "idx_board_views_board_position",
                table: "board_views");

            migrationBuilder.DropIndex(
                name: "idx_board_views_user_mode",
                table: "board_views");

            migrationBuilder.DropColumn(
                name: "is_collapsed",
                table: "lists");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "cards");

            migrationBuilder.DropColumn(
                name: "id",
                table: "board_views");

            migrationBuilder.DropColumn(
                name: "config",
                table: "board_views");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "board_views");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                table: "board_views");

            migrationBuilder.DropColumn(
                name: "is_default",
                table: "board_views");

            migrationBuilder.DropColumn(
                name: "name",
                table: "board_views");

            migrationBuilder.DropColumn(
                name: "position",
                table: "board_views");

            migrationBuilder.RenameColumn(
                name: "filters",
                table: "board_views",
                newName: "Filters");

            migrationBuilder.RenameColumn(
                name: "view_mode",
                table: "board_views",
                newName: "ViewMode");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "board_views",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "board_views",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "board_id",
                table: "board_views",
                newName: "BoardId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_views",
                table: "board_views",
                columns: new[] { "BoardId", "UserId" });

            migrationBuilder.AddForeignKey(
                name: "FK_board_views_boards_BoardId",
                table: "board_views",
                column: "BoardId",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
