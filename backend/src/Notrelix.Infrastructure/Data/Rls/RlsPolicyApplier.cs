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

        await VerifyEnforcedSchemasAsync(connection, ct);
    }

    /// <summary>
    /// Schemas whose RLS coverage is certified and therefore fails closed: every
    /// base table must have RLS enabled and at least one policy, or the apply fails.
    /// A schema is added here once its owning context has classified every table
    /// (work: WM-V3-DATA-003). Other target schemas are still reported by
    /// 011_verification.sql without failing the apply.
    /// </summary>
    private static readonly string[] EnforcedSchemas = ["work"];

    private async Task VerifyEnforcedSchemasAsync(NpgsqlConnection connection, CancellationToken ct)
    {
        const string sql = """
            SELECT n.nspname || '.' || c.relname || ': ' ||
                   CASE WHEN c.relrowsecurity THEN 'NO_POLICY' ELSE 'RLS_DISABLED' END
            FROM pg_class c
            JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE c.relkind = 'r'
              AND n.nspname = ANY(@schemas)
              AND (c.relrowsecurity = false
                   OR NOT EXISTS (
                       SELECT 1 FROM pg_policies p
                       WHERE p.schemaname = n.nspname AND p.tablename = c.relname))
            ORDER BY 1
            """;

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("schemas", EnforcedSchemas);

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
                "RLS verification failed for enforced schemas; every table must have RLS enabled and a policy: " +
                string.Join(", ", issues));
        }

        _logger.LogInformation("RLS verification passed for enforced schemas: {Schemas}", string.Join(", ", EnforcedSchemas));
    }
}
