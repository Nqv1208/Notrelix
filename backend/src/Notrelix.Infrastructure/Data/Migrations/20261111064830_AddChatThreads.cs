#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChatThreads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "also_sent_to_channel",
                schema: "chat",
                table: "messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "thread_sequence",
                schema: "chat",
                table: "messages",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "threads",
                schema: "chat",
                columns: table => new
                {
                    root_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    space_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reply_count = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    last_reply_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_threads", x => x.root_message_id);
                    table.ForeignKey(
                        name: "fk_threads_messages_root_message_id",
                        column: x => x.root_message_id,
                        principalSchema: "chat",
                        principalTable: "messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_threads_spaces_space_id",
                        column: x => x.space_id,
                        principalSchema: "chat",
                        principalTable: "spaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "thread_followers",
                schema: "chat",
                columns: table => new
                {
                    root_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    muted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    read_reply_sequence = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_thread_followers", x => new { x.root_message_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_thread_followers_threads_root_message_id",
                        column: x => x.root_message_id,
                        principalSchema: "chat",
                        principalTable: "threads",
                        principalColumn: "root_message_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_messages_thread_sequence",
                schema: "chat",
                table: "messages",
                columns: new[] { "thread_root_id", "thread_sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_thread_followers_user_workspace",
                schema: "chat",
                table: "thread_followers",
                columns: new[] { "user_id", "workspace_id" });

            migrationBuilder.CreateIndex(
                name: "ix_threads_space_id",
                schema: "chat",
                table: "threads",
                column: "space_id");

            migrationBuilder.CreateIndex(
                name: "ix_threads_workspace_last_reply",
                schema: "chat",
                table: "threads",
                columns: new[] { "workspace_id", "last_reply_at" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "thread_followers",
                schema: "chat");

            migrationBuilder.DropTable(
                name: "threads",
                schema: "chat");

            migrationBuilder.DropIndex(
                name: "ux_messages_thread_sequence",
                schema: "chat",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "also_sent_to_channel",
                schema: "chat",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "thread_sequence",
                schema: "chat",
                table: "messages");
        }
    }
}
