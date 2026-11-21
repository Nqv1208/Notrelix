using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFormPublicKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "public_key",
                schema: "work",
                table: "forms",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_forms_public_key",
                schema: "work",
                table: "forms",
                column: "public_key",
                unique: true,
                filter: "public_key IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_forms_public_key",
                schema: "work",
                table: "forms");

            migrationBuilder.DropColumn(
                name: "public_key",
                schema: "work",
                table: "forms");
        }
    }
}
