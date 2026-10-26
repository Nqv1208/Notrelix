#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Additive: a new table holding, per Item and sender, when the people in charge were last reminded. Nothing
    /// existing changes and there is nothing to backfill (expand step; nothing to contract). The target model is
    /// the model snapshot at this migration. Row-level security is applied by the RLS script run.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261027120000_AddBoardItemReminders")]
    public partial class AddBoardItemReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "board_item_reminders",
                schema: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reminded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_board_item_reminders", x => x.id);
                    table.ForeignKey(
                        name: "fk_board_item_reminders_board_items_item_id",
                        column: x => x.item_id,
                        principalSchema: "work",
                        principalTable: "board_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_board_item_reminders_item_sender",
                schema: "work",
                table: "board_item_reminders",
                columns: new[] { "item_id", "sender_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_board_item_reminders_sender",
                schema: "work",
                table: "board_item_reminders",
                column: "sender_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "board_item_reminders", schema: "work");
        }
    }
}
