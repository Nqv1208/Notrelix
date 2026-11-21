
#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountSso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_account_domains_domain",
                schema: "account",
                table: "account_domains");

            migrationBuilder.AddColumn<string>(
                name: "client_id",
                schema: "account",
                table: "account_identity_providers",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "client_secret_protected",
                schema: "account",
                table: "account_identity_providers",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "validation_error",
                schema: "account",
                table: "account_identity_providers",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "verification_token_protected",
                schema: "account",
                table: "account_domains",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_account_identity_providers_account",
                schema: "account",
                table: "account_identity_providers",
                column: "account_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_account_domains_account_domain",
                schema: "account",
                table: "account_domains",
                columns: new[] { "account_id", "domain" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_account_domains_domain_verified",
                schema: "account",
                table: "account_domains",
                column: "domain",
                unique: true,
                filter: "verification_status = 'Verified'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_account_identity_providers_account",
                schema: "account",
                table: "account_identity_providers");

            migrationBuilder.DropIndex(
                name: "idx_account_domains_account_domain",
                schema: "account",
                table: "account_domains");

            migrationBuilder.DropIndex(
                name: "idx_account_domains_domain_verified",
                schema: "account",
                table: "account_domains");

            migrationBuilder.DropColumn(
                name: "client_id",
                schema: "account",
                table: "account_identity_providers");

            migrationBuilder.DropColumn(
                name: "client_secret_protected",
                schema: "account",
                table: "account_identity_providers");

            migrationBuilder.DropColumn(
                name: "validation_error",
                schema: "account",
                table: "account_identity_providers");

            migrationBuilder.DropColumn(
                name: "verification_token_protected",
                schema: "account",
                table: "account_domains");

            migrationBuilder.CreateIndex(
                name: "idx_account_domains_domain",
                schema: "account",
                table: "account_domains",
                column: "domain",
                unique: true);
        }
    }
}
