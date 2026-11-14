
#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShareLinkLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "level",
                schema: "governance",
                table: "share_links",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Viewer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "level",
                schema: "governance",
                table: "share_links");
        }
    }
}
