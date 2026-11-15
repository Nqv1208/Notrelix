using Npgsql;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// The chat row policy reads the caller's grants once per statement (030_chat_policy_fast_path.sql) and keeps
/// ops.has_workspace_access as its second arm. The rows a caller sees must be exactly the rows that function accepts:
/// an active member sees the Workspace's rows, another Workspace's member, a suspended or removed member, and a
/// session with no user see none, and for each of them the policy agrees with the function row for row.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class ChatPolicyFastPathRlsTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);
    private static readonly string[] Tables = ["chat.spaces", "chat.messages", "chat.memberships"];

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private User _activeMember = null!;
    private User _suspended = null!;
    private User _removed = null!;

    public ChatPolicyFastPathRlsTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "chat-fast-a@example.com", "FastA", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "chat-fast-b@example.com", "FastB", "hash");
        _activeMember = await AppRoleMembers.AddAsync(_fixture, _a, "ActiveMember", WorkspaceRole.Member, WorkspaceMemberStatus.Active, "hash");
        _suspended = await AppRoleMembers.AddAsync(_fixture, _a, "SuspendedMember", WorkspaceRole.Member, WorkspaceMemberStatus.Suspended, "hash");
        _removed = await AppRoleMembers.AddAsync(_fixture, _a, "RemovedMember", WorkspaceRole.Member, WorkspaceMemberStatus.Removed, "hash");

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        foreach (var tenant in new[] { _a, _b })
        {
            var channel = Space.CreateChannel(tenant.AccountId, tenant.WorkspaceId, SpaceKind.ChannelPublic, "general", null, null, 1, tenant.User.Id, Now);
            ctx.ChatSpaces.Add(channel);
            for (var sequence = 1; sequence <= 3; sequence++)
                ctx.ChatMessages.Add(Message.CreateUserMessage(tenant.AccountId, tenant.WorkspaceId, channel.Id, sequence, tenant.User.Id, $"m{sequence}", $"c{sequence}", null, Now));
        }

        await ctx.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task SetSessionAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid? userId, AppRoleTenant tenant)
    {
        await using var scope = new NpgsqlCommand(
            "SELECT set_config('app.current_user_id', @u, true), set_config('app.current_account_id', @a, true), " +
            "set_config('app.current_workspace_id', @w, true), set_config('app.request_scope', 'app', true)",
            connection, transaction);
        scope.Parameters.AddWithValue("u", userId?.ToString() ?? string.Empty);
        scope.Parameters.AddWithValue("a", tenant.AccountId.ToString());
        scope.Parameters.AddWithValue("w", tenant.WorkspaceId.ToString());
        await scope.ExecuteNonQueryAsync();
    }

    /// <summary>What the policy lets the application login read from the table, in the tenant's Workspace.</summary>
    private async Task<long> SeenByPolicyAsync(Guid? userId, AppRoleTenant tenant, string table)
    {
        await using var connection = new NpgsqlConnection(_fixture.AppConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetSessionAsync(connection, transaction, userId, tenant);
        await using var command = new NpgsqlCommand($"SELECT count(*) FROM {table} WHERE workspace_id = @w", connection, transaction);
        command.Parameters.AddWithValue("w", tenant.WorkspaceId);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    /// <summary>What the original rule accepts, asked as the owner (which the policy does not bind) in the same session.</summary>
    private async Task<long> AcceptedByFunctionAsync(Guid? userId, AppRoleTenant tenant, string table)
    {
        await using var connection = new NpgsqlConnection(_fixture.OwnerConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetSessionAsync(connection, transaction, userId, tenant);
        await using var command = new NpgsqlCommand(
            $"SELECT count(*) FROM {table} WHERE workspace_id = @w AND ops.has_workspace_access(account_id, workspace_id)", connection, transaction);
        command.Parameters.AddWithValue("w", tenant.WorkspaceId);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    [Fact]
    public async Task AnActiveMemberReadsTheWorkspaceRowsAndNothingOfAnother()
    {
        (await SeenByPolicyAsync(_activeMember.Id, _a, "chat.messages")).Should().Be(3, "the positive control: the member reads this Workspace's messages");
        (await SeenByPolicyAsync(_activeMember.Id, _a, "chat.spaces")).Should().Be(1);
        (await SeenByPolicyAsync(_a.User.Id, _b, "chat.messages")).Should().Be(0, "the owner of A holds no grant in B");
    }

    [Fact]
    public async Task ASuspendedOrRemovedMemberAndASessionWithNoUserReadNothing()
    {
        (await SeenByPolicyAsync(_suspended.Id, _a, "chat.messages")).Should().Be(0);
        (await SeenByPolicyAsync(_removed.Id, _a, "chat.messages")).Should().Be(0);
        (await SeenByPolicyAsync(null, _a, "chat.messages")).Should().Be(0);
        (await SeenByPolicyAsync(null, _a, "chat.spaces")).Should().Be(0);
    }

    [Fact]
    public async Task ThePolicyAgreesWithTheOriginalFunctionRowForRow()
    {
        var callers = new (Guid? User, AppRoleTenant Tenant)[]
        {
            (_a.User.Id, _a), (_b.User.Id, _b), (_a.User.Id, _b), (_b.User.Id, _a), (_activeMember.Id, _a),
            (_suspended.Id, _a), (_removed.Id, _a), (null, _a), (Guid.NewGuid(), _a),
        };

        foreach (var (user, tenant) in callers)
        {
            foreach (var table in Tables)
            {
                var seen = await SeenByPolicyAsync(user, tenant, table);
                var accepted = await AcceptedByFunctionAsync(user, tenant, table);
                seen.Should().Be(accepted, $"{table} as {user?.ToString() ?? "no user"} in {tenant.WorkspaceId}: the fast path may only short-circuit what the function accepts");
            }
        }
    }
}
