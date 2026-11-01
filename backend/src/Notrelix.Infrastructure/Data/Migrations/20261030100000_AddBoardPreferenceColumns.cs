#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Additive: a member's own table columns (hidden, order, widths) are kept as an opaque, versioned JSON object
    /// next to the personal criteria. The column is nullable (expand step; no backfill, nothing to contract): a
    /// member without one sees the shared view's columns. The target model is the model snapshot at this migration.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261030100000_AddBoardPreferenceColumns")]
    public partial class AddBoardPreferenceColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "columns",
                schema: "work",
                table: "board_view_user_preferences",
                type: "character varying(4096)",
                maxLength: 4096,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "columns", schema: "work", table: "board_view_user_preferences");
        }
    }
}
