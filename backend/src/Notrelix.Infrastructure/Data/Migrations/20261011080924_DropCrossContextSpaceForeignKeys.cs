using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropCrossContextSpaceForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_boards_space",
                schema: "work",
                table: "boards");

            migrationBuilder.DropForeignKey(
                name: "fk_pages_space",
                schema: "docs",
                table: "pages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
