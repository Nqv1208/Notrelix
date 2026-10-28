using Notrelix.Infrastructure.Data.WorkManagement;

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Data-only step: the EF model does not change, so this migration carries no target model of its own.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261027150000_BackfillDefaultBoardGroups")]
    public partial class BackfillDefaultBoardGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Boards created before a new board started with default groups get them (idempotent).
            migrationBuilder.Sql(BoardGroupMigrationSql.Backfill);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Backfilled groups are ordinary groups people may already use; nothing is removed.
        }
    }
}
