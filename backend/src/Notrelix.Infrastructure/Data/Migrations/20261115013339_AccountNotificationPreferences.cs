using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AccountNotificationPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // One matrix per person: keep each user's most recently updated set (its entries stay through the
            // set_id cascade), drop the other workspaces' sets, and clear the entries' workspace scope.
            migrationBuilder.Sql("""
                DELETE FROM notifications.notification_preference_sets s
                USING (
                    SELECT id,
                           row_number() OVER (
                               PARTITION BY user_id
                               ORDER BY COALESCE(updated_at, created_at) DESC, created_at DESC, id DESC) AS rn
                    FROM notifications.notification_preference_sets) ranked
                WHERE s.id = ranked.id AND ranked.rn > 1;

                UPDATE notifications.notification_preferences
                SET workspace_id = NULL
                WHERE set_id IS NOT NULL;
                """);

            migrationBuilder.DropIndex(
                name: "ux_notification_preference_sets_member",
                schema: "notifications",
                table: "notification_preference_sets");

            migrationBuilder.DropColumn(
                name: "account_id",
                schema: "notifications",
                table: "notification_preference_sets");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                schema: "notifications",
                table: "notification_preference_sets");

            migrationBuilder.CreateIndex(
                name: "ux_notification_preference_sets_user",
                schema: "notifications",
                table: "notification_preference_sets",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_notification_preference_sets_user",
                schema: "notifications",
                table: "notification_preference_sets");

            migrationBuilder.AddColumn<Guid>(
                name: "account_id",
                schema: "notifications",
                table: "notification_preference_sets",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                schema: "notifications",
                table: "notification_preference_sets",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ux_notification_preference_sets_member",
                schema: "notifications",
                table: "notification_preference_sets",
                columns: new[] { "workspace_id", "user_id" },
                unique: true);
        }
    }
}
