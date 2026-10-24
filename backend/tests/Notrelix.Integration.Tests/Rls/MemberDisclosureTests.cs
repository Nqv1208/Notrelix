using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notrelix.API;
using Notrelix.Domain.Identity.Profiles;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Identity.Profiles;
using Notrelix.Infrastructure.Identity.Users;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Npgsql;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// Other people's email and phone under the application role (the users and profile rows of other people are
/// not readable by it). Both reads are owner-defined functions that mirror the existing Application rule
/// exactly. Email: only an active Owner or Admin of the Workspace reads email, only of active non-guest
/// members of that Workspace. Phone: only an active non-guest member reads it, only of active non-guest
/// members, only for people who opted in.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class MemberDisclosureTests : IAsyncLifetime
{
    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private User _admin = null!;
    private User _member = null!;
    private User _guest = null!;
    private User _suspended = null!;
    private User _optedOut = null!;

    public MemberDisclosureTests(AppRoleTestContainer fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "a-disc@example.com", "Aaa", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "b-disc@example.com", "Bbb", "hash");
        _admin = await AppRoleMembers.AddAsync(_fixture, _a, "Adm", WorkspaceRole.Admin, WorkspaceMemberStatus.Active, "hash");
        _member = await AppRoleMembers.AddAsync(_fixture, _a, "Mem", WorkspaceRole.Member, WorkspaceMemberStatus.Active, "hash");
        _guest = await AppRoleMembers.AddAsync(_fixture, _a, "Gst", WorkspaceRole.Guest, WorkspaceMemberStatus.Active, "hash");
        _suspended = await AppRoleMembers.AddAsync(_fixture, _a, "Sus", WorkspaceRole.Member, WorkspaceMemberStatus.Suspended, "hash");
        _optedOut = await AppRoleMembers.AddAsync(_fixture, _a, "Opt", WorkspaceRole.Member, WorkspaceMemberStatus.Active, "hash");

        var now = DateTimeOffset.UtcNow;
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using (var ctx = _fixture.CreateOwnerContext(system))
        {
            foreach (var user in new[] { _a.User, _admin, _member, _guest, _suspended, _optedOut, _b.User })
                ctx.UserProfiles.Add(UserProfile.Create(user.Id, now));
            await ctx.SaveChangesAsync();
        }

        foreach (var (user, visible) in new[] { (_a.User, true), (_admin, true), (_member, true), (_guest, true), (_suspended, true), (_optedOut, false), (_b.User, true) })
        {
            await OwnerScalarAsync(
                "UPDATE identity.user_profiles SET phone = @p, phone_visible_to_workspace_members = @v WHERE user_id = @u",
                ("p", $"+8490{user.Id.ToString("N")[^8..]}"), ("v", visible), ("u", user.Id));
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<string> OwnerScalarAsync(string sql, params (string, object)[] p)
    {
        await using var c = new NpgsqlConnection(_fixture.OwnerConnectionString);
        await c.OpenAsync();
        await using var command = new NpgsqlCommand(sql, c);
        foreach (var (name, value) in p) command.Parameters.AddWithValue(name, value);
        return Convert.ToString(await command.ExecuteScalarAsync()) ?? "";
    }

    private Guid[] Everyone => [_a.User.Id, _admin.Id, _member.Id, _guest.Id, _suspended.Id, _optedOut.Id, _b.User.Id];

    /// <summary>Runs a function query as <paramref name="reader"/> in the tenant scope of tenant A's workspace.</summary>
    private async Task<IReadOnlyCollection<Guid>> AsAsync(Guid? reader, string function, Guid workspace, string scope = "app")
    {
        await using var c = new NpgsqlConnection(_fixture.AppConnectionString);
        await c.OpenAsync();
        await using var tx = await c.BeginTransactionAsync();
        await using (var set = new NpgsqlCommand(
            "SELECT set_config('app.current_user_id', @u, true), set_config('app.current_account_id', @a, true), " +
            "set_config('app.current_workspace_id', @w, true), set_config('app.request_scope', @s, true)", c, tx))
        {
            set.Parameters.AddWithValue("u", reader?.ToString() ?? "");
            set.Parameters.AddWithValue("a", _a.AccountId.ToString());
            set.Parameters.AddWithValue("w", _a.WorkspaceId.ToString());
            set.Parameters.AddWithValue("s", scope);
            await set.ExecuteNonQueryAsync();
        }

        var ids = new List<Guid>();
        await using var query = new NpgsqlCommand($"SELECT user_id FROM {function}(@workspace, @ids)", c, tx);
        query.Parameters.AddWithValue("workspace", workspace);
        query.Parameters.AddWithValue("ids", Everyone);
        await using var reader2 = await query.ExecuteReaderAsync();
        while (await reader2.ReadAsync())
            ids.Add(reader2.GetGuid(0));
        return ids;
    }

    // ------------------------------------------------------------------------------------------------
    // Email
    // ------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Email_IsVisibleToAnOwnerAndAnAdmin_OfActiveNonGuestMembersOfTheSameWorkspaceOnly()
    {
        var expected = new[] { _a.User.Id, _admin.Id, _member.Id, _optedOut.Id };

        (await AsAsync(_a.User.Id, "ops.disclosable_member_emails", _a.WorkspaceId)).Should().BeEquivalentTo(expected,
            "an owner sees active non-guest members: not the guest, not the suspended member, not another tenant's user");
        (await AsAsync(_admin.Id, "ops.disclosable_member_emails", _a.WorkspaceId)).Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("member")]
    [InlineData("guest")]
    [InlineData("suspended")]
    [InlineData("stranger")]
    public async Task Email_IsNeverVisibleToAnyOtherReader(string who)
    {
        var reader = who switch
        {
            "member" => _member.Id,
            "guest" => _guest.Id,
            "suspended" => _suspended.Id,
            _ => _b.User.Id,
        };

        (await AsAsync(reader, "ops.disclosable_member_emails", _a.WorkspaceId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Email_IsNotVisibleOutsideTheWorkspaceItIsAskedFor_OrWithoutAUserOrInAnotherScope()
    {
        (await AsAsync(_a.User.Id, "ops.disclosable_member_emails", _b.WorkspaceId))
            .Should().BeEmpty("the owner of A is no administrator of B");
        (await AsAsync(null, "ops.disclosable_member_emails", _a.WorkspaceId)).Should().BeEmpty();
        (await AsAsync(_a.User.Id, "ops.disclosable_member_emails", _a.WorkspaceId, "token")).Should().BeEmpty();
        (await AsAsync(_a.User.Id, "ops.disclosable_member_emails", _a.WorkspaceId, "worker")).Should().BeEmpty(
            "a forged worker scope reads nothing");
    }

    [Fact]
    public async Task TheEmailAdapter_UnderTheAppRole_FollowsTheSameRule()
    {
        var rows = await ReadEmailsAsync(_a.User, _a.WorkspaceId);
        rows.Keys.Should().BeEquivalentTo(new[] { _a.User.Id, _admin.Id, _member.Id, _optedOut.Id });
        rows[_member.Id].Should().Be(_member.NormalizedEmail);

        (await ReadEmailsAsync(_member, _a.WorkspaceId)).Should().BeEmpty("a plain member reads nobody's email");
        (await ReadEmailsAsync(_b.User, _a.WorkspaceId)).Should().BeEmpty("another tenant's owner reads nobody's email");
    }

    private async Task<IReadOnlyDictionary<Guid, string>> ReadEmailsAsync(User reader, Guid workspaceId)
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(_a.AccountId, _a.WorkspaceId, reader.Id);
        await using var ctx = _fixture.CreateAppContext(tenant);
        await using var tx = await ctx.Database.BeginTransactionAsync();
        var rls = new RlsSessionContext(ctx, Options.Create(new RlsOptions { Enabled = true, SetSessionContext = true }), tenant, NullLogger<RlsSessionContext>.Instance);
        await rls.ApplyAsync(CancellationToken.None);
        return await new MemberEmailReader(ctx, rls).FindAsync(workspaceId, Everyone, CancellationToken.None);
    }

    // ------------------------------------------------------------------------------------------------
    // Phone
    // ------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("owner")]
    [InlineData("admin")]
    [InlineData("member")]
    public async Task Phone_IsVisibleToEveryActiveNonGuestMember_ForActiveNonGuestMembersWhoOptedIn(string who)
    {
        var reader = who switch { "owner" => _a.User.Id, "admin" => _admin.Id, _ => _member.Id };

        (await AsAsync(reader, "ops.shared_member_phones", _a.WorkspaceId)).Should().BeEquivalentTo(
            new[] { _a.User.Id, _admin.Id, _member.Id },
            "not the guest, not the suspended member, not the one who kept the default, not another tenant's user");
    }

    [Theory]
    [InlineData("guest")]
    [InlineData("suspended")]
    [InlineData("stranger")]
    public async Task Phone_IsNeverVisibleToAGuestASuspendedMemberOrAStranger(string who)
    {
        var reader = who switch { "guest" => _guest.Id, "suspended" => _suspended.Id, _ => _b.User.Id };
        (await AsAsync(reader, "ops.shared_member_phones", _a.WorkspaceId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Phone_IsNotVisibleOutsideTheWorkspaceItIsAskedFor_OrInAnotherScope()
    {
        (await AsAsync(_a.User.Id, "ops.shared_member_phones", _b.WorkspaceId)).Should().BeEmpty();
        (await AsAsync(_a.User.Id, "ops.shared_member_phones", _a.WorkspaceId, "token")).Should().BeEmpty();
        (await AsAsync(_a.User.Id, "ops.shared_member_phones", _a.WorkspaceId, "worker")).Should().BeEmpty();
    }

    [Fact]
    public async Task ThePhoneAdapter_UnderTheAppRole_FollowsTheSameRule()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(_a.AccountId, _a.WorkspaceId, _member.Id);
        await using var ctx = _fixture.CreateAppContext(tenant);
        await using var tx = await ctx.Database.BeginTransactionAsync();
        var rls = new RlsSessionContext(ctx, Options.Create(new RlsOptions { Enabled = true, SetSessionContext = true }), tenant, NullLogger<RlsSessionContext>.Instance);
        await rls.ApplyAsync(CancellationToken.None);

        var phones = await new MemberPhoneLookup(ctx, ctx, rls).FindSharedPhonesAsync(_a.WorkspaceId, Everyone, CancellationToken.None);
        phones.Keys.Should().BeEquivalentTo(new[] { _a.User.Id, _admin.Id, _member.Id });
    }

    [Fact]
    public async Task TheMemberList_OverHttp_ShowsOptedInPhonesToAMember_AndTheGuestCannotList()
    {
        using var factory = AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);

        var asMember = await ReadMembersAsync(factory, _member);
        asMember.Should().Contain($"+8490{_admin.Id.ToString("N")[^8..]}", "an opted-in colleague's phone is shown to a member");
        asMember.Should().NotContain($"+8490{_optedOut.Id.ToString("N")[^8..]}", "a colleague who kept the default is never shown");

        // A guest cannot list members at all (role baseline); the function above returns nothing to a guest.
        string jwt;
        using (var scope = factory.Services.CreateScope())
            jwt = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(_guest);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", jwt);
        (await client.GetAsync($"/api/v1/workspaces/{_a.WorkspaceId}/members")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<string> ReadMembersAsync(WebApplicationFactory<Program> factory, User reader)
    {
        string jwt;
        using (var scope = factory.Services.CreateScope())
            jwt = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(reader);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", jwt);
        var response = await client.GetAsync($"/api/v1/workspaces/{_a.WorkspaceId}/members");
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadAsStringAsync();
    }
}
