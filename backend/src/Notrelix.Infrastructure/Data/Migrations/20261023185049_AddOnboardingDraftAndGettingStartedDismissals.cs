#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOnboardingDraftAndGettingStartedDismissals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "getting_started_dismissals",
                schema: "workspace",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dismissed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_getting_started_dismissals", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "onboarding_drafts",
                schema: "workspace",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    data = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_onboarding_drafts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_getting_started_dismissals_member",
                schema: "workspace",
                table: "getting_started_dismissals",
                columns: new[] { "workspace_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_drafts_expires",
                schema: "workspace",
                table: "onboarding_drafts",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ux_onboarding_drafts_user",
                schema: "workspace",
                table: "onboarding_drafts",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "getting_started_dismissals",
                schema: "workspace");

            migrationBuilder.DropTable(
                name: "onboarding_drafts",
                schema: "workspace");
        }
    }
}
