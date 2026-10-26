#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInboxDoneSnoozeAndThreadMutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "done_at",
                schema: "notifications",
                table: "notification_recipients",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "replied_at",
                schema: "notifications",
                table: "notification_recipients",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "snoozed_until",
                schema: "notifications",
                table: "notification_recipients",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "notification_thread_mutes",
                schema: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_thread_mutes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_notification_thread_mutes_thread",
                schema: "notifications",
                table: "notification_thread_mutes",
                columns: new[] { "resource_type", "resource_id" });

            migrationBuilder.CreateIndex(
                name: "ux_notification_thread_mutes_member_thread",
                schema: "notifications",
                table: "notification_thread_mutes",
                columns: new[] { "user_id", "resource_type", "resource_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_thread_mutes",
                schema: "notifications");

            migrationBuilder.DropColumn(
                name: "done_at",
                schema: "notifications",
                table: "notification_recipients");

            migrationBuilder.DropColumn(
                name: "replied_at",
                schema: "notifications",
                table: "notification_recipients");

            migrationBuilder.DropColumn(
                name: "snoozed_until",
                schema: "notifications",
                table: "notification_recipients");
        }
    }
}
