#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <summary>
    /// The paged board read orders a group's Items by the member's sort and filters them in the database. The title
    /// sort must match the web client's collator (Vietnamese, accent- and case-insensitive, digits by value), which no
    /// built-in collation does, so an ICU collation with that behaviour is created. Two partial indexes serve the sort
    /// keys that live on the Item row: the due date and the title, each led by the group so a page is an index range.
    /// The default order (position) already has ux_work_board_items_group_position_active. Custom field sorts read
    /// the (item_id, field_id) value row and sort within the group. Raw SQL like the other secondary indexes, so the
    /// EF model does not change and this migration carries no target model. Additive; nothing to backfill or contract.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261031120000_AddBoardItemPagedReadIndexes")]
    public partial class AddBoardItemPagedReadIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_collation c JOIN pg_namespace n ON n.oid = c.collnamespace WHERE n.nspname = 'work' AND c.collname = 'nrx_text_sort') THEN " +
                "CREATE COLLATION work.nrx_text_sort (provider = icu, locale = 'vi-u-ks-level1-kn-true', deterministic = false); END IF; END $$;");
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS ix_board_items_group_due ON work.board_items (group_id, due_at, \"position\", id) " +
                "WHERE deleted_at IS NULL AND parent_item_id IS NULL AND NOT is_archived;");
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS ix_board_items_group_title ON work.board_items (group_id, (NULLIF(name, '') COLLATE work.nrx_text_sort), \"position\", id) " +
                "WHERE deleted_at IS NULL AND parent_item_id IS NULL AND NOT is_archived;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS work.ix_board_items_group_title;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS work.ix_board_items_group_due;");
            migrationBuilder.Sql("DROP COLLATION IF EXISTS work.nrx_text_sort;");
        }
    }
}
