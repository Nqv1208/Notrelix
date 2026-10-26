using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Notrelix.Infrastructure.Data.Documents;

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Data-only step: the EF model does not change (the <c>legacy_content</c> column is a retention column
    /// that no entity maps), so this migration carries no target model of its own.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261026090000_UpgradeBlocksToRichTextV1")]
    public partial class UpgradeBlocksToRichTextV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing blocks become rich text v1; what a block held before stays in legacy_content.
            migrationBuilder.Sql(BlockRichTextMigrationSql.AddLegacyColumn);
            migrationBuilder.Sql(BlockRichTextMigrationSql.CreateUpgradeFunction);
            migrationBuilder.Sql(BlockRichTextMigrationSql.Backfill);
            migrationBuilder.Sql(BlockRichTextMigrationSql.DropUpgradeFunction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Blocks the upgrade rewrote get their previous content and properties back; blocks written
            // as rich text v1 afterwards have no previous shape and keep it.
            migrationBuilder.Sql(BlockRichTextMigrationSql.Restore);
            migrationBuilder.Sql(BlockRichTextMigrationSql.DropLegacyColumn);
        }
    }
}
