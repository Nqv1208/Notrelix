using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Integration.Tests.Containers;
using Npgsql;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// The runtime logins are real LOGIN roles named <c>notrelix_app</c> and <c>notrelix_worker</c>, created by the
/// owner during rls-apply with the passwords their connection strings carry. Runs on a database of its own:
/// the shared fixtures give the two roles a different shape (group roles behind test logins).
/// </summary>
[Trait("Category", "RlsAppRole")]
public sealed class LoginRoleProvisioningTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _postgres = new();

    public Task InitializeAsync() => _postgres.InitializeAsync();

    public Task DisposeAsync() => _postgres.DisposeAsync();

    // The database address comes from the container's superuser string; only the login and password differ.
    private string Login(string role, string password) =>
        new NpgsqlConnectionStringBuilder(_postgres.OwnerConnectionString) { Username = role, Password = password, Pooling = false }.ConnectionString;

    private IConfiguration Config(string appPassword, string workerPassword) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:NotrelixDb"] = Login("notrelix_app", appPassword),
            ["ConnectionStrings:NotrelixWorkerDb"] = Login("notrelix_worker", workerPassword),
        }).Build();

    private async Task ProvisionAsync(IConfiguration config)
    {
        await using var context = _postgres.CreateOwnerContext();
        await RlsLoginRoleProvisioner.ProvisionAsync(context, config, NullLogger.Instance);
    }

    private static async Task<string> WhoAmIAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT session_user::text", connection);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    [Fact]
    public async Task Provisioning_CreatesLoginsThatConnectWithTheirConnectionStringPasswords_AndIsIdempotent()
    {
        var first = Config("app-password-1", "worker-password-1");
        await ProvisionAsync(first);
        await ProvisionAsync(first);

        (await WhoAmIAsync(first.GetConnectionString("NotrelixDb")!)).Should().Be("notrelix_app");
        (await WhoAmIAsync(first.GetConnectionString("NotrelixWorkerDb")!)).Should().Be("notrelix_worker");

        // Rotating a secret and re-running rls-apply re-asserts the password.
        var rotated = Config("app-password-2", "worker-password-2");
        await ProvisionAsync(rotated);

        (await WhoAmIAsync(rotated.GetConnectionString("NotrelixDb")!)).Should().Be("notrelix_app");
        var stale = async () => await WhoAmIAsync(first.GetConnectionString("NotrelixDb")!);
        await stale.Should().ThrowAsync<PostgresException>("the previous password no longer opens the login");
    }

    [Fact]
    public async Task ProvisionedLogins_AreConstrained_AndSatisfyTheRuntimeRoleGuardFacts()
    {
        var config = Config("app-password-3", "worker-password-3");
        await ProvisionAsync(config);

        foreach (var (name, expectWorker) in new[] { ("NotrelixDb", false), ("NotrelixWorkerDb", true) })
        {
            await using var connection = new NpgsqlConnection(config.GetConnectionString(name));
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                """
                SELECT r.rolsuper, r.rolbypassrls, r.rolcreaterole, r.rolcreatedb, r.rolcanlogin,
                       pg_has_role(session_user, 'notrelix_worker', 'MEMBER'),
                       pg_has_role(session_user, c.relowner, 'USAGE')
                  FROM pg_roles r, pg_class c
                  JOIN pg_namespace n ON n.oid = c.relnamespace
                 WHERE r.rolname = session_user AND n.nspname = 'identity' AND c.relname = 'users'
                """,
                connection);
            await using var reader = await command.ExecuteReaderAsync();
            (await reader.ReadAsync()).Should().BeTrue();
            reader.GetBoolean(0).Should().BeFalse("not a superuser");
            reader.GetBoolean(1).Should().BeFalse("not BYPASSRLS");
            reader.GetBoolean(2).Should().BeFalse("cannot create roles");
            reader.GetBoolean(3).Should().BeFalse("cannot create databases");
            reader.GetBoolean(4).Should().BeTrue("a login");
            reader.GetBoolean(5).Should().Be(expectWorker, "only the background login is a worker");
            reader.GetBoolean(6).Should().BeFalse("not the owner of the tenant tables");
        }
    }

    [Fact]
    public async Task Provisioning_RevokesAnyWorkerMembershipFromTheApplicationLogin()
    {
        await using (var owner = new NpgsqlConnection(_postgres.OwnerConnectionString))
        {
            await owner.OpenAsync();
            await using var grant = new NpgsqlCommand("GRANT notrelix_worker TO notrelix_app", owner);
            await grant.ExecuteNonQueryAsync();
        }

        var config = Config("app-password-4", "worker-password-4");
        await ProvisionAsync(config);

        await using var connection = new NpgsqlConnection(config.GetConnectionString("NotrelixDb"));
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT pg_has_role(session_user, 'notrelix_worker', 'MEMBER')", connection);
        ((bool)(await command.ExecuteScalarAsync())!).Should().BeFalse();
    }
}
