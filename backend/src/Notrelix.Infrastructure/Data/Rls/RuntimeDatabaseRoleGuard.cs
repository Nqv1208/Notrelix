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
/// <param name="MemberOfWorkerRole">The login role is a member of <c>notrelix_worker</c> (the role the unbounded
/// worker scope is honoured for).</param>
public sealed record RuntimeDatabaseRoleFacts(
    string CurrentUser,
    string SessionUser,
    bool CurrentIsSuperuser,
    bool CurrentBypassesRls,
    bool SessionIsSuperuser,
    bool SessionBypassesRls,
    bool? OwnsTenantTables,
    bool MemberOfWorkerRole = false);

/// <summary>
/// Startup check that the serving process does not connect with a role row-level security cannot
/// constrain. The migrate, seed and policy-apply commands use the owner connection and never run it.
/// </summary>
/// <remarks>
/// <see cref="RlsOptions.RoleEnforcement"/> decides the reaction: <c>Off</c> does not open a connection,
/// <c>Warn</c> logs, <c>Enforce</c> (the default of every environment) fails startup. Both database logins
/// are checked: the application login (requests) must be constrained and must NOT be a member of
/// <c>notrelix_worker</c>, and the background login (dispatchers, consumers, sweeps) must be constrained
/// and BE a member of it. A background login that is just the application login (no separate
/// <c>ConnectionStrings:NotrelixWorkerDb</c>) is a violation too, because the worker scope is honoured only
/// for members of <c>notrelix_worker</c>.
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
                 WHERE n.nspname = 'identity' AND c.relname = 'users'),
               COALESCE((SELECT pg_has_role(session_user, r.oid, 'MEMBER')
                           FROM pg_roles r WHERE r.rolname = 'notrelix_worker'), false)
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

        if (facts.MemberOfWorkerRole)
        {
            violations.Add($"login role '{facts.SessionUser}' is a member of notrelix_worker, so the application connection could assert the unbounded worker scope");
        }

        return violations;
    }

    /// <summary>The reasons the background login is not acceptable; empty when it is.</summary>
    public static IReadOnlyList<string> EvaluateWorker(RuntimeDatabaseRoleFacts facts)
    {
        // Same constraints as the application login (not a superuser, not BYPASSRLS, not the table owner,
        // no way back to a privileged role), except that it must BE the worker role member.
        var violations = Evaluate(facts with { MemberOfWorkerRole = false }).ToList();

        if (!facts.MemberOfWorkerRole)
        {
            violations.Add($"background login '{facts.SessionUser}' is not a member of notrelix_worker, so the worker scope would not be honoured for it");
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

        var violations = new List<string>();
        string? appRole = null;
        DatabaseConnectionStrings? connectionStrings;

        await using (var scope = _scopes.CreateAsyncScope())
        {
            connectionStrings = scope.ServiceProvider.GetService<DatabaseConnectionStrings>();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            if (!context.Database.IsNpgsql())
            {
                return;
            }

            var facts = await ReadFactsAsync(context, cancellationToken);
            appRole = facts.CurrentUser;
            violations.AddRange(Evaluate(facts).Select(v => "application connection: " + v));
        }

        string? workerRole = null;
        if (connectionStrings is { WorkerUsesApplicationFallback: true })
        {
            violations.Add(
                "background connection: ConnectionStrings:NotrelixWorkerDb is not set, so background work would run " +
                "as the application login");
        }
        else if (connectionStrings is not null)
        {
            await using var workerScope = _scopes.CreateWorkerAsyncScope();
            var context = workerScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var facts = await ReadFactsAsync(context, cancellationToken);
            workerRole = facts.CurrentUser;
            violations.AddRange(EvaluateWorker(facts).Select(v => "background connection: " + v));
        }

        // The operator login is optional (no string, no console). When configured it must be the dedicated
        // login: constrained like the others, never the worker, and never an application or owner login.
        string? operatorRole = null;
        if (connectionStrings is { Operator: not null })
        {
            await using var operatorScope = _scopes.CreateOperatorAsyncScope();
            var context = operatorScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var facts = await ReadFactsAsync(context, cancellationToken);
            operatorRole = facts.CurrentUser;
            violations.AddRange(Evaluate(facts).Select(v => "operator connection: " + v));
            if (facts.SessionUser != "notrelix_operator")
            {
                violations.Add($"operator connection: login '{facts.SessionUser}' is not notrelix_operator");
            }
        }

        if (violations.Count == 0)
        {
            _logger.LogInformation(
                "Runtime database roles are constrained by row-level security: application '{AppRole}', background '{WorkerRole}', operator '{OperatorRole}'.",
                appRole, workerRole, operatorRole ?? "(console off)");
            return;
        }

        var message =
            "The serving database connections are not what row-level security requires: " +
            string.Join("; ", violations) +
            ". Connect requests as the non-owner application login (ConnectionStrings:NotrelixDb), background work as " +
            "the worker login (ConnectionStrings:NotrelixWorkerDb) and keep the owner connection " +
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
                reader.IsDBNull(6) ? null : reader.GetBoolean(6),
                !reader.IsDBNull(7) && reader.GetBoolean(7));
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
