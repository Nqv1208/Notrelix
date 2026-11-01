#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Two live blocks of one page may not share a sort key under the same parent (a root block has no parent).
    /// Block order is decided in the application from the page's block set, so two writers reading it together could
    /// place two blocks on one key and later break every insert between them. The guard is a deferrable exclusion
    /// constraint rather than a unique index: a write that moves blocks onto keys another block of the same write
    /// leaves (a swap, a restored version) is judged on its committed result, at commit. A key that appears twice
    /// today aborts the migration before any change (it never rewrites keys); pre-production databases are reset.
    /// No model change, so the model snapshot stays as it is.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261101090000_AddDocumentBlockSiblingOrderGuard")]
    public partial class AddDocumentBlockSiblingOrderGuard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $preflight$
                DECLARE
                    duplicates bigint;
                BEGIN
                    SELECT count(*) INTO duplicates FROM (
                        SELECT 1 FROM docs.blocks
                        WHERE deleted_at IS NULL
                        GROUP BY page_id, COALESCE(parent_id, '00000000-0000-0000-0000-000000000000'::uuid), position
                        HAVING count(*) > 1
                    ) d;
                    IF duplicates > 0 THEN
                        RAISE EXCEPTION 'DOCS_BLOCK_ORDER_PREFLIGHT_FAILED: % sibling sort key(s) are used by more than one live block of a page', duplicates;
                    END IF;
                END
                $preflight$;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE docs.blocks
                    ADD CONSTRAINT ex_docs_blocks_sibling_position
                    EXCLUDE USING btree (
                        page_id WITH =,
                        (COALESCE(parent_id, '00000000-0000-0000-0000-000000000000'::uuid)) WITH =,
                        position WITH =)
                    WHERE (deleted_at IS NULL)
                    DEFERRABLE INITIALLY DEFERRED;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE docs.blocks DROP CONSTRAINT ex_docs_blocks_sibling_position;");
        }
    }
}
