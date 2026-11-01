#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <summary>
    /// The outbox claim keeps a stream in order by asking, for every candidate, whether an earlier message of the same
    /// stream is still not Processed. The only index that answered that (stream_key, stream_version, status) holds every
    /// message ever written, so the probe walked all earlier versions of the stream and filtered them one by one: a
    /// board whose 5,000 item events share one stream made every poll cost about 365 ms and grow with the square of the
    /// backlog. A partial index over only the unprocessed messages holds the stream's head first, so the probe stops at
    /// its first entry (the same poll measured 8 ms). Raw SQL like the other secondary indexes, so the EF model does not
    /// change and this migration carries no target model. Additive; nothing to backfill or contract. Built
    /// CONCURRENTLY (outside the migration transaction) because the outbox is written by every request.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261031110000_AddOutboxStreamUnprocessedIndex")]
    public partial class AddOutboxStreamUnprocessedIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_outbox_messages_stream_unprocessed " +
                "ON messaging.outbox_messages (stream_key, stream_version) " +
                "WHERE stream_key IS NOT NULL AND status <> 'Processed';",
                suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS messaging.ix_outbox_messages_stream_unprocessed;",
                suppressTransaction: true);
        }
    }
}
