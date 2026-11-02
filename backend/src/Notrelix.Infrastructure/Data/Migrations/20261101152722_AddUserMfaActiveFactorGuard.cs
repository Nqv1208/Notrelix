#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <summary>
    /// One active factor of a kind per person (UC-IDN-H02): two pending methods activated in parallel could both
    /// become active. A person who already holds duplicates keeps the earliest-verified one; the rest are disabled
    /// (never deleted) so the index can be created.
    /// </summary>
    public partial class AddUserMfaActiveFactorGuard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE identity.user_mfa_methods m
SET status = 'Disabled', is_primary = false, disabled_at = now(), version = version + 1
WHERE m.status = 'Active'
  AND EXISTS (
      SELECT 1 FROM identity.user_mfa_methods o
      WHERE o.user_id = m.user_id AND o.type = m.type AND o.status = 'Active'
        AND (COALESCE(o.verified_at, 'epoch'::timestamptz), o.id) < (COALESCE(m.verified_at, 'epoch'::timestamptz), m.id));");

            migrationBuilder.CreateIndex(
                name: "ux_user_mfa_methods_active_per_type",
                schema: "identity",
                table: "user_mfa_methods",
                columns: new[] { "user_id", "type" },
                unique: true,
                filter: "status = 'Active'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_user_mfa_methods_active_per_type",
                schema: "identity",
                table: "user_mfa_methods");
        }
    }
}
