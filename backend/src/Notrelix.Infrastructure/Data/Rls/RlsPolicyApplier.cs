namespace Notrelix.Infrastructure.Data.Rls;

public sealed class RlsPolicyApplier
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RlsPolicyApplier> _logger;

    private static readonly string[] ScriptNames =
    [
        "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.001_roles.sql",
        "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.002_context_helpers.sql",
        "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.003_authz_access_helpers.sql",
        "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.004_policy_runtime.sql",
        "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.005_grants.sql",
        "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.006_policies_identity.sql",
        "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.007_policies_platform.sql",
        "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.008_policies_workspace_scoped_domain.sql",
        "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.009_policies_notifications_activity_search.sql",
        "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.010_policies_events_messaging_audit_ops.sql",
        "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.011_verification.sql",
        "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.012_principal_scopes.sql",
        "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.013_co_member_profiles.sql",
        "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.014_anonymous_scopes.sql",
        "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.015_internal_principals.sql",
        "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.016_policies_derived_scope.sql",
        "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.017_webhook_scope.sql",
        "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.018_billing_usage.sql",
        "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.019_policy_helpers_migrator_only.sql",
        "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.020_support_role_no_content.sql",
        "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.021_billing_and_account_writes.sql",
        "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.022_audit_append_only.sql",
        "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.023_custom_role_assignments.sql",
        "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.024_membership_end_access.sql",
        "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.025_authz_access_helpers_static.sql",
    ];

    public RlsPolicyApplier(ApplicationDbContext context, ILogger<RlsPolicyApplier> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ApplyAsync(CancellationToken ct = default)
    {
        var assembly = typeof(RlsPolicyApplier).Assembly;
        var connection = (NpgsqlConnection)_context.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        var availableResources = assembly.GetManifestResourceNames()
            .OrderBy(x => x)
            .ToArray();

        foreach (var name in availableResources)
        {
            _logger.LogInformation("Embedded resource: {ResourceName}", name);
        }

        var appliedCount = 0;

        foreach (var resourceName in ScriptNames)
        {
            await using var stream = assembly.GetManifestResourceStream(resourceName);

            if (stream is null)
            {
                throw new InvalidOperationException(
                    $"Required RLS SQL resource was not found: {resourceName}{Environment.NewLine}" +
                    $"Available embedded resources:{Environment.NewLine}{string.Join(Environment.NewLine, availableResources)}");
            }

            using var reader = new StreamReader(stream);
            var sql = await reader.ReadToEndAsync(ct);

            try
            {
                _logger.LogInformation(
                    "Executing RLS script: {Script} (length={Length}, connection={Conn})",
                    resourceName,
                    sql.Length,
                    connection.ConnectionString?.Split(';')[0]);

                await using var cmd = new NpgsqlCommand(sql, connection);
                cmd.CommandTimeout = 60;
                await cmd.ExecuteNonQueryAsync(ct);

                appliedCount++;

                _logger.LogInformation("RLS script applied: {Script}", resourceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply RLS script: {Script}", resourceName);
                throw;
            }
        }

        if (appliedCount != ScriptNames.Length)
        {
            throw new InvalidOperationException(
                $"Expected to apply {ScriptNames.Length} RLS scripts, but applied {appliedCount}.");
        }

        _logger.LogInformation("Applied {Count} RLS scripts successfully.", appliedCount);

        await VerifyPolicyCoverageAsync(connection, ct);
    }

    /// <summary>
    /// Tables that carry no tenant or person data and are intentionally outside row-level security:
    /// the migration history is read and written by the owner only.
    /// </summary>
    private static readonly string[] UnenforcedTables = ["ops.__EFMigrationsHistory"];

    /// <summary>
    /// Every base table of every application schema must have row-level security enabled AND forced and at
    /// least one policy, or the apply fails: a table without a policy is open to the application login
    /// through the schema grants. The check scans the catalog, so a table added by a later migration cannot
    /// slip past by living in a schema nobody listed.
    /// </summary>
    private async Task VerifyPolicyCoverageAsync(NpgsqlConnection connection, CancellationToken ct)
    {
        const string sql = """
            SELECT n.nspname || '.' || c.relname || ': ' ||
                   CASE WHEN NOT c.relrowsecurity THEN 'RLS_DISABLED'
                        WHEN NOT c.relforcerowsecurity THEN 'RLS_NOT_FORCED'
                        ELSE 'NO_POLICY' END
            FROM pg_class c
            JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE c.relkind IN ('r', 'p')
              AND n.nspname NOT LIKE 'pg\_%'
              AND n.nspname NOT IN ('information_schema', 'public')
              AND (n.nspname || '.' || c.relname) <> ALL(@unenforced)
              AND (c.relrowsecurity = false
                   OR c.relforcerowsecurity = false
                   OR NOT EXISTS (
                       SELECT 1 FROM pg_policies p
                       WHERE p.schemaname = n.nspname AND p.tablename = c.relname))
            ORDER BY 1
            """;

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("unenforced", UnenforcedTables);

        var issues = new List<string>();
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                issues.Add(reader.GetString(0));
            }
        }

        if (issues.Count > 0)
        {
            throw new InvalidOperationException(
                "RLS verification failed; every application table must have row-level security enabled, forced and a policy: " +
                string.Join(", ", issues));
        }

        _logger.LogInformation("RLS verification passed: every application table has forced row-level security and a policy.");
    }
}
