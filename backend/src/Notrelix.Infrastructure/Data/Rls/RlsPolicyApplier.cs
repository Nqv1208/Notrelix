namespace Notrelix.Infrastructure.Data.Rls;

public sealed class RlsPolicyApplier
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RlsPolicyApplier> _logger;

    private static readonly string[] ScriptNames =
    [
        "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.schema-v2-rls-policy-pack.sql",
        // "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.001_roles.sql",
        // "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.002_helpers.sql",
        // "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.003_authz_projection.sql",
        // "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.004_grants.sql",
        // "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.005_policies_identity.sql",
        // "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.006_policies_workspace_governance_authz.sql",
        // "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.007_policies_workspace_scoped_domain.sql",
        // "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.008_policies_events.sql",
        // "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.009_policies_messaging.sql",
        // "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.010_policies_notifications.sql",
        // "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.011_policies_activity.sql",
        // "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.012_policies_audit.sql",
        // "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.013_policies_projection.sql",
        // "Notrelix.Infrastructure.Data.Rls.RlsSqlScripts.014_policies_ops.sql",
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
            await connection.OpenAsync(ct);

        foreach (var resourceName in ScriptNames)
        {
            await using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                _logger.LogWarning("RLS SQL resource '{Resource}' not found. Skipping.", resourceName);
                continue;
            }

            using var reader = new StreamReader(stream);
            var sql = await reader.ReadToEndAsync(ct);

            try
            {
                _logger.LogInformation("Executing RLS script: {Script} (length={Length}, connection={Conn})",
                    resourceName, sql.Length, connection.ConnectionString?.Split(';')[0]);
                await using var cmd = new NpgsqlCommand(sql, connection);
                cmd.CommandTimeout = 60;
                await cmd.ExecuteNonQueryAsync(ct);
                _logger.LogInformation("RLS script applied: {Script}", resourceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply RLS script: {Script}", resourceName);
                throw;
            }
        }
    }
}
