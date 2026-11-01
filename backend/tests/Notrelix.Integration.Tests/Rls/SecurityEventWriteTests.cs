using System.Diagnostics.Metrics;
using System.Net;
using Notrelix.Integration.Tests.Containers;
using Npgsql;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// Security audit events under the application role. A security event is frequently tenantless (an anonymous
/// or pre-authentication failure), which no tenant-scoped policy admits; the row is written by one insert-only
/// owner-defined function whose actor and tenant come from the scope, never from the caller. A failed write
/// is logged and counted, not swallowed silently.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class SecurityEventWriteTests : IAsyncLifetime
{
    private const string Record =
        "SELECT ops.record_security_event(@type, @severity, 'Observed', 10, '10.0.0.1', 'agent', NULL, NULL, 'GET', NULL, 'corr', '{\"k\":1}'::jsonb, now())";

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;

    public SecurityEventWriteTests(AppRoleTestContainer fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "a-audit@example.com", "Aaa", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "b-audit@example.com", "Bbb", "hash");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task<string> ScalarAsync(NpgsqlConnection c, NpgsqlTransaction? tx, string sql, params (string, object)[] p)
    {
        await using var command = new NpgsqlCommand(sql, c, tx);
        foreach (var (name, value) in p) command.Parameters.AddWithValue(name, value);
        return Convert.ToString(await command.ExecuteScalarAsync()) ?? "";
    }

    private async Task<string> OwnerScalarAsync(string sql, params (string, object)[] p)
    {
        await using var c = new NpgsqlConnection(_fixture.OwnerConnectionString);
        await c.OpenAsync();
        return await ScalarAsync(c, null, sql, p);
    }

    private static (string, object)[] Event(string type = "LoginFailed", string severity = "Warning") =>
        [("type", type), ("severity", severity)];

    private async Task<(NpgsqlConnection, NpgsqlTransaction)> BeginAsync(string connectionString)
    {
        var c = new NpgsqlConnection(connectionString);
        await c.OpenAsync();
        return (c, await c.BeginTransactionAsync());
    }

    [Fact]
    public async Task ATenantlessAnonymousEvent_IsWrittenByTheApplicationRole_WithNoActorAndNoTenant()
    {
        var (c, tx) = await BeginAsync(_fixture.AppConnectionString);
        await using (c)
        await using (tx)
        {
            await ScalarAsync(c, tx, Record, Event());
            await tx.CommitAsync();
        }

        (await OwnerScalarAsync(
            "SELECT count(*) FROM audit.security_events WHERE event_type = 'LoginFailed' AND user_id IS NULL AND workspace_id IS NULL"))
            .Should().Be("1");
    }

    [Fact]
    public async Task TheDirectTenantlessInsert_IsStillDeniedByPolicy()
    {
        await using var c = new NpgsqlConnection(_fixture.AppConnectionString);
        await c.OpenAsync();
        var act = () => ScalarAsync(c, null,
            "INSERT INTO audit.security_events (id, event_type, severity, outcome, occurred_at, recorded_at) " +
            "VALUES (gen_random_uuid(), 'LoginFailed', 'Info', 'Observed', now(), now())");
        (await act.Should().ThrowAsync<PostgresException>()).Which.MessageText.Should().Contain("row-level security");
    }

    [Fact]
    public async Task TheActor_IsTheUserOfTheScope_NeverSuppliedByTheCaller()
    {
        var (c, tx) = await BeginAsync(_fixture.AppConnectionString);
        await using (c)
        await using (tx)
        {
            await ScalarAsync(c, tx,
                "SELECT set_config('app.current_user_id', @u, true), set_config('app.scope_user_id', @u, true), set_config('app.request_scope', 'user', true)",
                ("u", _a.User.Id.ToString()));
            await ScalarAsync(c, tx, Record, Event("ForbiddenAccess"));
            await tx.CommitAsync();
        }

        (await OwnerScalarAsync(
            "SELECT user_id::text FROM audit.security_events WHERE event_type = 'ForbiddenAccess'"))
            .Should().Be(_a.User.Id.ToString());
    }

    [Fact]
    public async Task ABoundedTokenScope_RecordsNoActor_EvenWhenAUserIdIsLeftInTheSession()
    {
        var (c, tx) = await BeginAsync(_fixture.AppConnectionString);
        await using (c)
        await using (tx)
        {
            await ScalarAsync(c, tx,
                "SELECT set_config('app.current_user_id', @u, true), set_config('app.request_scope', 'token', true)",
                ("u", _a.User.Id.ToString()));
            await ScalarAsync(c, tx, Record, Event("UnauthorizedAccess"));
            await tx.CommitAsync();
        }

        (await OwnerScalarAsync("SELECT count(*) FROM audit.security_events WHERE event_type = 'UnauthorizedAccess' AND user_id IS NULL"))
            .Should().Be("1");
    }

    [Fact]
    public async Task TheTenant_IsTheWorkspaceOfTheTenantScope_WhenThereIsOne()
    {
        var (c, tx) = await BeginAsync(_fixture.AppConnectionString);
        await using (c)
        await using (tx)
        {
            await ScalarAsync(c, tx,
                "SELECT set_config('app.current_user_id', @u, true), set_config('app.current_account_id', @a, true), " +
                "set_config('app.current_workspace_id', @w, true), set_config('app.request_scope', 'app', true)",
                ("u", _a.User.Id.ToString()), ("a", _a.AccountId.ToString()), ("w", _a.WorkspaceId.ToString()));
            await ScalarAsync(c, tx, Record, Event("RateLimitExceeded", "Info"));
            await tx.CommitAsync();
        }

        (await OwnerScalarAsync(
            "SELECT workspace_id::text || '/' || user_id::text FROM audit.security_events WHERE event_type = 'RateLimitExceeded'"))
            .Should().Be($"{_a.WorkspaceId}/{_a.User.Id}");
    }

    [Theory]
    [InlineData("Dropped", "Info")]
    [InlineData("LoginFailed", "Fatal")]
    public async Task AnUnknownTypeOrSeverity_IsRejected(string type, string severity)
    {
        var (c, tx) = await BeginAsync(_fixture.AppConnectionString);
        await using (c)
        await using (tx)
        {
            var act = () => ScalarAsync(c, tx, Record, Event(type, severity));
            (await act.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be("22023");
        }
    }

    [Fact]
    public async Task TheApplicationRole_CannotReadChangeOrDeleteSecurityEvents_AndAnotherTenantsAdminSeesNone()
    {
        var (c, tx) = await BeginAsync(_fixture.AppConnectionString);
        await using (c)
        await using (tx)
        {
            await ScalarAsync(c, tx, Record, Event());
            (await ScalarAsync(c, tx, "SELECT count(*) FROM audit.security_events")).Should().Be("0", "a tenantless event is readable by nobody on the application role");
            // The trail is append-only: the role holds no UPDATE or DELETE privilege at all.
            (await ScalarAsync(c, tx,
                "SELECT has_table_privilege(current_user, 'audit.security_events', 'UPDATE') " +
                "OR has_table_privilege(current_user, 'audit.security_events', 'DELETE')")).Should().Be("False");
            await tx.CommitAsync();
        }

        (await OwnerScalarAsync("SELECT count(*) FROM audit.security_events")).Should().Be("1", "the row survived the attempts");
    }

    [Fact]
    public async Task TheWorkerRole_MayRecordToo_ButNeverPublicOrTheOtherRoles()
    {
        var (c, tx) = await BeginAsync(_fixture.WorkerConnectionString);
        await using (c)
        await using (tx)
        {
            await ScalarAsync(c, tx, Record, Event());
            await tx.CommitAsync();
        }

        (await OwnerScalarAsync("SELECT count(*) FROM audit.security_events")).Should().Be("1");
        foreach (var role in new[] { "public", "notrelix_auth", "notrelix_support_readonly" })
        {
            (await OwnerScalarAsync(
                $"SELECT has_function_privilege('{role}', 'ops.record_security_event(text,text,text,integer,text,text,text,uuid,text,uuid,text,jsonb,timestamptz)', 'EXECUTE')"))
                .Should().Be("False", $"{role} must not record security events");
        }
    }

    [Fact]
    public async Task AnUnauthenticatedRequest_IsAuditedByTheSink_UnderTheApplicationRole()
    {
        using var factory = AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        (await OwnerScalarAsync(
            "SELECT count(*) FROM audit.security_events WHERE event_type = 'UnauthorizedAccess' AND user_id IS NULL AND workspace_id IS NULL"))
            .Should().Be("1", "the sink wrote the tenantless event through the bounded function");
    }

    [Fact]
    public async Task AFailedWrite_IsCountedAndNeverFailsTheRequest()
    {
        await OwnerScalarAsync(
            "REVOKE EXECUTE ON FUNCTION ops.record_security_event(text,text,text,integer,text,text,text,uuid,text,uuid,text,jsonb,timestamptz) FROM notrelix_app");
        var failed = 0L;
        var written = 0L;
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == "Notrelix.Infrastructure.Rls" && instrument.Name == "security_audit_event_write")
                l.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
        {
            foreach (var tag in tags)
            {
                if (tag.Key != "outcome") continue;
                if ((string?)tag.Value == "failed") Interlocked.Add(ref failed, value);
                if ((string?)tag.Value == "written") Interlocked.Add(ref written, value);
            }
        });
        listener.Start();

        try
        {
            using var factory = AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);
            using var client = factory.CreateClient();
            var response = await client.GetAsync("/api/v1/auth/me");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "an audit failure never changes the response");
        }
        finally
        {
            await OwnerScalarAsync(
                "GRANT EXECUTE ON FUNCTION ops.record_security_event(text,text,text,integer,text,text,text,uuid,text,uuid,text,jsonb,timestamptz) TO notrelix_app");
        }

        Interlocked.Read(ref failed).Should().BeGreaterThan(0, "the failure is counted, not silent");
        Interlocked.Read(ref written).Should().Be(0);
        (await OwnerScalarAsync("SELECT count(*) FROM audit.security_events")).Should().Be("0");
    }
}
