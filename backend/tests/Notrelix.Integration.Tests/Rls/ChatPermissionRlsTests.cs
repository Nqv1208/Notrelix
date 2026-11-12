using Npgsql;
using Notrelix.Domain.Chat.Permissions;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// Channel permission overrides are workspace-scoped at the database (UC-CHT-H01, CHT-002): another Workspace reads none of them and
/// cannot write, change or remove one of this Workspace. Needs Docker locally.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class ChatPermissionRlsTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private Guid _spaceId;

    public ChatPermissionRlsTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "chat-perm-a@example.com", "PermA", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "chat-perm-b@example.com", "PermB", "hash");

        var channel = Space.CreateChannel(_a.AccountId, _a.WorkspaceId, SpaceKind.ChannelPublic, "read-only", null, null, 1, _a.User.Id, Now);
        channel.SetPermissionOverrides(
            [new ChannelPermissionEntry(ChannelPermissionSubject.Member, ChannelAction.Post, ChannelPermissionEffect.Deny)], _a.User.Id, Now);
        _spaceId = channel.Id;

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.ChatSpaces.Add(channel);
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
    public async Task OwningWorkspaceReadsItsOwnOverride()
    {
        var count = await AsAppAsync(_a, $"SELECT count(*) FROM chat.permission_overrides WHERE space_id = '{_spaceId}'");

        count.Scalar.Should().Be("1", "the positive control: the owning Workspace reads its own override");
    }

    [Fact]
    public async Task AnotherWorkspaceReadsNoOverride()
    {
        var count = await AsAppAsync(_b, $"SELECT count(*) FROM chat.permission_overrides WHERE space_id = '{_spaceId}'");

        count.Scalar.Should().Be("0");
    }

    [Fact]
    public async Task AnotherWorkspaceCannotWriteAnOverrideIntoThisWorkspace()
    {
        var act = () => AsAppAsync(_b,
            $"INSERT INTO chat.permission_overrides (space_id, subject, action, account_id, workspace_id, effect) " +
            $"VALUES ('{_spaceId}', 'Guest', 'Post', '{_a.AccountId}', '{_a.WorkspaceId}', 'Deny')", execute: true);

        await act.Should().ThrowAsync<Npgsql.PostgresException>("the row policy refuses an override row for another Workspace");
    }

    [Fact]
    public async Task AnotherWorkspaceCannotChangeOrRemoveAnOverrideOfThisWorkspace()
    {
        var changed = await AsAppAsync(_b, $"UPDATE chat.permission_overrides SET effect = 'Allow' WHERE space_id = '{_spaceId}'", execute: true);
        var removed = await AsAppAsync(_b, $"DELETE FROM chat.permission_overrides WHERE space_id = '{_spaceId}'", execute: true);

        changed.Rows.Should().Be(0);
        removed.Rows.Should().Be(0);
        var still = await AsAppAsync(_a, $"SELECT effect FROM chat.permission_overrides WHERE space_id = '{_spaceId}'");
        still.Scalar.Should().Be("Deny");
    }
}
