using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Additive: the item cover, repeat rule and reminder offset as nullable columns of the Item, the colour-blind
    /// label preference on the profile (default false), and two new tables, one vote per person per Item and
    /// Vote field, and the time each board member last opened an Item. Nothing existing changes and there is
    /// nothing to backfill (expand step; nothing to contract). The target model is the model snapshot at this
    /// migration. Row-level security for the new tables is applied by the RLS script run.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261030141928_AddPopupCapabilities")]
    public partial class AddPopupCapabilities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "colour_blind_labels",
                schema: "identity",
                table: "user_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "cover_attachment_id",
                schema: "work",
                table: "board_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cover_color",
                schema: "work",
                table: "board_items",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cover_style",
                schema: "work",
                table: "board_items",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "reminder_offset_minutes",
                schema: "work",
                table: "board_items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "repeat_rule",
                schema: "work",
                table: "board_items",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "board_item_views",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_viewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_board_item_views", x => x.id);
                    table.ForeignKey(
                        name: "fk_board_item_views_board_items_item_id",
                        column: x => x.item_id,
                        principalSchema: "work",
                        principalTable: "board_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "board_item_votes",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    voted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_board_item_votes", x => x.id);
                    table.ForeignKey(
                        name: "fk_board_item_votes_board_fields_field_id",
                        column: x => x.field_id,
                        principalSchema: "work",
                        principalTable: "board_fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_board_item_votes_board_items_item_id",
                        column: x => x.item_id,
                        principalSchema: "work",
                        principalTable: "board_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_board_item_views_item_user",
                schema: "work",
                table: "board_item_views",
                columns: new[] { "item_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_board_item_votes_field_id",
                schema: "work",
                table: "board_item_votes",
                column: "field_id");

            migrationBuilder.CreateIndex(
                name: "idx_board_item_votes_item_field_user",
                schema: "work",
                table: "board_item_votes",
                columns: new[] { "item_id", "field_id", "user_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "board_item_views",
                schema: "work");

            migrationBuilder.DropTable(
                name: "board_item_votes",
                schema: "work");

            migrationBuilder.DropColumn(
                name: "colour_blind_labels",
                schema: "identity",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "cover_attachment_id",
                schema: "work",
                table: "board_items");

            migrationBuilder.DropColumn(
                name: "cover_color",
                schema: "work",
                table: "board_items");

            migrationBuilder.DropColumn(
                name: "cover_style",
                schema: "work",
                table: "board_items");

            migrationBuilder.DropColumn(
                name: "reminder_offset_minutes",
                schema: "work",
                table: "board_items");

            migrationBuilder.DropColumn(
                name: "repeat_rule",
                schema: "work",
                table: "board_items");
        }
    }
}
