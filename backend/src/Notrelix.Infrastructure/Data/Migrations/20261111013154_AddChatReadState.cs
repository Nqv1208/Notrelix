#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChatReadState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "mention_count",
                schema: "chat",
                table: "memberships",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "muted_until",
                schema: "chat",
                table: "memberships",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "read_sequence",
                schema: "chat",
                table: "memberships",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "idx_memberships_user_workspace_active",
                schema: "chat",
                table: "memberships",
                columns: new[] { "user_id", "workspace_id" },
                filter: "left_at IS NULL")
                .Annotation("Npgsql:IndexInclude", new[] { "space_id", "read_sequence" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_memberships_user_workspace_active",
                schema: "chat",
                table: "memberships");

            migrationBuilder.DropColumn(
                name: "mention_count",
                schema: "chat",
                table: "memberships");

            migrationBuilder.DropColumn(
                name: "muted_until",
                schema: "chat",
                table: "memberships");

            migrationBuilder.DropColumn(
                name: "read_sequence",
                schema: "chat",
                table: "memberships");
        }
    }
}
