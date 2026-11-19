using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Rls;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// The Account's workspace list over the real host as the application role (RLS on): being an Account member does
/// not show a workspace the person does not belong to, so only the workspaces the caller is an active member of are
/// listed and a workspace the caller is not in never appears, not even by name. An archived workspace is listed to
/// its Owners and Admins only, a deleted one to nobody, and a person outside the Account gets the answer a missing
/// Account gets.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class AccountWorkspaceListMembershipFilterIntegrationTests : IAsyncLifetime
{
    private static readonly string[] VolatileKeys = ["correlationId", "traceId", "instance"];
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 8, 0, 0, TimeSpan.Zero);

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;

    public AccountWorkspaceListMembershipFilterIntegrationTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "a-list@example.com", "Aaa", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "b-list@example.com", "Bbb", "hash");
        await WriteAsync(ctx =>
        {
            ctx.AccountMembers.Add(AccountMember.Create(_a.AccountId, _a.User.Id, AccountRole.Owner, _a.User.Id, Now));
            ctx.AccessGrants.Add(AccountGrant(_a.User.Id, admin: true));
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private AccessGrant AccountGrant(Guid userId, bool admin) => new(
        accountId: _a.AccountId, workspaceId: null, userId: userId, sourceContext: "Account",
        membershipStatus: "Active", roleCodes: [admin ? "Owner" : "Member"], permissionCodes: [], isAccountAdmin: admin,
        isWorkspaceAdmin: false, grantedAt: Now);

    private async Task WriteAsync(Action<Notrelix.Infrastructure.Data.ApplicationDbContext> seed)
    {
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        seed(ctx);
        await ctx.SaveChangesAsync();
    }

    /// <summary>A person of Account A who is in no workspace yet.</summary>
    private async Task<User> AddAccountMemberAsync(string name)
    {
        var user = User.Create($"{name.ToLowerInvariant()}-{Guid.NewGuid():N}@example.com", name, "hash", Now, hasPasswordCredential: true);
        user.ConfirmEmail(user.Id, Now);
        await WriteAsync(ctx =>
        {
            ctx.Users.Add(user);
            ctx.AccountMembers.Add(AccountMember.Create(_a.AccountId, user.Id, AccountRole.Member, _a.User.Id, Now));
            ctx.AccessGrants.Add(AccountGrant(user.Id, admin: false));
        });
        return user;
    }

    /// <summary>A workspace of Account A owned by the Account owner; archived or deleted when asked.</summary>
    private async Task<Guid> AddWorkspaceAsync(string name, bool archived = false, bool deleted = false)
    {
        var workspace = Workspace.Create(_a.AccountId, _a.User.Id, name, $"ws-{Guid.NewGuid():N}"[..16], Now);
        if (archived) workspace.Archive(_a.User.Id, Now);
        if (deleted) workspace.Delete(_a.User.Id, Now);
        await WriteAsync(ctx =>
        {
            ctx.Workspaces.Add(workspace);
            ctx.WorkspaceMembers.Add(WorkspaceMember.Create(_a.AccountId, workspace.Id, _a.User.Id, WorkspaceRole.Owner, _a.User.Id, Now));
            ctx.AccessGrants.Add(WorkspaceGrant(workspace.Id, _a.User.Id, WorkspaceRole.Owner));
        });
        return workspace.Id;
    }

    private AccessGrant WorkspaceGrant(Guid workspaceId, Guid userId, WorkspaceRole role) => new(
        accountId: _a.AccountId, workspaceId: workspaceId, userId: userId, sourceContext: "Workspace",
        membershipStatus: "Active", roleCodes: [role.ToString()], permissionCodes: [], isAccountAdmin: false,
        isWorkspaceAdmin: role is WorkspaceRole.Owner or WorkspaceRole.Admin, grantedAt: Now);

    private Task JoinAsync(Guid workspaceId, User user, WorkspaceRole role) =>
        WriteAsync(ctx =>
        {
            ctx.WorkspaceMembers.Add(WorkspaceMember.Create(_a.AccountId, workspaceId, user.Id, role, _a.User.Id, Now));
            ctx.AccessGrants.Add(WorkspaceGrant(workspaceId, user.Id, role));
        });

    private static HttpClient ClientFor(WebApplicationFactory<Program> factory, User user)
    {
        using var scope = factory.Services.CreateScope();
        var token = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(user, null);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<HttpResponseMessage> ListAsync(User caller, Guid accountId)
    {
        using var factory = AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);
        using var client = ClientFor(factory, caller);
        var response = await client.GetAsync($"/api/v1/accounts/{accountId}/workspaces");
        await response.Content.LoadIntoBufferAsync();
        return response;
    }

    private async Task<string[]> NamesAsync(User caller)
    {
        var response = await ListAsync(caller, _a.AccountId);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        return JsonDocument.Parse(body).RootElement.EnumerateArray().Select(r => r.GetProperty("name").GetString()!).ToArray();
    }

    [Fact]
    public async Task AnAccountMemberInTwoOfThreeWorkspaces_ListsTwoRows_AndTheThirdNeverAppearsNotEvenItsName()
    {
        var first = _a.WorkspaceId;
        var second = await AddWorkspaceAsync("Beta Space");
        var third = await AddWorkspaceAsync("Gamma Secret Project");
        var alice = await AddAccountMemberAsync("Alice");
        await JoinAsync(first, alice, WorkspaceRole.Member);
        await JoinAsync(second, alice, WorkspaceRole.Guest);

        var response = await ListAsync(alice, _a.AccountId);
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        var rows = JsonDocument.Parse(body).RootElement.EnumerateArray().ToList();
        rows.Select(r => r.GetProperty("id").GetGuid()).Should().BeEquivalentTo([first, second]);
        rows.Should().HaveCount(2);
        rows.Single(r => r.GetProperty("id").GetGuid() == first).GetProperty("memberCount").GetInt32().Should().Be(2);
        body.Should().NotContain("Gamma", "a workspace the caller is not in never shows, not even its name");
        body.Should().NotContain(third.ToString());

        var owner = await NamesAsync(_a.User);
        owner.Should().HaveCount(3, "the Owner of all three sees all three");
    }

    [Fact]
    public async Task AnAccountMemberOfNoWorkspace_ListsNothing_AndASuspendedOrRemovedMemberDoesNotSeeThatWorkspace()
    {
        var second = await AddWorkspaceAsync("Beta Space");
        var nobody = await AddAccountMemberAsync("Nobody");
        var suspended = await AddAccountMemberAsync("Suspended");
        var removed = await AddAccountMemberAsync("Removed");
        await JoinAsync(second, suspended, WorkspaceRole.Member);
        await JoinAsync(second, removed, WorkspaceRole.Member);
        await WriteAsync(ctx =>
        {
            var rows = ctx.WorkspaceMembers.Where(m => m.WorkspaceId == second).ToList();
            rows.Single(m => m.UserId == suspended.Id).Suspend(_a.User.Id, Now, 1);
            rows.Single(m => m.UserId == removed.Id).Remove(1, _a.User.Id, Now);
        });

        (await NamesAsync(nobody)).Should().BeEmpty();
        (await NamesAsync(suspended)).Should().BeEmpty();
        (await NamesAsync(removed)).Should().BeEmpty();
    }

    [Fact]
    public async Task AnArchivedWorkspace_IsListedToItsOwnerAndAdminOnly_AndADeletedOneToNobody()
    {
        var archived = await AddWorkspaceAsync("Delta Archived", archived: true);
        await AddWorkspaceAsync("Epsilon Deleted", deleted: true);
        var admin = await AddAccountMemberAsync("Admin");
        var member = await AddAccountMemberAsync("Member");
        var guest = await AddAccountMemberAsync("Guest");
        await JoinAsync(archived, admin, WorkspaceRole.Admin);
        await JoinAsync(archived, member, WorkspaceRole.Member);
        await JoinAsync(archived, guest, WorkspaceRole.Guest);

        (await NamesAsync(_a.User)).Should().BeEquivalentTo(["Aaa WS", "Delta Archived"], "the Owner sees the archived one, never the deleted one");
        (await NamesAsync(admin)).Should().BeEquivalentTo(["Delta Archived"]);
        (await NamesAsync(member)).Should().BeEmpty("a Member reaches an archived workspace by its link, not through the list");
        (await NamesAsync(guest)).Should().BeEmpty();
    }

    [Fact]
    public async Task APersonOutsideTheAccount_GetsTheAnswerAMissingAccountGets_AndNoWorkspaceName()
    {
        await AddWorkspaceAsync("Beta Space");

        var otherTenant = await ListAsync(_b.User, _a.AccountId);
        var missing = await ListAsync(_b.User, Guid.NewGuid());
        var otherBody = await otherTenant.Content.ReadAsStringAsync();

        ((int)otherTenant.StatusCode).Should().BeInRange(400, 499, otherBody);
        otherTenant.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
        otherTenant.StatusCode.Should().Be(missing.StatusCode, "another tenant's Account id answers like a missing one");
        (await CanonicalAsync(otherTenant)).Should().BeEquivalentTo(await CanonicalAsync(missing));
        otherBody.Should().NotContain("Beta").And.NotContain("Aaa WS");
    }

    private static async Task<Dictionary<string, string>> CanonicalAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(text)) return [];
        using var document = JsonDocument.Parse(text);
        return document.RootElement.EnumerateObject()
            .Where(property => !VolatileKeys.Contains(property.Name))
            .ToDictionary(property => property.Name, property => property.Value.GetRawText());
    }
}
