using Notrelix.Integration.Tests.Containers;
using Npgsql;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// Followers of a resource are Workspace data: the application login reads and writes follower rows only
/// inside the Workspace it holds, and a person follows a resource once.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class ResourceWatcherPolicyTests : IAsyncLifetime
{
    private const string PostgresRlsViolation = "42501";
    private const string PostgresUniqueViolation = "23505";

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;

    public ResourceWatcherPolicyTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "watch-a@example.com", "WatchA", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "watch-b@example.com", "WatchB", "hash");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static string Insert(AppRoleTenant tenant, Guid user, Guid item) =>
        "INSERT INTO collab.resource_watchers (id, account_id, workspace_id, target_type, target_id, user_id, watch_level, created_at, version) " +
        $"VALUES (gen_random_uuid(), '{tenant.AccountId}', '{tenant.WorkspaceId}', 'work-management.board-item', '{item}', '{user}', 'All', now(), 1)";

    private async Task OwnerExecAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_fixture.OwnerConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<string?> AsAppAsync(AppRoleTenant tenant, string sql)
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

        try
        {
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            var result = await command.ExecuteScalarAsync();
            return result?.ToString() ?? "ok";
        }
        catch (PostgresException e) when (e.SqlState is PostgresRlsViolation or PostgresUniqueViolation)
        {
            return e.SqlState;
        }
    }

    [Fact]
    public async Task AFollowerRowIsVisibleAndWritableOnlyInsideItsOwnWorkspace()
    {
        var itemA = Guid.NewGuid();
        var itemB = Guid.NewGuid();
        await OwnerExecAsync(Insert(_b, _b.User.Id, itemB));

        (await AsAppAsync(_a, Insert(_a, _a.User.Id, itemA))).Should().Be("ok", "the caller's own workspace");
        (await AsAppAsync(_a, Insert(_b, _a.User.Id, itemB))).Should().Be(PostgresRlsViolation, "another tenant's workspace");

        (await AsAppAsync(_a, $"SELECT count(*) FROM collab.resource_watchers WHERE target_id = '{itemB}'"))
            .Should().Be("0", "another tenant's follower is invisible");
        (await AsAppAsync(_b, $"SELECT count(*) FROM collab.resource_watchers WHERE target_id = '{itemB}'"))
            .Should().Be("1", "the owner tenant still sees its own");
    }

    [Fact]
    public async Task APersonFollowsAResourceOnce()
    {
        var item = Guid.NewGuid();
        await OwnerExecAsync(Insert(_a, _a.User.Id, item));

        (await AsAppAsync(_a, Insert(_a, _a.User.Id, item))).Should().Be(PostgresUniqueViolation);
    }
}
