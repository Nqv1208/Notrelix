using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class M9BillingEntitlementsCapacityBackfill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "logical_operation_id",
                schema: "billing",
                table: "feature_usage_ledger",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_unlimited",
                schema: "billing",
                table: "entitlements",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // BILL-LIMIT-001: legacy rows with limit_value = 0 encoded
            // "unlimited" by convention. Backfill them to the explicit
            // representation BEFORE the zero-limit semantic flip makes a
            // numeric 0 mean "zero capacity".
            migrationBuilder.Sql(
                "UPDATE billing.entitlements SET is_unlimited = TRUE WHERE limit_value = 0;");

            migrationBuilder.CreateIndex(
                name: "ux_workspace_feature_usages_scope",
                schema: "billing",
                table: "workspace_feature_usages",
                columns: new[] { "account_id", "workspace_id", "feature_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_feature_usage_ledger_logical_operation",
                schema: "billing",
                table: "feature_usage_ledger",
                columns: new[] { "account_id", "workspace_id", "feature_code", "logical_operation_id" },
                unique: true,
                filter: "\"logical_operation_id\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_workspace_feature_usages_scope",
                schema: "billing",
                table: "workspace_feature_usages");

            migrationBuilder.DropIndex(
                name: "ux_feature_usage_ledger_logical_operation",
                schema: "billing",
                table: "feature_usage_ledger");

            migrationBuilder.DropColumn(
                name: "logical_operation_id",
                schema: "billing",
                table: "feature_usage_ledger");

            // BILL-LIMIT-001 rewind: restore legacy convention (0 = unlimited)
            // before dropping the explicit representation.
            migrationBuilder.Sql(
                "UPDATE billing.entitlements SET is_unlimited = FALSE WHERE limit_value = 0;");

            migrationBuilder.DropColumn(
                name: "is_unlimited",
                schema: "billing",
                table: "entitlements");
        }
    }
}
