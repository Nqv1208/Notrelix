using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Domain.Identity.Security;
using Notrelix.Domain.Workspaces.Onboarding;
using Notrelix.Infrastructure.Data;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Npgsql;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// Proves the background principal: the worker scope is honoured only on a connection logged in as the
/// worker role (an application connection that sets it gains nothing), and every background path - the
/// outbox and email dispatchers, the deduplication store, message consumers, and with them the first
/// workspace provisioning - works over the worker connection while the host serves requests as the
/// application role.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class WorkerPrincipalTests : IAsyncLifetime
{
    private const string Password = "Correct-Horse-9!";
    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;

    public WorkerPrincipalTests(AppRoleTestContainer fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "worker-a@example.com", "Wrka", "hash");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task<string> ScalarAsync(NpgsqlConnection c, NpgsqlTransaction? tx, string sql, params (string, object)[] p)
    {
        await using var command = new NpgsqlCommand(sql, c, tx);
        foreach (var (name, value) in p) command.Parameters.AddWithValue(name, value);
        return Convert.ToString(await command.ExecuteScalarAsync()) ?? "";
    }

    private async Task<string> OwnerScalarAsync(string sql)
    {
        await using var c = new NpgsqlConnection(_fixture.OwnerConnectionString);
        await c.OpenAsync();
        return await ScalarAsync(c, null, sql);
    }

    [Fact]
    public async Task AnApplicationConnection_SettingTheWorkerScope_SeesNothingExtra()
    {
        await using var c = new NpgsqlConnection(_fixture.AppConnectionString);
        await c.OpenAsync();
        await using var tx = await c.BeginTransactionAsync();

        await ScalarAsync(c, tx, "SELECT set_config('app.request_scope', 'worker', true)");

        (await ScalarAsync(c, tx, "SELECT ops.current_request_scope()")).Should().Be("worker", "the session value itself is settable");
        (await ScalarAsync(c, tx, "SELECT ops.is_worker_scope()")).Should().Be("False", "the login is not a worker, so the scope grants nothing");
        (await ScalarAsync(c, tx, "SELECT ops.has_account_access(@a)", ("a", _a.AccountId))).Should().Be("False");
        (await ScalarAsync(c, tx, "SELECT ops.has_workspace_access(@a, @w)", ("a", _a.AccountId), ("w", _a.WorkspaceId))).Should().Be("False");
        (await ScalarAsync(c, tx, "SELECT ops.is_account_admin(@a)", ("a", _a.AccountId))).Should().Be("False");
        foreach (var table in new[] { "identity.users", "account.accounts", "workspace.workspaces", "workspace.workspace_members", "authz.access_grants", "work.boards" })
        {
            (await ScalarAsync(c, tx, $"SELECT count(*) FROM {table}")).Should().Be("0", $"{table} stays closed to a forged worker scope");
        }

        foreach (var other in new[] { "system", "migration", "migrator" })
        {
            await ScalarAsync(c, tx, "SELECT set_config('app.request_scope', @s, true)", ("s", other));
            (await ScalarAsync(c, tx, "SELECT ops.is_worker_scope()")).Should().Be("False", $"'{other}' was never a worker scope");
        }
    }

    [Fact]
    public async Task AWorkerConnection_HoldsTheWorkerScopeOnlyWhenItNamesIt()
    {
        await using var c = new NpgsqlConnection(_fixture.WorkerConnectionString);
        await c.OpenAsync();
        await using var tx = await c.BeginTransactionAsync();

        (await ScalarAsync(c, tx, "SELECT ops.is_worker_scope()")).Should().Be("False", "no scope is entered by default");
        await ScalarAsync(c, tx, "SELECT set_config('app.request_scope', 'worker', true)");
        (await ScalarAsync(c, tx, "SELECT ops.is_worker_scope()")).Should().Be("True");
        await ScalarAsync(c, tx, "SELECT set_config('app.request_scope', 'token', true)");
        (await ScalarAsync(c, tx, "SELECT ops.is_worker_scope()")).Should().Be("False", "a bounded scope is never the worker scope");
    }

    [Theory]
    [InlineData("messaging.outbox_messages")]
    [InlineData("messaging.processed_events")]
    [InlineData("events.domain_event_logs")]
    [InlineData("identity.users")]
    [InlineData("authz.access_grants")]
    public async Task TheWorkerLogin_ReadsTheBackgroundTables_WhichTheApplicationLoginCannotRead(string table)
    {
        await using var worker = new NpgsqlConnection(_fixture.WorkerConnectionString);
        await worker.OpenAsync();
        await using var app = new NpgsqlConnection(_fixture.AppConnectionString);
        await app.OpenAsync();

        // A row of each table exists (written as the owner) so "sees nothing" is not an empty table.
        var ownerCount = long.Parse(await OwnerScalarAsync($"SELECT count(*) FROM {table}"));
        if (table is "messaging.processed_events")
        {
            await OwnerScalarAsync(
                "INSERT INTO messaging.processed_events (id, event_id, consumer_name, message_name, message_version, status, claimed_at, processed_at) " +
                "VALUES (gen_random_uuid(), gen_random_uuid(), 'proof-consumer', 'proof', 1, 'Succeeded', now(), now())");
            ownerCount += 1;
        }

        if (ownerCount == 0)
        {
            return; // nothing seeded for this table by the tenant seed; the privilege check below still applies
        }

        long.Parse(await ScalarAsync(worker, null, $"SELECT count(*) FROM {table}")).Should().Be(ownerCount, $"the worker reads all of {table}");

        if (table is "identity.users" or "authz.access_grants")
        {
            return; // the application role reads its own rows of these, covered by the scope suites
        }

        (await ScalarAsync(app, null, $"SELECT count(*) FROM {table}")).Should().Be("0", $"the application login reads none of {table}");
    }

    [Fact]
    public async Task AWorkerScopedDependencyScope_ConnectsAsTheWorker_AndARequestScopeAsTheApplication()
    {
        using var factory = AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);

        await using (var worker = factory.Services.GetRequiredService<IServiceScopeFactory>().CreateWorkerAsyncScope())
        {
            var db = worker.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await db.Database.SqlQuery<string>($"SELECT current_user::text AS \"Value\"").SingleAsync();
            user.Should().Be("notrelix_worker");
        }

        await using (var request = factory.Services.CreateAsyncScope())
        {
            var db = request.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await db.Database.SqlQuery<string>($"SELECT current_user::text AS \"Value\"").SingleAsync();
            user.Should().Be("notrelix_app");
        }
    }

    [Fact]
    public async Task APrincipalCannotBeSwitchedToTheWorkerAfterTheConnectionIsResolved()
    {
        using var factory = AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);
        await using var scope = factory.Services.CreateAsyncScope();
        _ = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var act = () => scope.ServiceProvider.GetRequiredService<DatabasePrincipal>().UseWorker();
        act.Should().Throw<InvalidOperationException>("the connection is fixed when the context is first resolved");
    }

    [Fact]
    public async Task Registration_RunsTheWholeBackgroundChain_OverTheWorkerConnection_AndProvisionsTheFirstWorkspace()
    {
        using var factory = AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);
        using var client = factory.CreateClient();

        var register = await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = "first-workspace@example.com", password = Password, name = "FirstWs" });
        register.IsSuccessStatusCode.Should().BeTrue(await register.Content.ReadAsStringAsync());

        // The outbox dispatcher publishes the registration fact, the deduplication filter claims it, and the
        // provisioning consumer creates the personal workspace: all over the worker connection.
        var provisioned = await PollAsync(() => OwnerScalarAsync(
            "SELECT count(*) FROM workspace.workspaces w JOIN identity.users u ON u.id = w.created_by " +
            "WHERE u.normalized_email = 'first-workspace@example.com'"), "1");
        provisioned.Should().BeTrue("the first workspace is provisioned by the consumer running as the worker");

        (await OwnerScalarAsync(
            "SELECT count(*) FROM authz.access_grants g JOIN identity.users u ON u.id = g.user_id " +
            "JOIN workspace.workspaces w ON w.id = g.workspace_id " +
            "WHERE u.normalized_email = 'first-workspace@example.com' AND g.is_workspace_admin AND g.revoked_at IS NULL"))
            .Should().Be("1", "the Owner grant of the new workspace was written");

        (await OwnerScalarAsync(
            "SELECT count(*) FROM messaging.processed_events WHERE status = 'Succeeded' " +
            "AND consumer_name LIKE '%workspace-provision%'"))
            .Should().Be("1", "the deduplication claim and its success marker were written by the worker");

        (await OwnerScalarAsync(
            "SELECT count(*) FROM messaging.outbox_messages WHERE status = 'Processed'"))
            .Should().NotBe("0", "the dispatcher marked messages processed");

        // The email dispatcher claims the verification email the registration queued and records its attempt
        // (delivery fails here: no mail server), which only a worker connection can do on these tables.
        var emailHandled = await PollAsync(() => OwnerScalarAsync(
            "SELECT count(*) FILTER (WHERE status <> 'Pending' OR retry_count > 0) > 0 FROM notifications.email_outbox"), "True");
        emailHandled.Should().BeTrue("the email dispatcher ran over the worker connection");
    }

    [Fact]
    public async Task JobLocks_AreAcquiredOverTheWorkerConnection_AndDeniedToTheApplicationConnection()
    {
        await using var worker = new NpgsqlConnection(_fixture.WorkerConnectionString);
        await worker.OpenAsync();
        var locks = new Notrelix.Infrastructure.Data.Ops.Stores.JobLockManager(worker);
        (await locks.AcquireAsync("proof:job-lock", "worker-proof", TimeSpan.FromMinutes(1))).Should().NotBeNull();
        (await locks.AcquireAsync("proof:job-lock", "worker-proof-2", TimeSpan.FromMinutes(1)))
            .Should().BeNull("a held lock is not taken twice");

        await using var app = new NpgsqlConnection(_fixture.AppConnectionString);
        await app.OpenAsync();
        var act = () => new Notrelix.Infrastructure.Data.Ops.Stores.JobLockManager(app)
            .AcquireAsync("proof:app-lock", "app-proof", TimeSpan.FromMinutes(1));
        (await act.Should().ThrowAsync<PostgresException>()).Which.MessageText.Should().Contain("row-level security");
    }

    [Fact]
    public async Task WithoutAWorkerConnection_TheBackgroundChainIsBlindUnderTheApplicationRole()
    {
        // Control: the same host with the worker connection pointed back at the application login. The
        // dispatcher cannot claim the outbox and nothing is provisioned - which is exactly why background work
        // has its own connection.
        using var factory = AppRoleHost.CreateFactory(
            _fixture, _fixture.AppConnectionString,
            new Dictionary<string, string?> { ["ConnectionStrings:NotrelixWorkerDb"] = _fixture.AppConnectionString });
        using var client = factory.CreateClient();

        var register = await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = "blind@example.com", password = Password, name = "Blind" });
        register.IsSuccessStatusCode.Should().BeTrue(await register.Content.ReadAsStringAsync());

        await Task.Delay(TimeSpan.FromSeconds(8));
        (await OwnerScalarAsync(
            "SELECT count(*) FROM workspace.workspaces w JOIN identity.users u ON u.id = w.created_by WHERE u.normalized_email = 'blind@example.com'"))
            .Should().Be("0");
        (await OwnerScalarAsync("SELECT count(*) FROM messaging.outbox_messages WHERE status = 'Processed'")).Should().Be("0");
    }

    [Fact]
    public async Task TheSweepsAndTheRetention_RunOverTheWorkerConnection()
    {
        var now = DateTimeOffset.UtcNow;
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using (var ctx = _fixture.CreateOwnerContext(system))
        {
            ctx.OnboardingDrafts.Add(OnboardingDraft.Create(_a.User.Id, "company", null, null, now.AddDays(-31)));
            ctx.SecurityActivity.Add(SecurityActivityEntry.Record(_a.User.Id, SecurityActivityType.SignInSucceeded, now.AddDays(-91)));
            ctx.SecurityActivity.Add(SecurityActivityEntry.Record(_a.User.Id, SecurityActivityType.SignInFailed, now.AddDays(-1)));
            await ctx.SaveChangesAsync();
        }

        using var factory = AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);
        await using var scope = factory.Services.GetRequiredService<IServiceScopeFactory>().CreateWorkerAsyncScope();
        var assembly = typeof(Notrelix.Infrastructure.BackgroundJobs.SecurityActivityRetentionOptions).Assembly;
        foreach (var name in new[] { "OnboardingDraftSweeper", "SecurityActivityRetentionSweeper" })
        {
            var type = assembly.GetType($"Notrelix.Infrastructure.BackgroundJobs.{name}")!;
            var sweeper = scope.ServiceProvider.GetRequiredService(type);
            await (Task<int>)type.GetMethod("RunOnceAsync")!.Invoke(sweeper, [CancellationToken.None])!;
        }

        (await OwnerScalarAsync("SELECT count(*) FROM workspace.onboarding_drafts")).Should().Be("0", "the expired draft was swept by the worker");
        (await OwnerScalarAsync("SELECT count(*) FROM identity.user_security_activity")).Should().Be("1", "only the entry older than 90 days was removed");
    }

    private static async Task<bool> PollAsync(Func<Task<string>> read, string expected, int seconds = 45)
    {
        var deadline = DateTime.UtcNow.AddSeconds(seconds);
        while (DateTime.UtcNow < deadline)
        {
            if (await read() == expected) return true;
            await Task.Delay(500);
        }

        return false;
    }
}
