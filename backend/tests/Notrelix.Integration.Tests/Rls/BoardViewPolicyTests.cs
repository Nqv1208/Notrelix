using Notrelix.Integration.Tests.Containers;
using Npgsql;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// Saved views are Workspace data: the application login reads and writes view rows only inside the
/// Workspace it holds, and a row keeps the visibility it was saved with (a new row is a Board view).
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class BoardViewPolicyTests : IAsyncLifetime
{
    private const string PostgresRlsViolation = "42501";

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;

    public BoardViewPolicyTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "views-a@example.com", "ViewsA", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "views-b@example.com", "ViewsB", "hash");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static string Insert(AppRoleTenant tenant, AppRoleTenant board, string name, string? visibility = null) =>
        "INSERT INTO work.board_views (id, account_id, workspace_id, board_id, name, type, config, is_default, is_archived, created_at, created_by"
        + (visibility is null ? "" : ", visibility") + ") "
        + $"VALUES (gen_random_uuid(), '{tenant.AccountId}', '{tenant.WorkspaceId}', '{board.BoardId}', '{name}', 'Table', '{{}}'::jsonb, false, false, now(), '{tenant.User.Id}'"
        + (visibility is null ? "" : $", '{visibility}'") + ")";

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
        catch (PostgresException e) when (e.SqlState == PostgresRlsViolation)
        {
            return e.SqlState;
        }
    }

    [Fact]
    public async Task AViewRowIsVisibleAndWritableOnlyInsideItsOwnWorkspace()
    {
        await OwnerExecAsync(Insert(_b, _b, "Theirs"));

        (await AsAppAsync(_a, Insert(_a, _a, "Mine", "Private"))).Should().Be("ok", "the caller's own workspace");
        (await AsAppAsync(_a, Insert(_b, _b, "Smuggled"))).Should().Be(PostgresRlsViolation, "another tenant's workspace");

        (await AsAppAsync(_a, "SELECT count(*) FROM work.board_views WHERE name = 'Theirs'"))
            .Should().Be("0", "another tenant's view is invisible");
        (await AsAppAsync(_b, "SELECT count(*) FROM work.board_views WHERE name = 'Theirs'"))
            .Should().Be("1", "the owner tenant still sees its own");
    }

    [Fact]
    public async Task ANewRowIsABoardViewAndAPrivateOneKeepsItsVisibility()
    {
        await OwnerExecAsync(Insert(_a, _a, "Plain"));
        await OwnerExecAsync(Insert(_a, _a, "Personal", "Private"));

        (await AsAppAsync(_a, "SELECT visibility FROM work.board_views WHERE name = 'Plain'")).Should().Be("Board");
        (await AsAppAsync(_a, "SELECT visibility FROM work.board_views WHERE name = 'Personal'")).Should().Be("Private");
    }
}
