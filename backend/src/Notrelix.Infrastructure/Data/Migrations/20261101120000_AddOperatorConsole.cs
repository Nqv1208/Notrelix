#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOperatorConsole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "operators");

            migrationBuilder.AddColumn<string>(
                name: "family_key",
                schema: "billing",
                table: "plans",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "revision",
                schema: "billing",
                table: "plans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "tier",
                schema: "billing",
                table: "plans",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_unlimited",
                schema: "billing",
                table: "plan_limits",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "operator_accounts",
                schema: "operators",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    display_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    failed_sign_in_count = table.Column<int>(type: "integer", nullable: false),
                    locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_signed_in_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_operator_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "operator_audit",
                schema: "operators",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    operator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    target_kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    before_json = table.Column<string>(type: "jsonb", nullable: true),
                    after_json = table.Column<string>(type: "jsonb", nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_operator_audit", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_plans_family_revision",
                schema: "billing",
                table: "plans",
                columns: new[] { "family_key", "revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_plan_limits_plan_feature",
                schema: "billing",
                table: "plan_limits",
                columns: new[] { "plan_id", "feature_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_operator_accounts_email",
                schema: "operators",
                table: "operator_accounts",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_operator_audit_account_time",
                schema: "operators",
                table: "operator_audit",
                columns: new[] { "account_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "idx_operator_audit_time",
                schema: "operators",
                table: "operator_audit",
                column: "occurred_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "operator_accounts",
                schema: "operators");

            migrationBuilder.DropTable(
                name: "operator_audit",
                schema: "operators");

            migrationBuilder.DropIndex(
                name: "ux_plans_family_revision",
                schema: "billing",
                table: "plans");

            migrationBuilder.DropIndex(
                name: "ux_plan_limits_plan_feature",
                schema: "billing",
                table: "plan_limits");

            migrationBuilder.DropColumn(
                name: "family_key",
                schema: "billing",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "revision",
                schema: "billing",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "tier",
                schema: "billing",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "is_unlimited",
                schema: "billing",
                table: "plan_limits");
        }
    }
}
