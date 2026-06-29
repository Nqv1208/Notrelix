using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SchemaCompletionV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "idx_workspaces_personal_per_user",
                schema: "workspace",
                table: "workspaces",
                column: "created_by",
                unique: true,
                filter: "is_personal = true AND deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_workspaces_personal_per_user",
                schema: "workspace",
                table: "workspaces");
        }
    }
}
