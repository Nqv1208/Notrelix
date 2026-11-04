using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBoardItemKeyAliases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "board_item_key_aliases",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_board_item_key_aliases", x => x.id);
                    table.ForeignKey(
                        name: "fk_board_item_key_aliases_board_items_item_id",
                        column: x => x.item_id,
                        principalSchema: "work",
                        principalTable: "board_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_board_item_key_aliases_item",
                schema: "work",
                table: "board_item_key_aliases",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ux_work_board_item_key_aliases_board_key",
                schema: "work",
                table: "board_item_key_aliases",
                columns: new[] { "board_id", "item_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "board_item_key_aliases",
                schema: "work");
        }
    }
}
