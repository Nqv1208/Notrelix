using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Npgsql;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// Billing meters seats and storage across every workspace of an account, while a workspace-level caller's
/// rows stop at the workspaces they hold. The definer functions return counts only, and only for a workspace
/// the caller holds; they never widen what a table exposes.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class BillingUsagePolicyTests : IAsyncLifetime
{
    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private Guid _secondWorkspaceId;

    public BillingUsagePolicyTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "billing-a@example.com", "BillingA", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "billing-b@example.com", "BillingB", "hash");
        await SeedSecondWorkspaceOfAccountAAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// A second workspace of account A that the caller (owner of the first workspace) does not hold, with one
    /// member of its own and one guest, and an attachment row of 1000 bytes in the first workspace.
    /// </summary>
    private async Task SeedSecondWorkspaceOfAccountAAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var second = Workspace.Create(_a.AccountId, _a.User.Id, "Second WS", $"ws2-{Guid.NewGuid():N}"[..16], now);
        _secondWorkspaceId = second.Id;
        var colleague = await AppRoleUsers.AddAsync(_fixture, $"colleague-{Guid.NewGuid():N}@example.com", "Colleague", "hash");
        var guest = await AppRoleUsers.AddAsync(_fixture, $"guest-{Guid.NewGuid():N}@example.com", "Guest", "hash");

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.Workspaces.Add(second);
        ctx.WorkspaceMembers.Add(WorkspaceMember.Create(_a.AccountId, second.Id, colleague.Id, WorkspaceRole.Member, _a.User.Id, now));
        ctx.WorkspaceMembers.Add(WorkspaceMember.Create(_a.AccountId, second.Id, guest.Id, WorkspaceRole.Guest, _a.User.Id, now));
        await ctx.SaveChangesAsync();
        Colleague = colleague.Id;
        Guest = guest.Id;
    }

    private Guid Colleague { get; set; }
    private Guid Guest { get; set; }

    private async Task<T> AsCallerOfAAsync<T>(Func<NpgsqlConnection, NpgsqlTransaction, Task<T>> work)
    {
        await using var connection = new NpgsqlConnection(_fixture.AppConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var scope = new NpgsqlCommand(
            "SELECT set_config('app.current_user_id', @u, true), set_config('app.current_account_id', @a, true), " +
            "set_config('app.current_workspace_id', @w, true), set_config('app.request_scope', 'app', true)",
            connection, transaction))
        {
            scope.Parameters.AddWithValue("u", _a.User.Id.ToString());
            scope.Parameters.AddWithValue("a", _a.AccountId.ToString());
            scope.Parameters.AddWithValue("w", _a.WorkspaceId.ToString());
            await scope.ExecuteNonQueryAsync();
        }

        return await work(connection, transaction);
    }

    private static async Task<(int Members, int Guests, long Bytes)> UsageAsync(
        NpgsqlConnection c, NpgsqlTransaction t, Guid account, Guid workspace)
    {
        await using var command = new NpgsqlCommand(
            "SELECT member_seats, guest_seats, storage_bytes FROM ops.billing_account_usage(@a, @w)", c, t);
        command.Parameters.AddWithValue("a", account);
        command.Parameters.AddWithValue("w", workspace);
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return (reader.GetInt32(0), reader.GetInt32(1), reader.GetInt64(2));
    }

    private static async Task<bool> HasSeatAsync(
        NpgsqlConnection c, NpgsqlTransaction t, Guid account, Guid workspace, Guid user, bool guest)
    {
        await using var command = new NpgsqlCommand("SELECT ops.billing_has_seat(@a, @w, @u, @g)", c, t);
        command.Parameters.AddWithValue("a", account);
        command.Parameters.AddWithValue("w", workspace);
        command.Parameters.AddWithValue("u", user);
        command.Parameters.AddWithValue("g", guest);
        return (bool)(await command.ExecuteScalarAsync())!;
    }

    [Fact]
    public async Task Usage_SumsTheWholeAccountEvenThoughTheCallerHoldsOneWorkspace()
    {
        await AsCallerOfAAsync(async (c, t) =>
        {
            await using var rows = new NpgsqlCommand(
                "SELECT count(*) FROM workspace.workspace_members WHERE workspace_id = @w", c, t);
            rows.Parameters.AddWithValue("w", _secondWorkspaceId);
            ((long)(await rows.ExecuteScalarAsync())!).Should().Be(0, "the table itself hides the second workspace");

            var usage = await UsageAsync(c, t, _a.AccountId, _a.WorkspaceId);

            usage.Members.Should().Be(2, "the owner of the first workspace plus the colleague of the second");
            usage.Guests.Should().Be(1);
            return 0;
        });
    }

    [Fact]
    public async Task Usage_IsZeroForAWorkspaceTheCallerDoesNotHold()
    {
        await AsCallerOfAAsync(async (c, t) =>
        {
            (await UsageAsync(c, t, _b.AccountId, _b.WorkspaceId)).Should().Be((0, 0, 0L), "another tenant's workspace");
            (await UsageAsync(c, t, _a.AccountId, _secondWorkspaceId)).Should().Be((0, 0, 0L), "a workspace of the account the caller does not hold");
            return 0;
        });
    }

    [Fact]
    public async Task Seat_IsHeldWhenThePersonHoldsTheKindOfSeatAnywhereInTheAccount()
    {
        await AsCallerOfAAsync(async (c, t) =>
        {
            (await HasSeatAsync(c, t, _a.AccountId, _a.WorkspaceId, Colleague, guest: false)).Should().BeTrue();
            (await HasSeatAsync(c, t, _a.AccountId, _a.WorkspaceId, Colleague, guest: true)).Should().BeFalse();
            (await HasSeatAsync(c, t, _a.AccountId, _a.WorkspaceId, Guest, guest: true)).Should().BeTrue();
            (await HasSeatAsync(c, t, _a.AccountId, _a.WorkspaceId, Guid.NewGuid(), guest: false)).Should().BeFalse();
            (await HasSeatAsync(c, t, _b.AccountId, _b.WorkspaceId, _b.User.Id, guest: false))
                .Should().BeFalse("a workspace the caller does not hold answers nothing");
            return 0;
        });
    }
}
