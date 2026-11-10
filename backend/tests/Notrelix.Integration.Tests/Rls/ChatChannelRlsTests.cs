using Npgsql;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// Chat channels and their memberships are workspace-scoped at the database: a person of another Workspace reads
/// no channel and no membership row of this one, while the owning Workspace reads its own. Channel-level
/// visibility (a private channel a Workspace member is not in) is decided by the application, not by this policy.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class ChatChannelRlsTests : IAsyncLifetime
{
    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private Guid _channelId;

    public ChatChannelRlsTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "chat-rls-a@example.com", "ChatA", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "chat-rls-b@example.com", "ChatB", "hash");

        var now = DateTimeOffset.UtcNow;
        var channel = Space.CreateChannel(_a.AccountId, _a.WorkspaceId, SpaceKind.ChannelPrivate, "secret", null, null, 1, _a.User.Id, now);
        _channelId = channel.Id;

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.ChatSpaces.Add(channel);
        await ctx.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

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

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        var result = await command.ExecuteScalarAsync();
        return result?.ToString();
    }

    [Fact]
    public async Task AnotherWorkspaceReadsNoChannelOfThisOne()
    {
        var count = await AsAppAsync(_b, $"SELECT count(*) FROM chat.spaces WHERE id = '{_channelId}'");

        count.Should().Be("0");
    }

    [Fact]
    public async Task AnotherWorkspaceReadsNoMembershipOfThisChannel()
    {
        var count = await AsAppAsync(_b, $"SELECT count(*) FROM chat.memberships WHERE space_id = '{_channelId}'");

        count.Should().Be("0");
    }

    [Fact]
    public async Task TheOwningWorkspaceReadsItsChannelAndItsManager()
    {
        var channels = await AsAppAsync(_a, $"SELECT count(*) FROM chat.spaces WHERE id = '{_channelId}'");
        var memberships = await AsAppAsync(_a, $"SELECT count(*) FROM chat.memberships WHERE space_id = '{_channelId}'");

        channels.Should().Be("1");
        memberships.Should().Be("1");
    }
}
