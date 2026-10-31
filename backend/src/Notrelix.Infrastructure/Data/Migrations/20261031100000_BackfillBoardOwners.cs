using Notrelix.Infrastructure.Data.WorkManagement;

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Existing-data step only (no schema change; the target model is the model snapshot at this migration): the
    /// creator of a live board that has no Owner becomes its Owner when still an active workspace member.
    /// Append-only: the rule is enforced for new boards by the create commands, this repairs the old ones.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261031100000_BackfillBoardOwners")]
    public partial class BackfillBoardOwners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(BoardOwnerMigrationSql.Backfill);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data repair: the added Owner rows are real access and are not removed on a downgrade.
        }
    }
}
