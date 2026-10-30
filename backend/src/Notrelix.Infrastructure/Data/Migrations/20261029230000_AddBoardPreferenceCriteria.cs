#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Additive: a member's personal view state (compound filter, sort, group, swimlane) is kept as an opaque,
    /// versioned JSON object next to the typed filter/sort/group rules, which stay for compatibility. The column is
    /// nullable (expand step; no backfill, nothing to contract). The target model is the model snapshot at this migration.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261029230000_AddBoardPreferenceCriteria")]
    public partial class AddBoardPreferenceCriteria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "criteria",
                schema: "work",
                table: "board_view_user_preferences",
                type: "character varying(8192)",
                maxLength: 8192,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "criteria", schema: "work", table: "board_view_user_preferences");
        }
    }
}
