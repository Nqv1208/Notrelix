#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Approval requests were found by their target (the Board or Item they are about) with only the primary key to
    /// go by: the Board's request list, an Item's request list and the "waiting review" count of the Space overview
    /// each read the whole table. A partial index on the live rows' target serves all three; on 20,000 requests the
    /// Board list went from 1.5 ms to 0.01 ms. Raw SQL like the other secondary indexes, so the EF model does not
    /// change and this migration carries no target model. Additive; nothing to backfill or contract.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261031100000_AddApprovalRequestTargetIndex")]
    public partial class AddApprovalRequestTargetIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS ix_approval_requests_target ON work.approval_requests (target_id) WHERE deleted_at IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS work.ix_approval_requests_target;");
        }
    }
}
