#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationPreferenceSets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "quiet_hours_json",
                schema: "notifications",
                table: "notification_preferences",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb",
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AddColumn<Guid>(
                name: "set_id",
                schema: "notifications",
                table: "notification_preferences",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "notification_preference_sets",
                schema: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_preference_sets", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_notification_preferences_set_id",
                schema: "notifications",
                table: "notification_preferences",
                column: "set_id");

            migrationBuilder.CreateIndex(
                name: "ux_notification_preference_sets_member",
                schema: "notifications",
                table: "notification_preference_sets",
                columns: new[] { "workspace_id", "user_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_preferences_notification_preference_sets_set_id",
                schema: "notifications",
                table: "notification_preferences",
                column: "set_id",
                principalSchema: "notifications",
                principalTable: "notification_preference_sets",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_notification_preferences_notification_preference_sets_set_id",
                schema: "notifications",
                table: "notification_preferences");

            migrationBuilder.DropTable(
                name: "notification_preference_sets",
                schema: "notifications");

            migrationBuilder.DropIndex(
                name: "ix_notification_preferences_set_id",
                schema: "notifications",
                table: "notification_preferences");

            migrationBuilder.DropColumn(
                name: "set_id",
                schema: "notifications",
                table: "notification_preferences");

            migrationBuilder.AlterColumn<string>(
                name: "quiet_hours_json",
                schema: "notifications",
                table: "notification_preferences",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldDefaultValueSql: "'{}'::jsonb");
        }
    }
}
