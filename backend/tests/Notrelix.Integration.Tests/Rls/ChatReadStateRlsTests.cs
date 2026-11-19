using Microsoft.Extensions.Options;
using Npgsql;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Data.Chat;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// The read state lives on chat.memberships, which the Workspace row policy already covers (008). A Workspace reads and moves
/// only its own boundaries; another Workspace reads none and moves none, through SQL or through the store.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class ChatReadStateRlsTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private Guid _spaceId;

    public ChatReadStateRlsTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "chat-read-a@example.com", "ReadA", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "chat-read-b@example.com", "ReadB", "hash");

        // The creator of A's channel is its manager and a member, so the membership row of the creator exists in A.
        var channel = Space.CreateChannel(_a.AccountId, _a.WorkspaceId, SpaceKind.ChannelPublic, "read-a", null, null, 1, _a.User.Id, Now);
        _spaceId = channel.Id;

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.ChatSpaces.Add(channel);
        await ctx.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<string?> AsAppScalarAsync(AppRoleTenant tenant, string sql)
    {
        await using var connection = new NpgsqlConnection(_fixture.AppConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetTenantAsync(connection, transaction, tenant);

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        var result = await command.ExecuteScalarAsync();
        return result?.ToString();
    }

    private async Task<int> AsAppNonQueryAsync(AppRoleTenant tenant, string sql)
    {
        await using var connection = new NpgsqlConnection(_fixture.AppConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetTenantAsync(connection, transaction, tenant);

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        var rows = await command.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
        return rows;
    }

    private static async Task SetTenantAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, AppRoleTenant tenant)
    {
        await using var scope = new NpgsqlCommand(
            "SELECT set_config('app.current_user_id', @u, true), set_config('app.current_account_id', @a, true), " +
            "set_config('app.current_workspace_id', @w, true), set_config('app.request_scope', 'app', true)",
            connection, transaction);
        scope.Parameters.AddWithValue("u", tenant.User.Id.ToString());
        scope.Parameters.AddWithValue("a", tenant.AccountId.ToString());
        scope.Parameters.AddWithValue("w", tenant.WorkspaceId.ToString());
        await scope.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task OwningWorkspaceMovesItsOwnBoundary()
    {
        var rows = await AsAppNonQueryAsync(_a, $"UPDATE chat.memberships SET read_sequence = 1 WHERE space_id = '{_spaceId}'");
        var value = await AsAppScalarAsync(_a, $"SELECT read_sequence FROM chat.memberships WHERE space_id = '{_spaceId}'");

        rows.Should().Be(1, "the positive control: the owning Workspace updates its own membership row");
        value.Should().Be("1");
    }

    [Fact]
    public async Task AnotherWorkspaceCannotMoveTheBoundary()
    {
        var rows = await AsAppNonQueryAsync(_b, $"UPDATE chat.memberships SET read_sequence = 9 WHERE space_id = '{_spaceId}'");
        var value = await AsAppScalarAsync(_a, $"SELECT read_sequence FROM chat.memberships WHERE space_id = '{_spaceId}'");

        rows.Should().Be(0, "the row policy hides the other Workspace's membership rows from the update");
        value.Should().Be("0", "the owning Workspace's boundary is untouched");
    }

    [Fact]
    public async Task AnotherWorkspaceReadsNoBoundary()
    {
        var count = await AsAppScalarAsync(_b, $"SELECT count(*) FROM chat.memberships WHERE space_id = '{_spaceId}'");

        count.Should().Be("0");
    }

    [Fact]
    public async Task TheStoreRefusesToMoveABoundaryFromAnotherWorkspace()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(_b.AccountId, _b.WorkspaceId, _b.User.Id);

        await using var db = _fixture.CreateAppContext(tenant);
        var moved = await new ChatReadStateStore(db).AdvanceAsync(_b.WorkspaceId, _spaceId, _a.User.Id, 5, CancellationToken.None);

        moved.Should().BeFalse("the membership is in another Workspace, so the store finds no row to move");
    }

    [Fact]
    public async Task TheStoreRefusesToRewindABoundaryFromAnotherWorkspace()
    {
        await AsAppNonQueryAsync(_a, $"UPDATE chat.memberships SET read_sequence = 5 WHERE space_id = '{_spaceId}'");
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(_b.AccountId, _b.WorkspaceId, _b.User.Id);

        await using var db = _fixture.CreateAppContext(tenant);
        var moved = await new ChatReadStateStore(db).RewindAsync(_b.WorkspaceId, _spaceId, _a.User.Id, 1, CancellationToken.None);
        var value = await AsAppScalarAsync(_a, $"SELECT read_sequence FROM chat.memberships WHERE space_id = '{_spaceId}'");

        moved.Should().BeFalse("the membership is in another Workspace, so the store finds no row to rewind");
        value.Should().Be("5", "the owning Workspace's boundary is untouched");
    }

    [Fact]
    public async Task TheOwningWorkspaceRewindsItsOwnBoundaryUnderTheAppRole_AndNeverForward()
    {
        await AsAppNonQueryAsync(_a, $"UPDATE chat.memberships SET read_sequence = 5 WHERE space_id = '{_spaceId}'");
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(_a.AccountId, _a.WorkspaceId, _a.User.Id);

        // One request: a transaction with the Workspace session context applied, as the data session does.
        async Task<bool> RewindAsync(long target)
        {
            await using var db = _fixture.CreateAppContext(tenant);
            await using var tx = await db.Database.BeginTransactionAsync();
            await new RlsSessionContext(db, Options.Create(new RlsOptions { SetSessionContext = true }), tenant).ApplyAsync(default);
            var moved = await new ChatReadStateStore(db).RewindAsync(_a.WorkspaceId, _spaceId, _a.User.Id, target, CancellationToken.None);
            await tx.CommitAsync();
            return moved;
        }

        (await RewindAsync(2)).Should().BeTrue();
        (await AsAppScalarAsync(_a, $"SELECT read_sequence FROM chat.memberships WHERE space_id = '{_spaceId}'")).Should().Be("2");

        await RewindAsync(4);
        (await AsAppScalarAsync(_a, $"SELECT read_sequence FROM chat.memberships WHERE space_id = '{_spaceId}'"))
            .Should().Be("2", "a target ahead of the boundary never reads anything forward");
    }
}
