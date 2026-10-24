namespace Notrelix.Infrastructure.Data.Rls;

/// <summary>
/// What the serving connection is, as the database sees it.
/// </summary>
/// <param name="CurrentUser">The effective role (<c>current_user</c>, honours <c>SET ROLE</c>).</param>
/// <param name="SessionUser">The login role (<c>session_user</c>).</param>
/// <param name="CurrentIsSuperuser">The effective role is a superuser.</param>
/// <param name="CurrentBypassesRls">The effective role has BYPASSRLS.</param>
/// <param name="SessionIsSuperuser">The login role is a superuser (it could reset a <c>SET ROLE</c>).</param>
/// <param name="SessionBypassesRls">The login role has BYPASSRLS.</param>
/// <param name="OwnsTenantTables">The effective role holds the privileges of the owner of <c>identity.users</c>
/// (<c>null</c> when that table does not exist yet).</param>
public sealed record RuntimeDatabaseRoleFacts(
    string CurrentUser,
    string SessionUser,
    bool CurrentIsSuperuser,
    bool CurrentBypassesRls,
    bool SessionIsSuperuser,
    bool SessionBypassesRls,
    bool? OwnsTenantTables);

/// <summary>
/// Startup check that the serving process does not connect with a role row-level security cannot
/// constrain. The migrate, seed and policy-apply commands use the owner connection and never run it.
/// </summary>
/// <remarks>
/// <see cref="RlsOptions.RoleEnforcement"/> decides the reaction: <c>Off</c> (default) does not open a
/// connection, <c>Warn</c> logs, <c>Enforce</c> fails startup.
/// </remarks>
public sealed class RuntimeDatabaseRoleGuard : IHostedService
{
    private const string FactsSql = """
        SELECT current_user::text,
               session_user::text,
               (SELECT r.rolsuper FROM pg_roles r WHERE r.rolname = current_user),
               (SELECT r.rolbypassrls FROM pg_roles r WHERE r.rolname = current_user),
               (SELECT r.rolsuper FROM pg_roles r WHERE r.rolname = session_user),
               (SELECT r.rolbypassrls FROM pg_roles r WHERE r.rolname = session_user),
               (SELECT pg_has_role(current_user, c.relowner, 'USAGE')
                  FROM pg_class c
                  JOIN pg_namespace n ON n.oid = c.relnamespace
                 WHERE n.nspname = 'identity' AND c.relname = 'users')
        """;

    private readonly IServiceScopeFactory _scopes;
    private readonly IOptions<RlsOptions> _options;
    private readonly ILogger<RuntimeDatabaseRoleGuard> _logger;

    public RuntimeDatabaseRoleGuard(
        IServiceScopeFactory scopes,
        IOptions<RlsOptions> options,
        ILogger<RuntimeDatabaseRoleGuard> logger)
    {
        _scopes = scopes;
        _options = options;
        _logger = logger;
    }

    /// <summary>The reasons the role is not acceptable for serving; empty when it is.</summary>
    public static IReadOnlyList<string> Evaluate(RuntimeDatabaseRoleFacts facts)
    {
        var violations = new List<string>();

        if (facts.CurrentIsSuperuser)
        {
            violations.Add($"role '{facts.CurrentUser}' is a superuser");
        }

        if (facts.CurrentBypassesRls)
        {
            violations.Add($"role '{facts.CurrentUser}' has BYPASSRLS");
        }

        if (facts.OwnsTenantTables == true)
        {
            violations.Add($"role '{facts.CurrentUser}' owns the tenant tables");
        }

        if (facts.SessionUser != facts.CurrentUser)
        {
            if (facts.SessionIsSuperuser)
            {
                violations.Add($"login role '{facts.SessionUser}' is a superuser and can leave '{facts.CurrentUser}' with RESET ROLE");
            }

            if (facts.SessionBypassesRls)
            {
                violations.Add($"login role '{facts.SessionUser}' has BYPASSRLS and can leave '{facts.CurrentUser}' with RESET ROLE");
            }
        }

        return violations;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var mode = _options.Value.RoleEnforcement;
        if (mode == RlsRoleEnforcementMode.Off)
        {
            return;
        }

        await using var scope = _scopes.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (!context.Database.IsNpgsql())
        {
            return;
        }

        var facts = await ReadFactsAsync(context, cancellationToken);
        var violations = Evaluate(facts);
        if (violations.Count == 0)
        {
            _logger.LogInformation(
                "Runtime database role '{Role}' is constrained by row-level security.", facts.CurrentUser);
            return;
        }

        var message =
            "The serving database connection is not constrained by row-level security: " +
            string.Join("; ", violations) +
            ". Connect the runtime as the non-owner application role and keep the owner connection " +
            "(ConnectionStrings:NotrelixMigrationDb) for migrate, seed and rls-apply.";

        if (mode == RlsRoleEnforcementMode.Enforce)
        {
            throw new InvalidOperationException(message);
        }

        _logger.LogWarning("{Message}", message);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task<RuntimeDatabaseRoleFacts> ReadFactsAsync(
        ApplicationDbContext context, CancellationToken ct)
    {
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        var wasClosed = connection.State != System.Data.ConnectionState.Open;
        if (wasClosed)
        {
            await connection.OpenAsync(ct);
        }

        try
        {
            await using var command = new NpgsqlCommand(FactsSql, connection);
            await using var reader = await command.ExecuteReaderAsync(ct);
            await reader.ReadAsync(ct);
            return new RuntimeDatabaseRoleFacts(
                reader.GetString(0),
                reader.GetString(1),
                !reader.IsDBNull(2) && reader.GetBoolean(2),
                !reader.IsDBNull(3) && reader.GetBoolean(3),
                !reader.IsDBNull(4) && reader.GetBoolean(4),
                !reader.IsDBNull(5) && reader.GetBoolean(5),
                reader.IsDBNull(6) ? null : reader.GetBoolean(6));
        }
        finally
        {
            if (wasClosed)
            {
                await connection.CloseAsync();
            }
        }
    }
}
