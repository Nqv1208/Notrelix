using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API;
using Notrelix.Domain.Operators;
using Notrelix.Infrastructure.Billing;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Rls;
using Notrelix.Testing.Application.Fakes;
using Npgsql;

namespace Notrelix.Integration.Tests.Operators;

/// <summary>
/// The platform operator console on the production-shaped database: the host serves tenants as
/// <c>notrelix_app</c> and the console as <c>notrelix_operator</c>. Proves the principal separation (tokens refused
/// in both directions, the operator login holds no tenant content), the operator actions end to end (suspend, lift,
/// plan, override, terminate, catalog) and that what the operator changes is what the tenant then sees.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class OperatorConsoleIntegrationTests : IAsyncLifetime
{
    private const string OperatorEmail = "ops@notrelix.test";
    private const string OperatorPassword = "Operator-Pass-12345";
    private const string Denied = "42501";

    private readonly AppRoleTestContainer _fixture;
    private WebApplicationFactory<Program> _host = null!;
    private AppRoleTenant _tenant = null!;
    private Guid _operatorId;

    public OperatorConsoleIntegrationTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _host = AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);

        string hash;
        using (var scope = _host.Services.CreateScope())
            hash = scope.ServiceProvider.GetRequiredService<IPasswordHasher>().HashPassword(OperatorPassword);

        await using (var owner = _fixture.CreateOwnerContext(new FakeCurrentTenantContext()))
        {
            await PlanCatalogSeed.EnsureAsync(owner);
            var account = OperatorAccount.Create(OperatorEmail, "Ops", hash, DateTimeOffset.UtcNow);
            _operatorId = account.Id;
            owner.OperatorAccounts.Add(account);
            await owner.SaveChangesAsync();
        }

        _tenant = await AppRoleTenants.SeedAsync(_fixture, "operator-tenant@example.com", "Optenant", "hash");
    }

    public Task DisposeAsync()
    {
        _host.Dispose();
        return Task.CompletedTask;
    }

    // ── helpers ────────────────────────────────────────────────────────────

    private async Task<string> SignInAsync(string email = OperatorEmail, string password = OperatorPassword)
    {
        using var client = _host.CreateClient();
        var response = await client.PostAsJsonAsync("/operator/v1/auth/sign-in", new { email, password });
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString()!;
    }

    private HttpClient As(string token)
    {
        var client = _host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return client;
    }

    private string TenantToken()
    {
        using var scope = _host.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(_tenant.User);
    }

    private async Task<JsonElement> GetJsonAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<long> AccountVersionAsync(HttpClient op) =>
        (await GetJsonAsync(op, $"/operator/v1/accounts/{_tenant.AccountId}")).GetProperty("account").GetProperty("version").GetInt64();

    private async Task<JsonElement> TenantCapabilityAsync(string code)
    {
        using var tenant = As(TenantToken());
        var caps = await GetJsonAsync(tenant, $"/api/v1/workspaces/{_tenant.WorkspaceId}/billing/capabilities");
        return caps.EnumerateArray().Single(c => c.GetProperty("code").GetString() == code);
    }

    private static async Task<string> OwnerScalarAsync(string connectionString, string sql, string? role = null)
    {
        await using var c = new NpgsqlConnection(connectionString);
        await c.OpenAsync();
        if (role is not null)
        {
            await using var setRole = new NpgsqlCommand($"SET ROLE {role}", c);
            await setRole.ExecuteNonQueryAsync();
        }

        await using var command = new NpgsqlCommand(sql, c);
        return Convert.ToString(await command.ExecuteScalarAsync()) ?? "";
    }

    private async Task<HttpResponseMessage> TenantCreateBoardAsync()
    {
        using var tenant = As(TenantToken());
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/workspaces/{_tenant.WorkspaceId}/boards/")
        {
            Content = JsonContent.Create(new { title = $"Board {Guid.NewGuid():N}"[..14] }),
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        return await tenant.SendAsync(request);
    }

    // ── sign-in and the two directions of token separation ─────────────────

    [Fact]
    public async Task An_operator_signs_in_and_the_token_opens_the_operator_routes()
    {
        var token = await SignInAsync();
        using var op = As(token);

        var me = await GetJsonAsync(op, "/operator/v1/auth/me");

        me.GetProperty("email").GetString().Should().Be(OperatorEmail);
        me.GetProperty("id").GetGuid().Should().Be(_operatorId);
    }

    [Theory]
    [InlineData("wrong-password-123", OperatorEmail)]
    [InlineData(OperatorPassword, "nobody@notrelix.test")]
    public async Task A_wrong_password_or_unknown_email_is_the_same_401(string password, string email)
    {
        using var client = _host.CreateClient();

        var response = await client.PostAsJsonAsync("/operator/v1/auth/sign-in", new { email, password });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).Should().NotContain("exist", "the answer never says which operators exist");
    }

    [Fact]
    public async Task Five_wrong_passwords_lock_the_operator_out_even_for_the_right_one()
    {
        using var client = _host.CreateClient();
        for (var i = 0; i < OperatorAccount.MaxFailedSignIns; i++)
            (await client.PostAsJsonAsync("/operator/v1/auth/sign-in", new { email = OperatorEmail, password = $"wrong-password-{i}" }))
                .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var right = await client.PostAsJsonAsync("/operator/v1/auth/sign-in", new { email = OperatorEmail, password = OperatorPassword });

        right.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_tenant_token_is_refused_on_every_operator_route()
    {
        using var tenant = As(TenantToken());

        foreach (var url in new[] { "/operator/v1/auth/me", "/operator/v1/accounts", $"/operator/v1/accounts/{_tenant.AccountId}", "/operator/v1/billing/plans", "/operator/v1/audit" })
            (await tenant.GetAsync(url)).StatusCode.Should().Be(HttpStatusCode.Unauthorized, url);

        (await tenant.PostAsJsonAsync($"/operator/v1/accounts/{_tenant.AccountId}/suspend", new { reason = "x", expectedVersion = 1 }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task An_operator_token_is_refused_on_tenant_routes()
    {
        using var op = As(await SignInAsync());

        (await op.GetAsync($"/api/v1/workspaces/{_tenant.WorkspaceId}/billing/plan")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await op.GetAsync($"/api/v1/workspaces/{_tenant.WorkspaceId}/boards")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await op.GetAsync("/api/v1/auth/me")).StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Operator_routes_need_a_token_and_a_forged_or_unsigned_token_is_refused()
    {
        using var anonymous = _host.CreateClient();
        (await anonymous.GetAsync("/operator/v1/accounts")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var forged = As("eyJhbGciOiJub25lIn0.eyJzdWIiOiIxIiwidG9rZW5fdXNlIjoib3BlcmF0b3IifQ.");
        (await forged.GetAsync("/operator/v1/accounts")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Disabling_an_operator_ends_access_on_the_next_request_not_when_the_token_runs_out()
    {
        using var op = As(await SignInAsync());
        (await op.GetAsync("/operator/v1/auth/me")).StatusCode.Should().Be(HttpStatusCode.OK);

        await using (var owner = _fixture.CreateOwnerContext(new FakeCurrentTenantContext()))
        {
            await owner.Database.ExecuteSqlAsync($"UPDATE operators.operator_accounts SET status = 'Disabled' WHERE id = '{_operatorId}'");
        }

        (await op.GetAsync("/operator/v1/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task The_console_is_off_without_an_operator_login_and_never_falls_back_to_another_login()
    {
        using var off = AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString,
            new Dictionary<string, string?> { ["ConnectionStrings:NotrelixOperatorDb"] = "", ["Operator:Jwt:SigningKey"] = "" });
        using var client = off.CreateClient();

        (await client.PostAsJsonAsync("/operator/v1/auth/sign-in", new { email = OperatorEmail, password = OperatorPassword }))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── the operator database login ────────────────────────────────────────

    [Fact]
    public async Task The_operator_login_can_read_exactly_the_allow_listed_tables_and_no_tenant_content()
    {
        var readable = await OwnerScalarAsync(_fixture.OwnerConnectionString, """
            SELECT coalesce(string_agg(n.nspname || '.' || c.relname, ',' ORDER BY n.nspname, c.relname), '')
              FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
             WHERE c.relkind IN ('r', 'p', 'v', 'm')
               AND n.nspname NOT IN ('pg_catalog', 'information_schema', 'pg_toast')
               AND has_any_column_privilege('notrelix_operator', c.oid, 'SELECT')
            """);

        readable.Split(',', StringSplitOptions.RemoveEmptyEntries).Should().BeEquivalentTo(
        [
            "account.accounts", "billing.entitlements", "billing.plan_limits", "billing.plan_prices", "billing.plans",
            "billing.subscriptions", "operators.operator_accounts", "operators.operator_audit",
        ], "the operator reads account identity and commercial billing state, nothing else");
    }

    [Fact]
    public async Task The_operator_login_is_refused_every_tenant_content_and_credential_table()
    {
        await using var c = new NpgsqlConnection(_fixture.OperatorConnectionString);
        await c.OpenAsync();
        (await OwnerScalarAsync(_fixture.OperatorConnectionString, "SELECT session_user")).Should().Be("notrelix_operator");

        foreach (var table in new[]
                 {
                     "work.boards", "work.board_items", "docs.pages", "docs.blocks", "collab.comments", "collab.attachments",
                     "workspace.workspaces", "workspace.workspace_members", "account.account_members", "identity.users",
                     "identity.user_sessions", "identity.api_tokens", "authz.access_grants", "billing.invoices", "billing.payment_methods",
                     "messaging.outbox_messages", "events.domain_event_logs", "audit.audit_logs",
                 })
        {
            var read = async () =>
            {
                await using var command = new NpgsqlCommand($"SELECT count(*) FROM {table}", c);
                await command.ExecuteScalarAsync();
            };
            (await read.Should().ThrowAsync<PostgresException>(table)).Which.SqlState.Should().Be(Denied, table);
        }
    }

    [Fact]
    public async Task The_operator_login_may_change_only_the_account_status_never_its_name_and_never_delete_a_row()
    {
        await using var c = new NpgsqlConnection(_fixture.OperatorConnectionString);
        await c.OpenAsync();

        foreach (var sql in new[]
                 {
                     $"UPDATE account.accounts SET name = 'Hijacked' WHERE id = '{_tenant.AccountId}'",
                     $"UPDATE account.accounts SET deleted_at = now() WHERE id = '{_tenant.AccountId}'",
                     $"DELETE FROM account.accounts WHERE id = '{_tenant.AccountId}'",
                     "DELETE FROM operators.operator_audit",
                     "UPDATE operators.operator_audit SET reason = 'edited'",
                     "DELETE FROM billing.plans",
                 })
        {
            var run = async () =>
            {
                await using var command = new NpgsqlCommand(sql, c);
                await command.ExecuteNonQueryAsync();
            };
            (await run.Should().ThrowAsync<PostgresException>(sql)).Which.SqlState.Should().Be(Denied, sql);
        }
    }

    [Fact]
    public async Task The_operator_login_cannot_call_the_tenant_access_helpers_or_forge_a_tenant_scope()
    {
        await using var c = new NpgsqlConnection(_fixture.OperatorConnectionString);
        await c.OpenAsync();
        await using var tx = await c.BeginTransactionAsync();
        await using (var forge = new NpgsqlCommand(
            $"SELECT set_config('app.request_scope', 'app', true), set_config('app.current_account_id', '{_tenant.AccountId}', true)", c, tx))
            await forge.ExecuteNonQueryAsync();

        var read = async () =>
        {
            await using var command = new NpgsqlCommand("SELECT count(*) FROM work.boards", c, tx);
            await command.ExecuteScalarAsync();
        };

        (await read.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(Denied, "a session value grants a login nothing it holds no privilege for");
    }

    [Fact]
    public async Task The_tenant_logins_hold_nothing_on_the_operator_tables_and_cannot_write_subscriptions()
    {
        foreach (var role in new[] { "notrelix_app", "notrelix_auth", "notrelix_worker", "notrelix_support_readonly" })
        {
            foreach (var table in new[] { "operators.operator_accounts", "operators.operator_audit" })
                (await OwnerScalarAsync(_fixture.OwnerConnectionString, $"SELECT has_any_column_privilege('{role}', '{table}', 'SELECT,INSERT,UPDATE')")).Should().Be("False", $"{role} on {table}");
        }

        foreach (var privilege in new[] { "INSERT", "UPDATE", "DELETE" })
        {
            (await OwnerScalarAsync(_fixture.OwnerConnectionString, $"SELECT has_table_privilege('notrelix_app', 'billing.subscriptions', '{privilege}')"))
                .Should().Be("False", $"{privilege} on billing.subscriptions is the operator's, not the tenant's");
        }

        (await OwnerScalarAsync(_fixture.OwnerConnectionString, "SELECT has_table_privilege('notrelix_app', 'billing.subscriptions', 'SELECT')"))
            .Should().Be("True", "a tenant still reads its own plan");
    }

    [Fact]
    public async Task The_operator_role_is_a_member_of_nothing_and_no_runtime_role_is_a_member_of_it()
    {
        (await OwnerScalarAsync(_fixture.OwnerConnectionString, """
            SELECT count(*) FROM pg_auth_members am
              JOIN pg_roles g ON g.oid = am.roleid JOIN pg_roles m ON m.oid = am.member
             WHERE m.rolname = 'notrelix_operator' OR g.rolname = 'notrelix_operator'
            """)).Should().Be("0");
        (await OwnerScalarAsync(_fixture.OwnerConnectionString,
            "SELECT rolsuper OR rolbypassrls OR rolcreaterole FROM pg_roles WHERE rolname = 'notrelix_operator'")).Should().Be("False");
    }

    // ── suspend and lift ───────────────────────────────────────────────────

    [Fact]
    public async Task Suspending_an_account_blocks_tenant_writes_keeps_reads_and_lifting_restores_writes()
    {
        (await TenantCreateBoardAsync()).StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);

        using var op = As(await SignInAsync());
        var version = await AccountVersionAsync(op);
        var suspend = await op.PostAsJsonAsync($"/operator/v1/accounts/{_tenant.AccountId}/suspend", new { reason = "chargeback dispute", expectedVersion = version });
        suspend.StatusCode.Should().Be(HttpStatusCode.OK, await suspend.Content.ReadAsStringAsync());
        (await suspend.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString().Should().Be("Suspended");

        var blocked = await TenantCreateBoardAsync();
        blocked.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await blocked.Content.ReadAsStringAsync()).Should().Contain("account.suspended");
        using (var tenant = As(TenantToken()))
            (await tenant.GetAsync($"/api/v1/workspaces/{_tenant.WorkspaceId}/boards")).StatusCode.Should().Be(HttpStatusCode.OK, "reads stay open");

        var lift = await op.PostAsJsonAsync($"/operator/v1/accounts/{_tenant.AccountId}/lift-suspension", new { reason = "dispute settled", expectedVersion = version + 1 });
        lift.StatusCode.Should().Be(HttpStatusCode.OK);
        (await TenantCreateBoardAsync()).StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
    }

    [Fact]
    public async Task Suspend_is_audited_with_actor_reason_before_after_and_time_and_a_repeat_is_a_no_op()
    {
        using var op = As(await SignInAsync());
        var version = await AccountVersionAsync(op);
        await op.PostAsJsonAsync($"/operator/v1/accounts/{_tenant.AccountId}/suspend", new { reason = "fraud", expectedVersion = version });
        var again = await op.PostAsJsonAsync($"/operator/v1/accounts/{_tenant.AccountId}/suspend", new { reason = "fraud again", expectedVersion = version + 1 });
        again.StatusCode.Should().Be(HttpStatusCode.OK);

        var entries = await GetJsonAsync(op, $"/operator/v1/audit?accountId={_tenant.AccountId}");

        var entry = entries.EnumerateArray().Should().ContainSingle("the repeat changed nothing, so it left no entry").Subject;
        entry.GetProperty("action").GetString().Should().Be("account.suspend");
        entry.GetProperty("operatorEmail").GetString().Should().Be(OperatorEmail);
        entry.GetProperty("reason").GetString().Should().Be("fraud");
        entry.GetProperty("beforeJson").GetString().Should().Contain("Active");
        entry.GetProperty("afterJson").GetString().Should().Contain("Suspended");
        entry.GetProperty("occurredAt").GetDateTimeOffset().Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(2));
    }

    [Fact]
    public async Task A_stale_version_is_409_a_missing_reason_is_refused_and_neither_changes_the_account()
    {
        using var op = As(await SignInAsync());
        var version = await AccountVersionAsync(op);

        var stale = await op.PostAsJsonAsync($"/operator/v1/accounts/{_tenant.AccountId}/suspend", new { reason = "x", expectedVersion = version + 7 });
        var noReason = await op.PostAsJsonAsync($"/operator/v1/accounts/{_tenant.AccountId}/suspend", new { reason = "  ", expectedVersion = version });
        var noVersion = await op.PostAsJsonAsync($"/operator/v1/accounts/{_tenant.AccountId}/suspend", new { reason = "x", expectedVersion = 0 });

        stale.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ((int)noReason.StatusCode).Should().BeInRange(400, 422);
        noVersion.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetJsonAsync(op, $"/operator/v1/accounts/{_tenant.AccountId}")).GetProperty("account").GetProperty("status").GetString().Should().Be("Active");
        (await GetJsonAsync(op, "/operator/v1/audit")).GetArrayLength().Should().Be(0, "a refused action leaves no audit row");
    }

    [Fact]
    public async Task The_account_list_and_detail_show_identity_status_and_counts_never_content()
    {
        using var op = As(await SignInAsync());

        var page = await GetJsonAsync(op, "/operator/v1/accounts?search=optenant");
        var detail = await GetJsonAsync(op, $"/operator/v1/accounts/{_tenant.AccountId}");

        page.GetProperty("total").GetInt32().Should().Be(1);
        page.GetProperty("items")[0].EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(["id", "name", "slug", "status", "version", "createdAt"]);
        detail.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(["account", "activeMembers", "workspaces"]);
        detail.GetProperty("workspaces").GetInt32().Should().Be(1);
    }

    // ── plan, override, terminate: what the tenant sees changes ────────────

    [Fact]
    public async Task Moving_an_account_to_a_plan_changes_the_limits_the_tenant_sees_and_terminating_returns_it_to_free()
    {
        (await TenantCapabilityAsync("MEMBER_SEAT")).GetProperty("limit").GetInt32().Should().Be(10, "no subscription: the Free plan");

        using var op = As(await SignInAsync());
        var plans = await GetJsonAsync(op, "/operator/v1/billing/plans");
        var pro = plans.EnumerateArray().Single(p => p.GetProperty("familyKey").GetString() == "pro" && p.GetProperty("revision").GetInt32() == 1);
        var change = await op.PutAsJsonAsync($"/operator/v1/accounts/{_tenant.AccountId}/billing/plan", new { planId = pro.GetProperty("id").GetGuid(), reason = "contract 2026-11" });
        change.StatusCode.Should().Be(HttpStatusCode.OK, await change.Content.ReadAsStringAsync());

        (await TenantCapabilityAsync("MEMBER_SEAT")).GetProperty("limit").GetInt32().Should().Be(500, "the tenant now sees the Pro limit");
        using (var tenant = As(TenantToken()))
            (await GetJsonAsync(tenant, $"/api/v1/workspaces/{_tenant.WorkspaceId}/billing/plan")).GetProperty("currentPlan").GetProperty("code").GetString().Should().Be("pro");

        var billing = await GetJsonAsync(op, $"/operator/v1/accounts/{_tenant.AccountId}/billing");
        var terminate = await op.PostAsJsonAsync($"/operator/v1/accounts/{_tenant.AccountId}/billing/terminate",
            new { reason = "account closure", expectedVersion = billing.GetProperty("subscription").GetProperty("version").GetInt64() });
        terminate.StatusCode.Should().Be(HttpStatusCode.OK, await terminate.Content.ReadAsStringAsync());
        (await TenantCapabilityAsync("MEMBER_SEAT")).GetProperty("limit").GetInt32().Should().Be(10, "terminated: back on Free");

        var audit = await GetJsonAsync(op, $"/operator/v1/audit?accountId={_tenant.AccountId}");
        audit.EnumerateArray().Select(e => e.GetProperty("action").GetString()).Should().BeEquivalentTo(["subscription.change-plan", "subscription.terminate"]);
    }

    [Fact]
    public async Task A_manual_override_wins_over_the_plan_and_revoking_it_returns_the_plan_limit()
    {
        using var op = As(await SignInAsync());
        var grant = await op.PostAsJsonAsync($"/operator/v1/accounts/{_tenant.AccountId}/billing/overrides",
            new { featureCode = "MEMBER_SEAT", isUnlimited = false, limit = 777, expiresAt = DateTimeOffset.UtcNow.AddDays(10), reason = "pilot" });
        grant.StatusCode.Should().Be(HttpStatusCode.OK, await grant.Content.ReadAsStringAsync());
        (await TenantCapabilityAsync("MEMBER_SEAT")).GetProperty("limit").GetInt32().Should().Be(777);

        var overrideDto = (await grant.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("overrides").EnumerateArray().Single();
        var revoke = await op.PostAsJsonAsync($"/operator/v1/billing/overrides/{overrideDto.GetProperty("id").GetGuid()}/revoke",
            new { reason = "pilot over", expectedVersion = overrideDto.GetProperty("version").GetInt64() });
        revoke.StatusCode.Should().Be(HttpStatusCode.OK, await revoke.Content.ReadAsStringAsync());
        (await TenantCapabilityAsync("MEMBER_SEAT")).GetProperty("limit").GetInt32().Should().Be(10);
    }

    [Fact]
    public async Task An_override_that_has_expired_no_longer_applies()
    {
        using var op = As(await SignInAsync());
        await op.PostAsJsonAsync($"/operator/v1/accounts/{_tenant.AccountId}/billing/overrides",
            new { featureCode = "MEMBER_SEAT", isUnlimited = false, limit = 777, expiresAt = DateTimeOffset.UtcNow.AddDays(10), reason = "pilot" });
        (await TenantCapabilityAsync("MEMBER_SEAT")).GetProperty("limit").GetInt32().Should().Be(777);

        await using (var owner = _fixture.CreateOwnerContext(new FakeCurrentTenantContext()))
            await owner.Database.ExecuteSqlRawAsync("UPDATE billing.entitlements SET expires_at = now() - interval '1 minute'");

        (await TenantCapabilityAsync("MEMBER_SEAT")).GetProperty("limit").GetInt32().Should().Be(10, "past its expiry the plan limit applies");
    }

    [Fact]
    public async Task An_unlimited_override_and_an_invalid_override_are_handled_as_specified()
    {
        using var op = As(await SignInAsync());
        var bad = await op.PostAsJsonAsync($"/operator/v1/accounts/{_tenant.AccountId}/billing/overrides",
            new { featureCode = "TELEPORTS", isUnlimited = false, limit = 1, reason = "x" });
        bad.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var unlimited = await op.PostAsJsonAsync($"/operator/v1/accounts/{_tenant.AccountId}/billing/overrides",
            new { featureCode = "MEMBER_SEAT", isUnlimited = true, limit = 0, reason = "enterprise pilot" });
        unlimited.StatusCode.Should().Be(HttpStatusCode.OK);
        var cap = await TenantCapabilityAsync("MEMBER_SEAT");
        cap.GetProperty("isUnlimited").GetBoolean().Should().BeTrue();
    }

    // ── plan catalog by revision ───────────────────────────────────────────

    [Fact]
    public async Task A_published_revision_is_on_sale_while_a_subscription_keeps_the_revision_it_references()
    {
        using var op = As(await SignInAsync());
        var pro1 = (await GetJsonAsync(op, "/operator/v1/billing/plans")).EnumerateArray()
            .Single(p => p.GetProperty("familyKey").GetString() == "pro" && p.GetProperty("revision").GetInt32() == 1);
        await op.PutAsJsonAsync($"/operator/v1/accounts/{_tenant.AccountId}/billing/plan", new { planId = pro1.GetProperty("id").GetGuid(), reason = "start" });

        var publish = await op.PostAsJsonAsync("/operator/v1/billing/plans", new
        {
            familyKey = "pro",
            revision = 2,
            name = "Pro",
            tier = "Pro",
            priceAmount = 250000,
            currency = "VND",
            period = "Monthly",
            limits = new[] { new { featureCode = "MEMBER_SEAT", isUnlimited = false, limit = 900 } },
            reason = "new pricing",
        });
        publish.StatusCode.Should().Be(HttpStatusCode.OK, await publish.Content.ReadAsStringAsync());

        using var tenant = As(TenantToken());
        var overview = await GetJsonAsync(tenant, $"/api/v1/workspaces/{_tenant.WorkspaceId}/billing/plan");
        var sellablePro = overview.GetProperty("availablePlans").EnumerateArray().Single(p => p.GetProperty("code").GetString() == "pro");
        sellablePro.GetProperty("limits").EnumerateArray().Single(l => l.GetProperty("code").GetString() == "MEMBER_SEAT").GetProperty("limit").GetInt32()
            .Should().Be(900, "the catalog read comes from the database, with no frontend or backend release");
        (await TenantCapabilityAsync("MEMBER_SEAT")).GetProperty("limit").GetInt32().Should().Be(500, "the subscription keeps revision 1");
    }

    [Fact]
    public async Task Two_limits_for_one_feature_and_a_skipped_revision_are_rejected_and_the_catalog_is_unchanged()
    {
        using var op = As(await SignInAsync());
        var duplicate = await op.PostAsJsonAsync("/operator/v1/billing/plans", new
        {
            familyKey = "team",
            revision = 1,
            name = "Team",
            tier = "Pro",
            priceAmount = 1,
            currency = "VND",
            period = "Monthly",
            limits = new[] { new { featureCode = "MEMBER_SEAT", isUnlimited = false, limit = 1 }, new { featureCode = "MEMBER_SEAT", isUnlimited = false, limit = 2 } },
            reason = "x",
        });
        var skipped = await op.PostAsJsonAsync("/operator/v1/billing/plans", new
        {
            familyKey = "pro",
            revision = 9,
            name = "Pro",
            tier = "Pro",
            priceAmount = 1,
            currency = "VND",
            period = "Monthly",
            limits = Array.Empty<object>(),
            reason = "x",
        });

        ((int)duplicate.StatusCode).Should().BeInRange(400, 422);
        skipped.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await GetJsonAsync(op, "/operator/v1/billing/plans")).GetArrayLength().Should().Be(3);
    }

    [Fact]
    public async Task Deprecating_a_revision_takes_it_off_sale_but_the_subscribed_account_keeps_its_limits()
    {
        using var op = As(await SignInAsync());
        var pro1 = (await GetJsonAsync(op, "/operator/v1/billing/plans")).EnumerateArray()
            .Single(p => p.GetProperty("familyKey").GetString() == "pro");
        await op.PutAsJsonAsync($"/operator/v1/accounts/{_tenant.AccountId}/billing/plan", new { planId = pro1.GetProperty("id").GetGuid(), reason = "start" });

        var deprecate = await op.PostAsJsonAsync($"/operator/v1/billing/plans/{pro1.GetProperty("id").GetGuid()}/deprecate",
            new { reason = "retired", expectedVersion = pro1.GetProperty("version").GetInt64() });
        deprecate.StatusCode.Should().Be(HttpStatusCode.OK, await deprecate.Content.ReadAsStringAsync());

        using var tenant = As(TenantToken());
        var overview = await GetJsonAsync(tenant, $"/api/v1/workspaces/{_tenant.WorkspaceId}/billing/plan");
        overview.GetProperty("availablePlans").EnumerateArray().Select(p => p.GetProperty("code").GetString()).Should().NotContain("pro");
        (await TenantCapabilityAsync("MEMBER_SEAT")).GetProperty("limit").GetInt32().Should().Be(500);

        var assign = await op.PutAsJsonAsync($"/operator/v1/accounts/{Guid.NewGuid()}/billing/plan", new { planId = pro1.GetProperty("id").GetGuid(), reason = "x" });
        assign.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── the audit trail and the operator tables ────────────────────────────

    [Fact]
    public async Task Every_operator_action_leaves_an_audit_row_in_the_same_transaction_as_its_change()
    {
        using var op = As(await SignInAsync());
        var version = await AccountVersionAsync(op);
        await op.PostAsJsonAsync($"/operator/v1/accounts/{_tenant.AccountId}/suspend", new { reason = "r1", expectedVersion = version });
        await op.PostAsJsonAsync($"/operator/v1/accounts/{_tenant.AccountId}/billing/overrides",
            new { featureCode = "STORAGE_GB", isUnlimited = false, limit = 9, reason = "r2" });

        (await OwnerScalarAsync(_fixture.OwnerConnectionString, "SELECT count(*) FROM operators.operator_audit")).Should().Be("2");
        (await OwnerScalarAsync(_fixture.OwnerConnectionString, "SELECT count(DISTINCT operator_id) FROM operators.operator_audit")).Should().Be("1");
        (await OwnerScalarAsync(_fixture.OwnerConnectionString, "SELECT count(*) FROM events.domain_event_logs")).Should().NotBe("0", "the change published its events through the outbox");
    }
}
