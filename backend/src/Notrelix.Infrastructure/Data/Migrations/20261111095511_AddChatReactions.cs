
#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChatReactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reactions",
                schema: "chat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    space_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reactions", x => x.id);
                    table.ForeignKey(
                        name: "fk_reactions_chat_spaces_space_id",
                        column: x => x.space_id,
                        principalSchema: "chat",
                        principalTable: "spaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_reactions_messages_message_id",
                        column: x => x.message_id,
                        principalSchema: "chat",
                        principalTable: "messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_reactions_message_created_at",
                schema: "chat",
                table: "reactions",
                columns: new[] { "message_id", "created_at", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_reactions_space_id",
                schema: "chat",
                table: "reactions",
                column: "space_id");

            migrationBuilder.CreateIndex(
                name: "ux_reactions_message_user_kind",
                schema: "chat",
                table: "reactions",
                columns: new[] { "message_id", "user_id", "kind" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reactions",
                schema: "chat");
        }
    }
}
