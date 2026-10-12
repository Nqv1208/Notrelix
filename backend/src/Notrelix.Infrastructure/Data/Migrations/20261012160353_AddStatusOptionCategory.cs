using Microsoft.EntityFrameworkCore.Migrations;
using Notrelix.Infrastructure.Data.WorkManagement;

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusOptionCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "category",
                schema: "work",
                table: "field_options",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            // Existing Status options get a category by name and position; idempotent.
            migrationBuilder.Sql(StatusOptionCategoryMigrationSql.Backfill);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "category",
                schema: "work",
                table: "field_options");
        }
    }
}
