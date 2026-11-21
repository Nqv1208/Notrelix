#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChatCalls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "call_id",
                schema: "chat",
                table: "messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "calls",
                schema: "chat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    space_id = table.Column<Guid>(type: "uuid", nullable: false),
                    space_kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    initiator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    state = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    end_reason = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    room_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    region = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    answered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    empty_since = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_calls", x => x.id);
                    table.ForeignKey(
                        name: "fk_calls_chat_spaces_space_id",
                        column: x => x.space_id,
                        principalSchema: "chat",
                        principalTable: "spaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "call_participants",
                schema: "chat",
                columns: table => new
                {
                    call_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    left_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    audio_on = table.Column<bool>(type: "boolean", nullable: false),
                    video_on = table.Column<bool>(type: "boolean", nullable: false),
                    sharing_screen = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_call_participants", x => new { x.call_id, x.person_id });
                    table.ForeignKey(
                        name: "fk_call_participants_calls_call_id",
                        column: x => x.call_id,
                        principalSchema: "chat",
                        principalTable: "calls",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_messages_space_call",
                schema: "chat",
                table: "messages",
                columns: new[] { "space_id", "call_id" },
                unique: true,
                filter: "call_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_call_participants_person_live",
                schema: "chat",
                table: "call_participants",
                column: "person_id",
                unique: true,
                filter: "left_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_calls_live_started_at",
                schema: "chat",
                table: "calls",
                column: "started_at",
                filter: "state IN ('ringing','active')");

            migrationBuilder.CreateIndex(
                name: "ix_calls_space_started_at",
                schema: "chat",
                table: "calls",
                columns: new[] { "space_id", "started_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ux_calls_room_key",
                schema: "chat",
                table: "calls",
                column: "room_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_calls_space_live",
                schema: "chat",
                table: "calls",
                column: "space_id",
                unique: true,
                filter: "state IN ('ringing','active')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "call_participants",
                schema: "chat");

            migrationBuilder.DropTable(
                name: "calls",
                schema: "chat");

            migrationBuilder.DropIndex(
                name: "ux_messages_space_call",
                schema: "chat",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "call_id",
                schema: "chat",
                table: "messages");
        }
    }
}
