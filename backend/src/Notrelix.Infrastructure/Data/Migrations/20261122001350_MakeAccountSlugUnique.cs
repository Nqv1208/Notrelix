using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeAccountSlugUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Until now two Accounts could share a slug (every personal Account was named from the display name
            // alone). The oldest keeps its slug; each later one gets its own id as a tail, so the unique
            // index below can be built on data that already holds duplicates. Nothing else is rewritten.
            migrationBuilder.Sql("""
                UPDATE account.accounts a
                SET slug = left(a.slug, 95) || '-' || replace(a.id::text, '-', '')
                FROM (
                    SELECT id, row_number() OVER (PARTITION BY slug ORDER BY created_at NULLS FIRST, id) AS rn
                    FROM account.accounts
                ) d
                WHERE d.id = a.id AND d.rn > 1;
                """);

            migrationBuilder.DropIndex(
                name: "idx_account_slug",
                schema: "account",
                table: "accounts");

            migrationBuilder.CreateIndex(
                name: "idx_account_slug",
                schema: "account",
                table: "accounts",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_account_slug",
                schema: "account",
                table: "accounts");

            migrationBuilder.CreateIndex(
                name: "idx_account_slug",
                schema: "account",
                table: "accounts",
                column: "slug");
        }
    }
}
