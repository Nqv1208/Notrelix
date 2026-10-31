using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Custom roles become Account-scoped definitions assigned per workspace (owner decision 2026-10-31):
    /// the role loses its workspace, gains its fixed built-in base, a person holds one role per workspace,
    /// and every role change leaves an append-only audit fact. No custom role could be created before this
    /// change (no endpoint), so no stored role is rewritten. The row-level security policies that name the
    /// dropped column are removed first and re-applied by the policy scripts.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261031120000_AddCustomRoleAccountScope")]
    public partial class AddCustomRoleAccountScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF to_regprocedure('ops.drop_all_policies_for_table(text,text)') IS NOT NULL THEN
        PERFORM ops.drop_all_policies_for_table('governance', 'custom_role_permissions');
        PERFORM ops.drop_all_policies_for_table('governance', 'custom_roles');
    END IF;
END $$;");

            migrationBuilder.DropIndex(name: "idx_custom_roles_workspace_id", schema: "governance", table: "custom_roles");
            migrationBuilder.DropColumn(name: "workspace_id", schema: "governance", table: "custom_roles");
            migrationBuilder.AddColumn<string>(
                name: "base_role", schema: "governance", table: "custom_roles",
                type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Member");
            migrationBuilder.Sql("ALTER TABLE governance.custom_roles ALTER COLUMN base_role DROP DEFAULT;");
            migrationBuilder.CreateIndex(name: "idx_custom_roles_account_id", schema: "governance", table: "custom_roles", column: "account_id");
            migrationBuilder.Sql("CREATE UNIQUE INDEX ux_custom_roles_account_name ON governance.custom_roles (account_id, lower(name));");

            migrationBuilder.DropIndex(name: "idx_member_role_assignments_unique", schema: "governance", table: "member_role_assignments");
            migrationBuilder.CreateIndex(
                name: "idx_member_role_assignments_unique", schema: "governance", table: "member_role_assignments",
                columns: new[] { "workspace_id", "member_id" }, unique: true);

            migrationBuilder.CreateTable(
                name: "custom_role_changes",
                schema: "governance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subject_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_custom_role_changes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_custom_role_changes_role", schema: "governance", table: "custom_role_changes",
                columns: new[] { "account_id", "role_id", "occurred_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "custom_role_changes", schema: "governance");
            migrationBuilder.DropIndex(name: "idx_member_role_assignments_unique", schema: "governance", table: "member_role_assignments");
            migrationBuilder.CreateIndex(
                name: "idx_member_role_assignments_unique", schema: "governance", table: "member_role_assignments",
                columns: new[] { "member_id", "custom_role_id" }, unique: true);
            migrationBuilder.Sql("DROP INDEX IF EXISTS governance.ux_custom_roles_account_name;");
            migrationBuilder.DropIndex(name: "idx_custom_roles_account_id", schema: "governance", table: "custom_roles");
            migrationBuilder.DropColumn(name: "base_role", schema: "governance", table: "custom_roles");
            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id", schema: "governance", table: "custom_roles", type: "uuid", nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
            migrationBuilder.CreateIndex(name: "idx_custom_roles_workspace_id", schema: "governance", table: "custom_roles", column: "workspace_id");
        }
    }
}
