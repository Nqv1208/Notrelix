#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class M10PlacementRowVersion : Migration
    {
        /// <inheritdoc />
        /// <remarks>
        /// The PostgreSQL xmin concurrency token needs NO DDL: xmin is a system
        /// column that already exists on every table, so the designer-emitted
        /// AddColumn/DropColumn were removed (documented Npgsql pattern). The
        /// migration records the model change (row-version mapping on the
        /// placement projection) with an empty Up/Down.
        /// </remarks>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
