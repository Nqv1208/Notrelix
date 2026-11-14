using Microsoft.Extensions.Options;
using Notrelix.Domain.Automation.Executions;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Automation.RulesEngine;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Npgsql;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// UC-AUT-F03 and UC-AUT-N05. Automation rules and executions are workspace-scoped under row-level security:
/// the application login sees, changes and creates only rows of a workspace it holds a grant for. The
/// rule-loading filter of a fact (workspace, account, active) therefore never admits another workspace's
/// rule, even when both workspaces carry the same group id value in their trigger configuration.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class AutomationTenantIsolationTests : IAsyncLifetime
{
    private const string PostgresRlsViolation = "42501";

    /// <summary>The same group id value in both workspaces: only the fact's own workspace may load its rule.</summary>
    private static readonly string SharedGroupId = Guid.Parse("7c1d2e3f-4a5b-4c6d-8e9f-0a1b2c3d4e5f").ToString();

    private static readonly DateTimeOffset Now = new(2026, 10, 20, 9, 0, 0, TimeSpan.Zero);

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private Guid _ruleA;
    private Guid _ruleB;
    private Guid _executionA;
    private Guid _executionB;

    public AutomationTenantIsolationTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "automation-a@example.com", "AutoA", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "automation-b@example.com", "AutoB", "hash");

        _ruleA = await SeedRuleAsync(_a, "A group rule");
        _ruleB = await SeedRuleAsync(_b, "B group rule");
        _executionA = await SeedExecutionAsync(_a, _ruleA);
        _executionB = await SeedExecutionAsync(_b, _ruleB);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task FactOfWorkspaceOne_LoadsOnlyWorkspaceOneRules_EvenWhenAnotherWorkspaceHasTheSameGroupId()
    {
        // The rule-loading filter of a fact of workspace A, as the application login runs it.
        var loaded = await AsAppAsync(_a, (c, t) => IdsAsync(c, t,
            "SELECT id::text FROM automation.automation_rules " +
            $"WHERE account_id = '{_a.AccountId}' AND workspace_id = '{_a.WorkspaceId}' AND status = 'Active' " +
            $"AND configuration::text LIKE '%{SharedGroupId}%'"));
        loaded.Should().BeEquivalentTo([_ruleA.ToString()], "a fact of W1 loads W1's rule only");

        // The same filter pointed at W2 finds nothing for a W1 holder: W2's rule is not reachable from W1.
        var foreign = await AsAppAsync(_a, (c, t) => IdsAsync(c, t,
            "SELECT id::text FROM automation.automation_rules " +
            $"WHERE account_id = '{_b.AccountId}' AND workspace_id = '{_b.WorkspaceId}' AND status = 'Active' " +
            $"AND configuration::text LIKE '%{SharedGroupId}%'"));
        foreign.Should().BeEmpty("W2's rule must not load for a fact of W1");

        // And symmetrically for W2.
        var loadedB = await AsAppAsync(_b, (c, t) => IdsAsync(c, t,
            "SELECT id::text FROM automation.automation_rules " +
            $"WHERE account_id = '{_b.AccountId}' AND workspace_id = '{_b.WorkspaceId}' AND status = 'Active' " +
            $"AND configuration::text LIKE '%{SharedGroupId}%'"));
        loadedB.Should().BeEquivalentTo([_ruleB.ToString()]);
    }

    [Fact]
    public async Task AppRole_SeesOnlyItsOwnWorkspaceRulesAndExecutions()
    {
        (await AsAppAsync(_a, (c, t) => IdsAsync(c, t, "SELECT id::text FROM automation.automation_rules")))
            .Should().BeEquivalentTo([_ruleA.ToString()], "a W1 holder reads W1's rules and nothing of W2");
        (await AsAppAsync(_a, (c, t) => IdsAsync(c, t, "SELECT id::text FROM automation.automation_executions")))
            .Should().BeEquivalentTo([_executionA.ToString()], "a W1 holder reads W1's executions and nothing of W2");

        (await AsAppAsync(_b, (c, t) => IdsAsync(c, t, "SELECT id::text FROM automation.automation_rules")))
            .Should().BeEquivalentTo([_ruleB.ToString()]);
        (await AsAppAsync(_b, (c, t) => IdsAsync(c, t, "SELECT id::text FROM automation.automation_executions")))
            .Should().BeEquivalentTo([_executionB.ToString()]);

        (await AsAppAsync(_a, (c, t) => IdsAsync(c, t, $"SELECT id::text FROM automation.automation_rules WHERE id = '{_ruleB}'")))
            .Should().BeEmpty("a W2 rule id is not found from W1, the same answer as a missing row");
    }

    [Fact]
    public async Task AppRole_CannotUpdateOrDeleteAnotherWorkspacesRuleOrExecution()
    {
        var updatedRules = await AsAppAsync(_a, (c, t) => ExecuteAsync(c, t,
            $"UPDATE automation.automation_rules SET name = 'hijacked' WHERE id = '{_ruleB}'"));
        var deletedRules = await AsAppAsync(_a, (c, t) => ExecuteAsync(c, t,
            $"DELETE FROM automation.automation_rules WHERE id = '{_ruleB}'"));
        var updatedExecutions = await AsAppAsync(_a, (c, t) => ExecuteAsync(c, t,
            $"UPDATE automation.automation_executions SET status = 'Failed' WHERE id = '{_executionB}'"));
        var deletedExecutions = await AsAppAsync(_a, (c, t) => ExecuteAsync(c, t,
            $"DELETE FROM automation.automation_executions WHERE id = '{_executionB}'"));

        updatedRules.Should().Be(0, "W2's rule is invisible to W1, so an update matches no row");
        deletedRules.Should().Be(0);
        updatedExecutions.Should().Be(0);
        deletedExecutions.Should().Be(0);

        (await OwnerScalarAsync($"SELECT name FROM automation.automation_rules WHERE id = '{_ruleB}'"))
            .Should().Be("B group rule", "W2's rule is unchanged by W1");
        (await OwnerScalarAsync($"SELECT count(*)::text FROM automation.automation_executions WHERE id = '{_executionB}'"))
            .Should().Be("1", "W2's execution still exists");
    }

    [Fact]
    public async Task AppRole_CannotCreateRuleOrExecutionInAnotherWorkspace_ButCanCreateInItsOwn()
    {
        (await InsertIsDeniedAsync(_a, db => db.AutomationRules.Add(NewRule(_b, "smuggled rule"))))
            .Should().BeTrue("a rule may not be created in another workspace");
        (await InsertIsDeniedAsync(_a, db => db.AutomationExecutions.Add(NewExecution(_b, _ruleB))))
            .Should().BeTrue("an execution may not be created in another workspace");

        (await InsertIsDeniedAsync(_a, db => db.AutomationRules.Add(NewRule(_a, "own rule"))))
            .Should().BeFalse("a W1 holder may create a rule in W1");
    }

    private static AutomationConfiguration GroupMoveConfiguration() => AutomationConfiguration.Create(
        AutomationTriggerDefinition.Create("ItemMovedToGroup", $$"""{"groupId":"{{SharedGroupId}}"}"""),
        AutomationActionDefinition.Create("Webhook", """{"webhookPath":"group-moved"}"""));

    private static AutomationRule NewRule(AppRoleTenant tenant, string name) =>
        AutomationRule.Create(tenant.AccountId, tenant.WorkspaceId, name, GroupMoveConfiguration(), tenant.User.Id, Now);

    private static AutomationExecution NewExecution(AppRoleTenant tenant, Guid ruleId) =>
        AutomationExecution.Create(tenant.AccountId, tenant.WorkspaceId, ruleId, Guid.NewGuid(), Now);

    private async Task<Guid> SeedRuleAsync(AppRoleTenant tenant, string name)
    {
        var rule = NewRule(tenant, name);
        rule.Enable(tenant.User.Id, Now);

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.AutomationRules.Add(rule);
        await ctx.SaveChangesAsync();
        return rule.Id;
    }

    private async Task<Guid> SeedExecutionAsync(AppRoleTenant tenant, Guid ruleId)
    {
        var execution = NewExecution(tenant, ruleId);

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.AutomationExecutions.Add(execution);
        await ctx.SaveChangesAsync();
        return execution.Id;
    }

    /// <summary>
    /// Tries one EF insert as the application login inside the workspace scope of <paramref name="scope"/>.
    /// Returns true when row-level security refuses it (SQLSTATE 42501).
    /// </summary>
    private async Task<bool> InsertIsDeniedAsync(AppRoleTenant scope, Action<ApplicationDbContext> add)
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(scope.AccountId, scope.WorkspaceId, scope.User.Id);

        await using var db = _fixture.CreateAppContext(tenant);
        var rls = new RlsSessionContext(db, Options.Create(new RlsOptions { SetSessionContext = true }), tenant);
        await using var tx = await db.Database.BeginTransactionAsync();
        await rls.ApplyAsync(default);

        add(db);
        try
        {
            await db.SaveChangesAsync();
            return false;
        }
        catch (Exception e) when (e.GetBaseException() is PostgresException { SqlState: PostgresRlsViolation })
        {
            return true;
        }
    }

    /// <summary>Runs <paramref name="work"/> on the application login inside the workspace scope of <paramref name="tenant"/>.</summary>
    private async Task<T> AsAppAsync<T>(AppRoleTenant tenant, Func<NpgsqlConnection, NpgsqlTransaction, Task<T>> work)
    {
        await using var connection = new NpgsqlConnection(_fixture.AppConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var scope = new NpgsqlCommand(
            "SELECT set_config('app.current_user_id', @u, true), set_config('app.current_account_id', @a, true), " +
            "set_config('app.current_workspace_id', @w, true), set_config('app.request_scope', 'app', true)",
            connection, transaction))
        {
            scope.Parameters.AddWithValue("u", tenant.User.Id.ToString());
            scope.Parameters.AddWithValue("a", tenant.AccountId.ToString());
            scope.Parameters.AddWithValue("w", tenant.WorkspaceId.ToString());
            await scope.ExecuteNonQueryAsync();
        }

        return await work(connection, transaction);
    }

    private static async Task<List<string>> IdsAsync(NpgsqlConnection c, NpgsqlTransaction t, string sql)
    {
        await using var command = new NpgsqlCommand(sql, c, t);
        var ids = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            ids.Add(reader.GetString(0));
        return ids;
    }

    private static async Task<int> ExecuteAsync(NpgsqlConnection c, NpgsqlTransaction t, string sql)
    {
        await using var command = new NpgsqlCommand(sql, c, t);
        return await command.ExecuteNonQueryAsync();
    }

    private async Task<string?> OwnerScalarAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_fixture.OwnerConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return (await command.ExecuteScalarAsync())?.ToString();
    }
}
