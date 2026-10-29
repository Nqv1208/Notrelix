#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <summary>
    /// No schema change: the form question config lives in the existing json column "config". Until now the model
    /// mapped none of its properties, so the stored config was always an empty object (required, limits, texts were
    /// never saved). The model now maps them, plus the options list of unmapped choice questions; rows already stored
    /// as an empty object read back with defaults. Only the model snapshot changes.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261029110000_MapFormQuestionConfigProperties")]
    public partial class MapFormQuestionConfigProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
