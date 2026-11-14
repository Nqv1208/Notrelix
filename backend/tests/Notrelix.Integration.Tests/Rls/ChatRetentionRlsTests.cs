using Npgsql;
using Notrelix.Domain.Chat.Exports;
using Notrelix.Domain.Chat.Retention;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// The retention rule and the export jobs are workspace-scoped at the database (UC-CHT-H05, H06, CHT-002): another Workspace reads none of
/// them and cannot write, change or remove one of this Workspace. Needs Docker locally.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class ChatRetentionRlsTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private Guid _jobId;

    public ChatRetentionRlsTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "chat-ret-a@example.com", "RetA", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "chat-ret-b@example.com", "RetB", "hash");

        var policy = RetentionPolicy.CreateDefault(_a.AccountId, _a.WorkspaceId, Now);
        policy.Change(90, RetentionMode.Purge, _a.User.Id, Now);
        var job = ChatExportJob.Request(_a.AccountId, _a.WorkspaceId, _a.User.Id, Guid.NewGuid(), null, null, Now);
        _jobId = job.Id;

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.ChatRetentionPolicies.Add(policy);
        ctx.ChatExportJobs.Add(job);
        await ctx.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(string? Scalar, int Rows)> AsAppAsync(AppRoleTenant tenant, string sql, bool execute = false)
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

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        if (execute)
            return (null, await command.ExecuteNonQueryAsync());
        return ((await command.ExecuteScalarAsync())?.ToString(), 0);
    }

    [Fact]
    public async Task OwningWorkspaceReadsItsRuleAndItsExport()
    {
        (await AsAppAsync(_a, "SELECT count(*) FROM chat.retention_policies")).Scalar.Should().Be("1");
        (await AsAppAsync(_a, $"SELECT count(*) FROM chat.export_jobs WHERE id = '{_jobId}'")).Scalar.Should().Be("1");
    }

    [Fact]
    public async Task AnotherWorkspaceReadsNeitherTheRuleNorTheExport()
    {
        (await AsAppAsync(_b, $"SELECT count(*) FROM chat.retention_policies WHERE workspace_id = '{_a.WorkspaceId}'")).Scalar.Should().Be("0");
        (await AsAppAsync(_b, $"SELECT count(*) FROM chat.export_jobs WHERE id = '{_jobId}'")).Scalar.Should().Be("0");
    }

    [Fact]
    public async Task AnotherWorkspaceCannotWriteARuleOrAnExportIntoThisWorkspace()
    {
        var rule = () => AsAppAsync(_b,
            $"INSERT INTO chat.retention_policies (id, account_id, workspace_id, retention_days, mode, created_at, version) " +
            $"VALUES (gen_random_uuid(), '{_a.AccountId}', '{_a.WorkspaceId}', 30, 'purge', now(), 1)", execute: true);
        await rule.Should().ThrowAsync<PostgresException>("the row policy refuses a rule row for another Workspace");
    }

    [Fact]
    public async Task AnotherWorkspaceCannotChangeOrRemoveThem()
    {
        var changed = await AsAppAsync(_b, "UPDATE chat.retention_policies SET retention_days = 7", execute: true);
        var removed = await AsAppAsync(_b, $"DELETE FROM chat.export_jobs WHERE id = '{_jobId}'", execute: true);

        changed.Rows.Should().Be(0);
        removed.Rows.Should().Be(0);
        (await AsAppAsync(_a, "SELECT retention_days FROM chat.retention_policies")).Scalar.Should().Be("90");
    }
}
