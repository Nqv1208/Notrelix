namespace Notrelix.Infrastructure.Data.Rls;

/// <summary>A database login the policy pack expects, with the password its connection string carries.</summary>
public sealed record LoginRoleCredential(string RoleName, string Password)
{
    public override string ToString() => $"LoginRoleCredential {{ RoleName = {RoleName} }}";
}

/// <summary>
/// Creates the two runtime logins (<c>notrelix_app</c> and <c>notrelix_worker</c>) as LOGIN roles with the
/// passwords their own connection strings carry, so the secret that serves the connection is the single
/// source of the role's password. Runs as the owner during <c>--rls-apply</c> and is idempotent: every run
/// re-asserts the attributes and the password.
/// </summary>
/// <remarks>
/// The roles are created NOLOGIN by <c>001_roles.sql</c> and receive their grants from the policy pack; this
/// step only turns them into logins. Passwords are quoted by the server (<c>format('%L')</c>) and never logged.
/// </remarks>
public static class RlsLoginRoleProvisioner
{
    public const string ApplicationRoleName = "notrelix_app";
    public const string WorkerRoleName = "notrelix_worker";

    /// <summary>
    /// The credentials carried by <c>ConnectionStrings:NotrelixDb</c> and <c>ConnectionStrings:NotrelixWorkerDb</c>.
    /// Throws when either string is missing, names another login, has no password, or both name the same login.
    /// </summary>
    public static IReadOnlyList<LoginRoleCredential> ResolveCredentials(IConfiguration configuration) =>
    [
        FromConnectionString(configuration, DatabaseConnectionStrings.ApplicationConnectionStringName, ApplicationRoleName),
        FromConnectionString(configuration, DatabaseConnectionStrings.WorkerConnectionStringName, WorkerRoleName),
    ];

    private static LoginRoleCredential FromConnectionString(IConfiguration configuration, string name, string expectedRole)
    {
        var connectionString = configuration.GetConnectionString(name);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"ConnectionStrings:{name} is required to provision the '{expectedRole}' login (its password is taken from it).");
        }

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (!string.Equals(builder.Username, expectedRole, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"ConnectionStrings:{name} must connect as '{expectedRole}' but names '{builder.Username}'.");
        }

        if (string.IsNullOrEmpty(builder.Password))
        {
            throw new InvalidOperationException($"ConnectionStrings:{name} carries no password for '{expectedRole}'.");
        }

        return new LoginRoleCredential(expectedRole, builder.Password);
    }

    /// <summary>Provisions the logins named by the connection strings over the owner context's connection.</summary>
    public static Task ProvisionAsync(
        ApplicationDbContext ownerContext, IConfiguration configuration, ILogger logger, CancellationToken ct = default) =>
        ProvisionAsync((NpgsqlConnection)ownerContext.Database.GetDbConnection(), ResolveCredentials(configuration), logger, ct);

    public static async Task ProvisionAsync(
        NpgsqlConnection connection,
        IReadOnlyList<LoginRoleCredential> credentials,
        ILogger logger,
        CancellationToken ct = default)
    {
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        foreach (var credential in credentials)
        {
            // Quoting is done by the server so neither the role name nor the password is concatenated here.
            await using var build = new NpgsqlCommand(
                "SELECT format('ALTER ROLE %I WITH LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS PASSWORD %L', @role, @password)",
                connection);
            build.Parameters.AddWithValue("role", credential.RoleName);
            build.Parameters.AddWithValue("password", credential.Password);
            var statement = (string)(await build.ExecuteScalarAsync(ct))!;

            await using var alter = new NpgsqlCommand(statement, connection);
            await alter.ExecuteNonQueryAsync(ct);

            logger.LogInformation("Database login '{Role}' provisioned.", credential.RoleName);
        }

        // The application login must never reach the unbounded worker scope through membership.
        await using var revoke = new NpgsqlCommand(
            """
            DO $$
            BEGIN
                IF pg_has_role('notrelix_app', 'notrelix_worker', 'MEMBER') THEN
                    REVOKE notrelix_worker FROM notrelix_app;
                END IF;
            END $$;
            """,
            connection);
        await revoke.ExecuteNonQueryAsync(ct);
    }
}
