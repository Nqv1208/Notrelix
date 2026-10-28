#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Additive: a saved filter carries the web filter bar state (filter facets, group, sort) as an opaque JSON
    /// object. Saved filters were unreachable before this change, so no row exists to backfill; the column is
    /// nullable (expand step; nothing to contract). The target model is the model snapshot at this migration.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261028190000_AddSavedFilterCriteria")]
    public partial class AddSavedFilterCriteria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "criteria",
                schema: "work",
                table: "saved_filters",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "criteria", schema: "work", table: "saved_filters");
        }
    }
}
