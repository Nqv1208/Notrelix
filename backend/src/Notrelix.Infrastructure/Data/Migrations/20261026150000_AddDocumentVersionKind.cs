#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Additive: a history entry says why it exists (automatic, named or restore). Existing rows were all taken
    /// by the system, so the column starts as Automatic (expand step; nothing to backfill or contract). The
    /// target model is the model snapshot at this migration.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261026150000_AddDocumentVersionKind")]
    public partial class AddDocumentVersionKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "kind",
                schema: "docs",
                table: "document_versions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Automatic");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "kind", schema: "docs", table: "document_versions");
        }
    }
}
