using Npgsql;

namespace Notrelix.Integration.Tests.Rls;

public sealed partial class DatabaseAccessTighteningTests
{
    [Fact]
    public async Task SupportRole_CannotReadCredentialsOrTenantContent()
    {
        await using var c = new NpgsqlConnection(_fixture.OwnerConnectionString);
        await c.OpenAsync();
        await ExecAsync(c, null, "SET ROLE notrelix_support_readonly");

        foreach (var table in new[]
                 {
                     "identity.users", "identity.user_sessions", "identity.mfa_recovery_codes", "identity.oauth_accounts",
                     "identity.api_tokens", "identity.password_reset_tokens", "integration.integration_secret_blobs",
                     "work.boards", "work.board_items", "docs.pages", "collab.comments", "collab.attachments",
                 })
        {
            var read = async () => await ExecAsync(c, null, $"SELECT count(*) FROM {table}");
            (await read.Should().ThrowAsync<PostgresException>(table)).Which.SqlState.Should().Be(Denied, table);
        }

        await ExecAsync(c, null, "RESET ROLE");

        // Generic: no table of the database is readable by the role except the shared catalogs.
        var readable = await OwnerScalarAsync("""
            SELECT coalesce(string_agg(n.nspname || '.' || c.relname, ',' ORDER BY n.nspname, c.relname), '')
              FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
             WHERE c.relkind IN ('r', 'p', 'v', 'm')
               AND n.nspname NOT IN ('pg_catalog', 'information_schema', 'pg_toast')
               AND has_any_column_privilege('notrelix_support_readonly', c.oid, 'SELECT')
            """);
        readable.Split(',', StringSplitOptions.RemoveEmptyEntries).Should().BeEquivalentTo(
            ["automation.automation_templates", "billing.plan_limits", "billing.plan_prices", "billing.plans"],
            "support keeps only shared catalogs until each case type is designed");
    }
}
