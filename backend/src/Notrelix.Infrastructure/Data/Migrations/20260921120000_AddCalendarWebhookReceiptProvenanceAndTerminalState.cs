#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260921120000_AddCalendarWebhookReceiptProvenanceAndTerminalState")]
    public partial class AddCalendarWebhookReceiptProvenanceAndTerminalState : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing receipt rows may be legacy/unbound. Keep the new
            // provenance nullable for historical rows; the Capture factory
            // requires all three identities for every new accepted receipt.
            migrationBuilder.AddColumn<Guid>(
                name: "account_id",
                schema: "integration",
                table: "inbound_webhook_receipts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                schema: "integration",
                table: "inbound_webhook_receipts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "terminal_at",
                schema: "integration",
                table: "inbound_webhook_receipts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "failure_code",
                schema: "integration",
                table: "inbound_webhook_receipts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "failure_detail",
                schema: "integration",
                table: "inbound_webhook_receipts",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "account_id", schema: "integration", table: "inbound_webhook_receipts");
            migrationBuilder.DropColumn(name: "workspace_id", schema: "integration", table: "inbound_webhook_receipts");
            migrationBuilder.DropColumn(name: "terminal_at", schema: "integration", table: "inbound_webhook_receipts");
            migrationBuilder.DropColumn(name: "failure_code", schema: "integration", table: "inbound_webhook_receipts");
            migrationBuilder.DropColumn(name: "failure_detail", schema: "integration", table: "inbound_webhook_receipts");
        }
    }
}
