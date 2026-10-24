using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Npgsql;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// Proves the co-member profile resolver under the application role: display name and avatar resolve for
/// people who share a workspace membership with the caller in any state (guests included), never for
/// anyone else, and no other identity column is reachable through the function or through the table.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class CoMemberProfileTests : IAsyncLifetime
{
    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private Domain.Identity.Users.User _activeMember = null!;
    private Domain.Identity.Users.User _suspendedMember = null!;
    private Domain.Identity.Users.User _removedMember = null!;
    private Domain.Identity.Users.User _guest = null!;

    public CoMemberProfileTests(AppRoleTestContainer fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "a-comember@example.com", "Aaa", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "b-comember@example.com", "Bbb", "hash");
        _activeMember = await AppRoleMembers.AddAsync(_fixture, _a, "Active", WorkspaceRole.Member, WorkspaceMemberStatus.Active, "hash");
        _suspendedMember = await AppRoleMembers.AddAsync(_fixture, _a, "Suspended", WorkspaceRole.Member, WorkspaceMemberStatus.Suspended, "hash");
        _removedMember = await AppRoleMembers.AddAsync(_fixture, _a, "Removed", WorkspaceRole.Member, WorkspaceMemberStatus.Removed, "hash");
        _guest = await AppRoleMembers.AddAsync(_fixture, _a, "Guesty", WorkspaceRole.Guest, WorkspaceMemberStatus.Active, "hash");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private sealed record Profile(Guid UserId, string Name, string? Avatar);

    /// <summary>Resolves the given ids as <paramref name="caller"/> in the given scope kind, in one transaction.</summary>
    private async Task<List<Profile>> ResolveAsync(Guid? caller, string scope, params Guid[] ids)
    {
        await using var c = new NpgsqlConnection(_fixture.AppConnectionString);
        await c.OpenAsync();
        await using var tx = await c.BeginTransactionAsync();
        await using (var set = new NpgsqlCommand(
            "SELECT set_config('app.current_user_id', @u, true), set_config('app.request_scope', @s, true)", c, tx))
        {
            set.Parameters.AddWithValue("u", caller?.ToString() ?? "");
            set.Parameters.AddWithValue("s", scope);
            await set.ExecuteNonQueryAsync();
        }

        await using var read = new NpgsqlCommand(
            "SELECT user_id, name, avatar FROM ops.co_member_profiles(@ids)", c, tx);
        read.Parameters.AddWithValue("ids", ids);
        var rows = new List<Profile>();
        await using var reader = await read.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            rows.Add(new Profile(reader.GetGuid(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2)));
        return rows;
    }

    [Theory]
    [InlineData("app")]
    [InlineData("user")]
    public async Task ACoMember_ResolvesNameAndAvatar_ForActiveSuspendedRemovedAndGuestMembers(string scope)
    {
        var rows = await ResolveAsync(_a.User.Id, scope,
            _activeMember.Id, _suspendedMember.Id, _removedMember.Id, _guest.Id, _a.User.Id);

        rows.Select(r => r.UserId).Should().BeEquivalentTo(
            [_activeMember.Id, _suspendedMember.Id, _removedMember.Id, _guest.Id, _a.User.Id],
            "memberships in any state resolve, so historical attribution keeps working, and the caller resolves themselves");
        rows.Single(r => r.UserId == _removedMember.Id).Name.Should().Be("Removed");
        rows.Single(r => r.UserId == _guest.Id).Avatar.Should().EndWith("/avatar?v=1");
    }

    [Fact]
    public async Task AGuest_ResolvesTheMembersOfTheirWorkspace_AndAMemberResolvesTheGuest()
    {
        (await ResolveAsync(_guest.Id, "app", _a.User.Id, _activeMember.Id)).Select(r => r.Name)
            .Should().BeEquivalentTo(["Aaa", "Active"]);
        (await ResolveAsync(_activeMember.Id, "app", _guest.Id)).Should().ContainSingle().Which.Name.Should().Be("Guesty");
    }

    [Fact]
    public async Task APersonWhoSharesNoWorkspace_IsNotResolvable_AndMixedRequestsReturnOnlyTheCoMembers()
    {
        (await ResolveAsync(_b.User.Id, "app", _a.User.Id, _activeMember.Id, _guest.Id)).Should().BeEmpty();
        (await ResolveAsync(_a.User.Id, "app", _b.User.Id)).Should().BeEmpty("a non-co-member's name and avatar do not resolve");

        var mixed = await ResolveAsync(_a.User.Id, "app", _b.User.Id, _activeMember.Id);
        mixed.Should().ContainSingle().Which.UserId.Should().Be(_activeMember.Id);
    }

    [Fact]
    public async Task NoCallerIdentity_AndNoUnrelatedScope_ResolveAnything_ExceptTheCallerThemselves()
    {
        (await ResolveAsync(null, "app", _a.User.Id, _activeMember.Id)).Should().BeEmpty("no session context means no caller");
        (await ResolveAsync(_a.User.Id, "token", _activeMember.Id)).Should().BeEmpty("a token scope has no user relationship");
        (await ResolveAsync(_a.User.Id, "credential", _activeMember.Id)).Should().BeEmpty();
        (await ResolveAsync(_a.User.Id, "registration", _activeMember.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task TheFunctionReturnsOnlyIdNameAndAvatar_AndTheUsersRowStaysHidden()
    {
        await using var c = new NpgsqlConnection(_fixture.AppConnectionString);
        await c.OpenAsync();
        await using var tx = await c.BeginTransactionAsync();
        await using (var set = new NpgsqlCommand(
            "SELECT set_config('app.current_user_id', @u, true), set_config('app.request_scope', 'app', true)", c, tx))
        {
            set.Parameters.AddWithValue("u", _a.User.Id.ToString());
            await set.ExecuteNonQueryAsync();
        }

        await using (var shape = new NpgsqlCommand("SELECT * FROM ops.co_member_profiles(@ids)", c, tx))
        {
            shape.Parameters.AddWithValue("ids", new[] { _activeMember.Id });
            await using var reader = await shape.ExecuteReaderAsync();
            Enumerable.Range(0, reader.FieldCount).Select(reader.GetName)
                .Should().Equal("user_id", "name", "avatar");
        }

        await using var direct = new NpgsqlCommand(
            "SELECT count(*) FROM identity.users WHERE id = @id", c, tx);
        direct.Parameters.AddWithValue("id", _activeMember.Id);
        Convert.ToInt64(await direct.ExecuteScalarAsync()).Should().Be(0,
            "the function does not widen the row policy: email and credentials stay unreachable");
    }

    [Theory]
    [InlineData("ops.co_member_profiles(uuid[])")]
    [InlineData("ops.co_member_avatar_storage_key(uuid)")]
    [InlineData("ops.shares_workspace_with_caller(uuid)")]
    public async Task TheResolverFunctions_AreExecutableByTheApplicationAndWorkerRolesOnly(string signature)
    {
        await using var c = new NpgsqlConnection(_fixture.OwnerConnectionString);
        await c.OpenAsync();
        async Task<bool> CanAsync(string role)
        {
            await using var q = new NpgsqlCommand($"SELECT has_function_privilege('{role}', '{signature}', 'EXECUTE')", c);
            return (bool)(await q.ExecuteScalarAsync())!;
        }

        (await CanAsync("notrelix_app")).Should().BeTrue();
        (await CanAsync("notrelix_worker")).Should().BeTrue("background consumers resolve display names through the same functions");
        foreach (var other in new[] { "public", "notrelix_auth", "notrelix_support_readonly" })
            (await CanAsync(other)).Should().BeFalse($"{other} must not execute {signature}");
    }

    [Fact]
    public async Task TheWorkspaceMemberList_UnderTheAppRole_ShowsCoMemberNames()
    {
        using var factory = AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);
        string jwt;
        using (var scope = factory.Services.CreateScope())
            jwt = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(_activeMember);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", jwt);

        var members = await client.GetAsync($"/api/v1/workspaces/{_a.WorkspaceId}/members");
        members.StatusCode.Should().Be(HttpStatusCode.OK, await members.Content.ReadAsStringAsync());
        var body = await members.Content.ReadAsStringAsync();
        body.Should().Contain("Aaa", "the workspace owner's display name resolves for a co-member")
            .And.Contain("Guesty");

        var other = await client.GetAsync($"/api/v1/workspaces/{_b.WorkspaceId}/members");
        other.StatusCode.Should().NotBe(HttpStatusCode.OK, "another tenant's member list stays closed");
        (await other.Content.ReadAsStringAsync()).Should().NotContain("Bbb");
    }
}
