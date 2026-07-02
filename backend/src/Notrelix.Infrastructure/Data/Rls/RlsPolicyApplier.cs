namespace Notrelix.Infrastructure.Data.Rls;

public sealed class RlsPolicyApplier
{
    private readonly DbContext _context;
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
    ];

    public RlsPolicyApplier(DbContext context, ILogger<RlsPolicyApplier> logger)
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
    }
}
