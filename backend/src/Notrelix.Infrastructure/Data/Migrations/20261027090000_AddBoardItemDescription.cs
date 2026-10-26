using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Additive: an item can carry a Markdown description owned by Work Management. Existing items have none,
    /// so the column is nullable and there is nothing to backfill (expand step; nothing to contract). The
    /// target model is the model snapshot at this migration.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261027090000_AddBoardItemDescription")]
    public partial class AddBoardItemDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "description_md",
                schema: "work",
                table: "board_items",
                type: "character varying(20000)",
                maxLength: 20000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "description_md", schema: "work", table: "board_items");
        }
    }
}
