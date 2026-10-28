using Notrelix.Integration.Tests.Containers;
using Npgsql;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// The policy pack covers every application table, in every schema. A table without a forced policy is open
/// to the application login through the schema grants, so this is checked against the catalog of the database
/// the migrations and the pack actually produced, not against a list someone keeps by hand.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class PolicyCoverageTests
{
    private static readonly string[] OutsideRowLevelSecurity = ["ops.__EFMigrationsHistory"];

    /// <summary>Shared catalogs: no tenant or owner, readable by every request, written only by the worker.</summary>
    private static readonly string[] ApplicationReadableCatalogs =
    [
        "billing.plans", "billing.plan_prices", "billing.plan_limits", "automation.automation_templates",
    ];

    private readonly PostgresTestContainer _db;

    public PolicyCoverageTests(PostgresTestContainer db) => _db = db;

    private async Task<List<string>> QueryAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_db.OwnerConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<string>();
        while (await reader.ReadAsync())
        {
            rows.Add(reader.GetString(0));
        }

        return rows;
    }

    [Fact]
    public async Task EveryApplicationTable_HasForcedRowLevelSecurity_AndAtLeastOnePolicy()
    {
        var tables = await QueryAsync("""
            SELECT n.nspname || '.' || c.relname
              FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
             WHERE c.relkind IN ('r', 'p')
               AND n.nspname NOT LIKE 'pg\_%' AND n.nspname NOT IN ('information_schema', 'public')
             ORDER BY 1
            """);
        tables.Should().HaveCountGreaterThan(100, "the catalog scan must find the application tables, not nothing");

        var unprotected = await QueryAsync("""
            SELECT n.nspname || '.' || c.relname
              FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
             WHERE c.relkind IN ('r', 'p')
               AND n.nspname NOT LIKE 'pg\_%' AND n.nspname NOT IN ('information_schema', 'public')
               AND NOT (c.relrowsecurity AND c.relforcerowsecurity
                        AND EXISTS (SELECT 1 FROM pg_policies p WHERE p.schemaname = n.nspname AND p.tablename = c.relname))
             ORDER BY 1
            """);

        unprotected.Except(OutsideRowLevelSecurity).Should().BeEmpty(
            "every application table must have forced row-level security and a policy");
        unprotected.Should().BeSubsetOf(OutsideRowLevelSecurity);
    }

    [Fact]
    public async Task NoPolicyOfTheApplicationRole_IsUnconditional_ExceptTheNamedSharedCatalogs()
    {
        var unconditional = await QueryAsync("""
            SELECT schemaname || '.' || tablename || ' ' || policyname || ' ' || cmd
              FROM pg_policies
             WHERE 'notrelix_app' = ANY(roles)
               AND (qual = 'true' OR with_check = 'true')
             ORDER BY 1
            """);

        unconditional.Should().OnlyContain(
            row => ApplicationReadableCatalogs.Any(table => row.StartsWith(table + " ", StringComparison.Ordinal))
                   && row.EndsWith(" SELECT", StringComparison.Ordinal),
            "an unconditional application policy turns row-level security off for that table; only the shared catalogs may be read freely");
    }

    [Fact]
    public async Task SharedCatalogs_AreReadOnlyForTheApplicationRole()
    {
        var writable = await QueryAsync("""
            SELECT schemaname || '.' || tablename || ' ' || cmd
              FROM pg_policies
             WHERE 'notrelix_app' = ANY(roles) AND cmd <> 'SELECT'
               AND (schemaname || '.' || tablename) = ANY(@catalogs)
            """.Replace("@catalogs", "ARRAY['" + string.Join("','", ApplicationReadableCatalogs) + "']"));

        writable.Should().BeEmpty("the application role never writes a shared catalog");
    }
}
