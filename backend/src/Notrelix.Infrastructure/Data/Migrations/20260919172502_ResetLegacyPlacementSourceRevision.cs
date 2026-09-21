#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ResetLegacyPlacementSourceRevision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Wave B producer-revision transition (AR-FLOW-01:E2, frozen
            // decision 1): legacy placement rows carry a receiver-timestamp
            // tick-scale source_revision, which is not in the producer-revision
            // domain. Comparing a small producer revision (BoardItem.Version
            // starting at 1) against a legacy tick value would make every live
            // v2 fact look stale, so those rows are invalidated (reset to 0)
            // instead. The authoritative rebuild, or the first v2 live fact
            // (revision >= 1), then supersedes each row through ApplyNewer /
            // Reconcile. This is forward recovery by design: the migration only
            // resets the derived projection watermark, never Work source truth.
            migrationBuilder.Sql(
                """
                UPDATE reporting.workspace_work_item_placements
                SET source_revision = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The legacy tick-scale watermark values were not preserved;
            // reverse migration would reintroduce the stale-row failure mode.
            // Recovery is forward-only via authoritative rebuild. This is the
            // approved invalidate/reset transition, not a reversible reshape.
        }
    }
}
