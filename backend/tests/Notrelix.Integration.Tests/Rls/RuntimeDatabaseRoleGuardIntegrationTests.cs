using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Integration.Tests.Containers;
using Npgsql;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// The runtime role guard against real PostgreSQL roles, in every enforcement mode: the container
/// superuser, a login that holds the owner's privileges, a BYPASSRLS login, and the plain application role.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class RuntimeDatabaseRoleGuardIntegrationTests
{
    private readonly AppRoleTestContainer _fixture;

    public RuntimeDatabaseRoleGuardIntegrationTests(AppRoleTestContainer fixture) => _fixture = fixture;

    private async Task<string> CreateLoginAsync(string name, string attributes, string? memberOf = null)
    {
        var secret = Guid.NewGuid().ToString("N");
        await using var connection = new NpgsqlConnection(_fixture.OwnerConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"CREATE ROLE {name} LOGIN PASSWORD '{secret}' {attributes}" +
            (memberOf is null ? "" : $" IN ROLE {memberOf}") + ";";
        await command.ExecuteNonQueryAsync();
        return new NpgsqlConnectionStringBuilder(_fixture.OwnerConnectionString) { Username = name, Password = secret }.ConnectionString;
    }

    private static RuntimeDatabaseRoleGuard Guard(string connectionString, RlsRoleEnforcementMode mode, out List<string> warnings)
    {
        var captured = new List<string>();
        warnings = captured;
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(o => o.UseNpgsql(connectionString));
        var provider = services.BuildServiceProvider();
        return new RuntimeDatabaseRoleGuard(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new RlsOptions { RoleEnforcement = mode }),
            new CapturingLogger(captured));
    }

    private sealed class CapturingLogger(List<string> warnings) : ILogger<RuntimeDatabaseRoleGuard>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning) warnings.Add(formatter(state, exception));
        }
    }

    [Fact]
    public async Task Superuser_IsRefusedWhenEnforced_WarnedWhenWarn_AndIgnoredWhenOff()
    {
        var cs = _fixture.OwnerConnectionString;

        var enforce = Guard(cs, RlsRoleEnforcementMode.Enforce, out _);
        (await FluentActions.Awaiting(() => enforce.StartAsync(default)).Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Contain("superuser");

        var warn = Guard(cs, RlsRoleEnforcementMode.Warn, out var warnings);
        await warn.StartAsync(default);
        warnings.Should().ContainSingle().Which.Should().Contain("superuser");

        var off = Guard(cs, RlsRoleEnforcementMode.Off, out var offWarnings);
        await off.StartAsync(default);
        offWarnings.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplicationRole_StartsInEveryMode()
    {
        foreach (var mode in Enum.GetValues<RlsRoleEnforcementMode>())
        {
            var guard = Guard(_fixture.AppConnectionString, mode, out var warnings);
            await guard.StartAsync(default);
            warnings.Should().BeEmpty($"the application role is acceptable in {mode} mode");
        }
    }

    [Fact]
    public async Task LoginHoldingTheOwnersPrivileges_IsRefusedWhenEnforced()
    {
        var cs = await CreateLoginAsync("guard_owner_member", "NOSUPERUSER", memberOf: "notrelix");
        var guard = Guard(cs, RlsRoleEnforcementMode.Enforce, out _);

        (await FluentActions.Awaiting(() => guard.StartAsync(default)).Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Contain("owns the tenant tables");
    }

    [Fact]
    public async Task BypassRlsLogin_IsRefusedWhenEnforced()
    {
        var cs = await CreateLoginAsync("guard_bypass_login", "NOSUPERUSER BYPASSRLS");
        var guard = Guard(cs, RlsRoleEnforcementMode.Enforce, out _);

        (await FluentActions.Awaiting(() => guard.StartAsync(default)).Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Contain("BYPASSRLS");
    }

    // ----- the background login ------------------------------------------------------------------------

    private static RuntimeDatabaseRoleGuard TwoLoginGuard(
        string application, string? worker, RlsRoleEnforcementMode mode, out List<string> warnings)
    {
        var captured = new List<string>();
        warnings = captured;
        var services = new ServiceCollection();
        var strings = string.IsNullOrWhiteSpace(worker)
            ? new DatabaseConnectionStrings(application, application, true)
            : new DatabaseConnectionStrings(application, worker, false);
        services.AddSingleton(strings);
        services.AddScoped<DatabasePrincipal>();
        services.AddDbContext<ApplicationDbContext>((sp, o) =>
            o.UseNpgsql(sp.GetRequiredService<DatabasePrincipal>().IsWorker ? strings.Worker : strings.Application));
        var provider = services.BuildServiceProvider();
        return new RuntimeDatabaseRoleGuard(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new RlsOptions { RoleEnforcement = mode }),
            new CapturingLogger(captured));
    }

    [Fact]
    public async Task ApplicationLoginAndWorkerLogin_StartInEveryMode()
    {
        foreach (var mode in Enum.GetValues<RlsRoleEnforcementMode>())
        {
            var guard = TwoLoginGuard(_fixture.AppConnectionString, _fixture.WorkerConnectionString, mode, out var warnings);
            await guard.StartAsync(default);
            warnings.Should().BeEmpty($"the application and worker logins are what {mode} mode expects");
        }
    }

    [Fact]
    public async Task MissingWorkerConnection_IsRefusedWhenEnforced_BecauseBackgroundWorkWouldRunAsTheApplicationLogin()
    {
        var guard = TwoLoginGuard(_fixture.AppConnectionString, null, RlsRoleEnforcementMode.Enforce, out _);

        (await FluentActions.Awaiting(() => guard.StartAsync(default)).Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Contain("ConnectionStrings:NotrelixWorkerDb is not set");
    }

    [Fact]
    public async Task WorkerConnectionThatIsTheApplicationLogin_IsRefusedWhenEnforced()
    {
        var guard = TwoLoginGuard(_fixture.AppConnectionString, _fixture.AppConnectionString, RlsRoleEnforcementMode.Enforce, out _);

        (await FluentActions.Awaiting(() => guard.StartAsync(default)).Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Contain("not a member of notrelix_worker");
    }

    [Fact]
    public async Task ApplicationConnectionThatIsTheWorkerLogin_IsRefusedWhenEnforced()
    {
        var guard = TwoLoginGuard(_fixture.WorkerConnectionString, _fixture.WorkerConnectionString, RlsRoleEnforcementMode.Enforce, out _);

        (await FluentActions.Awaiting(() => guard.StartAsync(default)).Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Contain("application connection").And.Contain("member of notrelix_worker");
    }

    [Fact]
    public async Task PrivilegedWorkerConnection_IsRefusedWhenEnforced_AndWarnedWhenWarn()
    {
        var enforce = TwoLoginGuard(_fixture.AppConnectionString, _fixture.OwnerConnectionString, RlsRoleEnforcementMode.Enforce, out _);
        (await FluentActions.Awaiting(() => enforce.StartAsync(default)).Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Contain("background connection").And.Contain("superuser");

        var warn = TwoLoginGuard(_fixture.AppConnectionString, _fixture.OwnerConnectionString, RlsRoleEnforcementMode.Warn, out var warnings);
        await warn.StartAsync(default);
        warnings.Should().ContainSingle().Which.Should().Contain("background connection");
    }
}
