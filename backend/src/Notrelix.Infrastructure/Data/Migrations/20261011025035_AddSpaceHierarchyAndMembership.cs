using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Notrelix.Infrastructure.Data.Workspaces;

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSpaceHierarchyAndMembership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ADR-009: fail closed before any DDL when existing Board placements
            // cannot be honoured; never guess a placement.
            migrationBuilder.Sql(SpaceHierarchyMigrationSql.Preflight);

            migrationBuilder.AddColumn<Guid>(
                name: "parent_space_id",
                schema: "workspace",
                table: "spaces",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "position",
                schema: "workspace",
                table: "spaces",
                type: "text",
                nullable: false,
                defaultValue: "",
                collation: "C");

            migrationBuilder.AddColumn<Guid>(
                name: "space_id",
                schema: "docs",
                table: "pages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "space_members",
                schema: "workspace",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    space_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_space_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_space_members_spaces_space_id",
                        column: x => x.space_id,
                        principalSchema: "workspace",
                        principalTable: "spaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "space_paths",
                schema: "workspace",
                columns: table => new
                {
                    ancestor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    descendant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    depth = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_space_paths", x => new { x.ancestor_id, x.descendant_id });
                    table.ForeignKey(
                        name: "fk_space_paths_ancestor",
                        column: x => x.ancestor_id,
                        principalSchema: "workspace",
                        principalTable: "spaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_space_paths_descendant",
                        column: x => x.descendant_id,
                        principalSchema: "workspace",
                        principalTable: "spaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Existing data: every Space is a root. Give siblings distinct ordered
            // keys before the unique position index exists.
            migrationBuilder.Sql(SpaceHierarchyMigrationSql.BackfillPositions);

            migrationBuilder.CreateIndex(
                name: "ix_spaces_parent_space_id",
                schema: "workspace",
                table: "spaces",
                column: "parent_space_id");

            migrationBuilder.CreateIndex(
                name: "ix_workspace_spaces_parent",
                schema: "workspace",
                table: "spaces",
                columns: new[] { "workspace_id", "parent_space_id" });

            migrationBuilder.CreateIndex(
                name: "ux_workspace_spaces_parent_position_active",
                schema: "workspace",
                table: "spaces",
                columns: new[] { "workspace_id", "parent_space_id", "position" },
                unique: true,
                filter: "deleted_at IS NULL")
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ix_docs_pages_workspace_space",
                schema: "docs",
                table: "pages",
                columns: new[] { "workspace_id", "space_id" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_pages_space_id",
                schema: "docs",
                table: "pages",
                column: "space_id");

            migrationBuilder.CreateIndex(
                name: "ix_boards_space_id",
                schema: "work",
                table: "boards",
                column: "space_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_boards_workspace_space",
                schema: "work",
                table: "boards",
                columns: new[] { "workspace_id", "space_id" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_workspace_space_members_subject",
                schema: "workspace",
                table: "space_members",
                columns: new[] { "workspace_id", "subject_type", "subject_id" });

            migrationBuilder.CreateIndex(
                name: "ux_workspace_space_members_space_subject",
                schema: "workspace",
                table: "space_members",
                columns: new[] { "space_id", "subject_type", "subject_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_workspace_space_paths_descendant",
                schema: "workspace",
                table: "space_paths",
                columns: new[] { "descendant_id", "depth" });

            migrationBuilder.AddForeignKey(
                name: "fk_boards_space",
                schema: "work",
                table: "boards",
                column: "space_id",
                principalSchema: "workspace",
                principalTable: "spaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pages_space",
                schema: "docs",
                table: "pages",
                column: "space_id",
                principalSchema: "workspace",
                principalTable: "spaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_spaces_parent_space",
                schema: "workspace",
                table: "spaces",
                column: "parent_space_id",
                principalSchema: "workspace",
                principalTable: "spaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            // Existing data, once the unique keys exist: closure self-rows for every
            // (root) Space and each creator as its Space Admin (WSP-024).
            migrationBuilder.Sql(SpaceHierarchyMigrationSql.BackfillClosure);
            migrationBuilder.Sql(SpaceHierarchyMigrationSql.BackfillCreatorAdmins);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_boards_space",
                schema: "work",
                table: "boards");

            migrationBuilder.DropForeignKey(
                name: "fk_pages_space",
                schema: "docs",
                table: "pages");

            migrationBuilder.DropForeignKey(
                name: "fk_spaces_parent_space",
                schema: "workspace",
                table: "spaces");

            migrationBuilder.DropTable(
                name: "space_members",
                schema: "workspace");

            migrationBuilder.DropTable(
                name: "space_paths",
                schema: "workspace");

            migrationBuilder.DropIndex(
                name: "ix_spaces_parent_space_id",
                schema: "workspace",
                table: "spaces");

            migrationBuilder.DropIndex(
                name: "ix_workspace_spaces_parent",
                schema: "workspace",
                table: "spaces");

            migrationBuilder.DropIndex(
                name: "ux_workspace_spaces_parent_position_active",
                schema: "workspace",
                table: "spaces");

            migrationBuilder.DropIndex(
                name: "ix_docs_pages_workspace_space",
                schema: "docs",
                table: "pages");

            migrationBuilder.DropIndex(
                name: "ix_pages_space_id",
                schema: "docs",
                table: "pages");

            migrationBuilder.DropIndex(
                name: "ix_boards_space_id",
                schema: "work",
                table: "boards");

            migrationBuilder.DropIndex(
                name: "ix_work_boards_workspace_space",
                schema: "work",
                table: "boards");

            migrationBuilder.DropColumn(
                name: "parent_space_id",
                schema: "workspace",
                table: "spaces");

            migrationBuilder.DropColumn(
                name: "position",
                schema: "workspace",
                table: "spaces");

            migrationBuilder.DropColumn(
                name: "space_id",
                schema: "docs",
                table: "pages");
        }
    }
}
