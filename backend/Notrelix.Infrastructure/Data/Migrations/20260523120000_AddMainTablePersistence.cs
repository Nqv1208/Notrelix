using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260523120000_AddMainTablePersistence")]
    public partial class AddMainTablePersistence : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "color",
                table: "lists",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "#579bfc");

            migrationBuilder.AddColumn<string>(
                name: "field_values",
                table: "cards",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "color",
                table: "lists");

            migrationBuilder.DropColumn(
                name: "field_values",
                table: "cards");
        }
    }
}
