using Notrelix.Infrastructure.Data.WorkManagement;

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReconcileStatusCompletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing data only (no schema change): legacy Status fields get a done
            // option and Items already in a done status get their completion time
            // (idempotent, fails loudly under row-level security or when incomplete).
            migrationBuilder.Sql(StatusCompletionMigrationSql.Backfill);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Forward-only data step: the categories and completion times it set are
            // indistinguishable from later user changes, so nothing is reverted.
        }
    }
}
