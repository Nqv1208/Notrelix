using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API;
using Notrelix.API.Realtime;
using Notrelix.Application.Common.Realtime.Gateway;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Forms;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// A person without access to a Workspace gets the identical answer for a resource that exists there and for an
/// id that never existed, so the answer cannot be used to find out what exists (NRX-004). Proven over the real
/// production graph on the application role for a person of the same account who is not a member of the
/// Workspace, a person of another account, and a member with no grant on a Private Board. A Guest, who can see
/// the Workspace but lacks the permission, keeps Forbidden.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class NonMemberDisclosureTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private User _sameAccountOutsider = null!;
    private User _member = null!;
    private User _guest = null!;
    private User _suspended = null!;
    private User _accountAdmin = null!;
    private Guid _itemId;
    private Guid _formId;
    private Guid _commentId;
    private Guid _privateItemId;
    private Guid _privateFormId;

    public NonMemberDisclosureTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "a-nonmember@example.com", "Aaa", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "b-nonmember@example.com", "Bbb", "hash");
        _sameAccountOutsider = await AppRoleUsers.AddAsync(_fixture, "outsider-nonmember@example.com", "Out", "hash");
        _member = await AppRoleMembers.AddAsync(_fixture, _a, "Mem", WorkspaceRole.Member, WorkspaceMemberStatus.Active, "hash");
        _guest = await AppRoleMembers.AddAsync(_fixture, _a, "Gst", WorkspaceRole.Guest, WorkspaceMemberStatus.Active, "hash");
        _suspended = await AppRoleMembers.AddAsync(_fixture, _a, "Sus", WorkspaceRole.Member, WorkspaceMemberStatus.Suspended, "hash");

        _accountAdmin = await AppRoleUsers.AddAsync(_fixture, "admin-nonmember@example.com", "Adm", "hash");

        var owner = _a.User.Id;
        var privateBoard = Board.Create(_a.AccountId, _a.WorkspaceId, owner, "Private", null, Now, BoardVisibility.Private);
        var group = BoardGroup.Create(_a.AccountId, _a.WorkspaceId, _a.BoardId, "Todo", Color.Create("#808080"), FractionalIndex.Create("a0"), owner, Now);
        var privateGroup = BoardGroup.Create(_a.AccountId, _a.WorkspaceId, privateBoard.Id, "Todo", Color.Create("#808080"), FractionalIndex.Create("a0"), owner, Now);
        var item = BoardItem.CreateRoot(_a.AccountId, _a.WorkspaceId, _a.BoardId, group.Id, "Task", FractionalIndex.Create("a0"), owner, Now);
        var privateItem = BoardItem.CreateRoot(_a.AccountId, _a.WorkspaceId, privateBoard.Id, privateGroup.Id, "Secret", FractionalIndex.Create("a0"), owner, Now);
        var form = Form.Create(_a.AccountId, _a.WorkspaceId, _a.BoardId, "Intake", $"intake-{Guid.NewGuid():N}"[..16], owner, Now);
        var privateForm = Form.Create(_a.AccountId, _a.WorkspaceId, privateBoard.Id, "Secret intake", $"secret-{Guid.NewGuid():N}"[..16], owner, Now);
        var comment = Comment.Create(
            _a.AccountId, _a.WorkspaceId,
            ResourceRef.Create(ResourceKind.Create("work-management.board-item"), item.Id, _a.WorkspaceId),
            "A comment", owner, Now);

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.AccountMembers.Add(AccountMember.Create(_a.AccountId, _sameAccountOutsider.Id, AccountRole.Member, owner, Now));
        // An Account admin reads every Workspace row of the Account (RLS), so the policy engine, not the row
        // filter, is what must hide an existing Workspace from them while they hold no Workspace membership.
        ctx.AccountMembers.Add(AccountMember.Create(_a.AccountId, _accountAdmin.Id, AccountRole.Admin, owner, Now));
        ctx.AccessGrants.Add(new Notrelix.Infrastructure.Data.Authz.AccessGrant(
            accountId: _a.AccountId, workspaceId: null, userId: _accountAdmin.Id, sourceContext: "Account",
            membershipStatus: "Active", roleCodes: ["Admin"], permissionCodes: [], isAccountAdmin: true,
            isWorkspaceAdmin: false, grantedAt: Now));
        ctx.Boards.Add(privateBoard);
        ctx.BoardGroups.AddRange(group, privateGroup);
        ctx.BoardItems.AddRange(item, privateItem);
        ctx.Forms.AddRange(form, privateForm);
        ctx.Comments.Add(comment);
        await ctx.SaveChangesAsync();

        _itemId = item.Id;
        _privateItemId = privateItem.Id;
        _formId = form.Id;
        _privateFormId = privateForm.Id;
        _commentId = comment.Id;
    }

    public Task DisposeAsync()
    {
        _factory?.Dispose();
        return Task.CompletedTask;
    }

    private WebApplicationFactory<Program>? _factory;

    private WebApplicationFactory<Program> Factory => _factory ??= AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);

    private async Task<(HttpStatusCode Status, string Body)> AsAsync(User who, HttpMethod method, string url, string? json = null)
    {
        var factory = Factory;
        string token;
        using (var scope = factory.Services.CreateScope())
            token = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(who);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        using var request = new HttpRequestMessage(method, url);
        if (json is not null) request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(request);
        return (response.StatusCode, Normalize(await response.Content.ReadAsStringAsync()));
    }

    // The body may echo the requested id or a trace id; compare what the answer says, not which id it was asked about.
    private static string Normalize(string body) =>
        Regex.Replace(body, "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}|[0-9a-f]{32}|0H[0-9A-Z]{11}", "<id>");

    private IEnumerable<(string Name, HttpMethod Method, string Existing, string Missing, string? Json)> Routes(Guid itemId, Guid formId)
    {
        yield return ("board item", HttpMethod.Get, $"/api/v1/board-items/{itemId}", $"/api/v1/board-items/{Guid.NewGuid()}", null);
        yield return ("form", HttpMethod.Get, $"/api/v1/forms/{formId}", $"/api/v1/forms/{Guid.NewGuid()}", null);
        yield return ("comments of an item", HttpMethod.Get, $"/api/v1/board-items/{itemId}/comments", $"/api/v1/board-items/{Guid.NewGuid()}/comments", null);
    }

    // Every Workspaces read a person outside the Workspace can try: the roster, their own membership, the
    // invitations, the Space tree and the Space list. An existing Workspace answers like a made-up one.
    private async Task AssertWorkspaceReadsIdenticalAsync(User who, string because)
    {
        foreach (var route in new[] { "members", "members/me", "invitations", "spaces/tree", "spaces" })
        {
            var found = await AsAsync(who, HttpMethod.Get, $"/api/v1/workspaces/{_a.WorkspaceId}/{route}");
            var made = await AsAsync(who, HttpMethod.Get, $"/api/v1/workspaces/{Guid.NewGuid()}/{route}");
            made.Status.Should().Be(HttpStatusCode.NotFound, $"{route}: a made-up workspace is NotFound");
            found.Should().Be(made, $"{route}: {because}");
        }
    }

    private async Task AssertIdenticalAsync(User who, string because, Guid itemId, Guid formId)
    {
        foreach (var (name, method, existing, missing, json) in Routes(itemId, formId))
        {
            var found = await AsAsync(who, method, existing, json);
            var made = await AsAsync(who, method, missing, json);
            made.Status.Should().Be(HttpStatusCode.NotFound, $"{name}: a made-up id is NotFound");
            found.Status.Should().Be(made.Status, $"{name}: {because}");
            found.Body.Should().Be(made.Body, $"{name}: {because}");
        }
    }

    [Fact]
    public async Task SameAccountNonMember_GetsTheIdenticalNotFound_ForExistingAndMadeUpIds()
    {
        await AssertIdenticalAsync(_sameAccountOutsider, "a person of the same account outside the Workspace learns nothing", _itemId, _formId);

        var comment = await AsAsync(_sameAccountOutsider, HttpMethod.Patch, $"/api/v1/comments/{_commentId}", "{\"contentMd\":\"x\",\"expectedVersion\":1}");
        var madeComment = await AsAsync(_sameAccountOutsider, HttpMethod.Patch, $"/api/v1/comments/{Guid.NewGuid()}", "{\"contentMd\":\"x\",\"expectedVersion\":1}");
        madeComment.Status.Should().Be(HttpStatusCode.NotFound);
        comment.Should().Be(madeComment, "an existing comment answers like a made-up one");

        await AssertWorkspaceReadsIdenticalAsync(_sameAccountOutsider, "a person without a seat in the Workspace learns nothing");
    }

    [Fact]
    public async Task MemberOfAnotherAccount_GetsTheIdenticalNotFound_ForExistingAndMadeUpIds()
    {
        await AssertIdenticalAsync(_b.User, "a person of another account learns nothing", _itemId, _formId);

        await AssertWorkspaceReadsIdenticalAsync(_b.User, "a person without a seat in the Workspace learns nothing");
    }

    [Fact]
    public async Task AccountAdminWithoutWorkspaceMembership_GetsTheIdenticalNotFound_ForExistingAndMadeUpIds()
    {
        await AssertIdenticalAsync(_accountAdmin, "an Account admin who is not in the Workspace learns nothing", _itemId, _formId);

        await AssertWorkspaceReadsIdenticalAsync(_accountAdmin, "a person without a seat in the Workspace learns nothing");
    }

    [Fact]
    public async Task SuspendedMember_GetsTheIdenticalNotFound_ForExistingAndMadeUpIds()
    {
        await AssertIdenticalAsync(_suspended, "a suspended member holds no access and learns nothing", _itemId, _formId);

        await AssertWorkspaceReadsIdenticalAsync(_suspended, "a person without a seat in the Workspace learns nothing");
    }

    [Fact]
    public async Task MemberWithoutAGrantOnAPrivateBoard_GetsTheIdenticalNotFound()
    {
        await AssertIdenticalAsync(_member, "a member with no grant on a Private Board learns nothing", _privateItemId, _privateFormId);
    }

    [Fact]
    public async Task RealtimeJoin_AnswersTheSame_ForAnExistingAndAMadeUpScope_ToAPersonWithoutAccess()
    {
        var authorizer = Factory.Services.GetRequiredService<IRealtimeSubscriptionAuthorizer>();
        var outsiders = new[] { _sameAccountOutsider, _b.User, _suspended, _accountAdmin };
        foreach (var who in outsiders)
        {
            var principal = new RealtimePrincipal(who.Id, who.Email.Value, who.Name, null, null, null, null, Guid.NewGuid());
            foreach (var (kind, existing) in new[]
            {
                (RealtimeScopeKind.Workspace, _a.WorkspaceId),
                (RealtimeScopeKind.Board, _a.BoardId),
            })
            {
                var found = await authorizer.AuthorizeAsync(principal, new RealtimeScope(kind, existing), default);
                var made = await authorizer.AuthorizeAsync(principal, new RealtimeScope(kind, Guid.NewGuid()), default);
                made.Outcome.Should().Be(RealtimeAuthorizationOutcome.Denied);
                found.Outcome.Should().Be(made.Outcome, $"{kind}: joining an existing scope is answered like joining a made-up one");
                found.Grant.Should().BeNull();
            }
        }

        // The member of the same Workspace still joins: the denial is about access, not about the route.
        var memberPrincipal = new RealtimePrincipal(_member.Id, _member.Email.Value, _member.Name, null, null, null, null, Guid.NewGuid());
        (await authorizer.AuthorizeAsync(memberPrincipal, new RealtimeScope(RealtimeScopeKind.Workspace, _a.WorkspaceId), default))
            .Outcome.Should().Be(RealtimeAuthorizationOutcome.Granted);
    }

    [Fact]
    public async Task GuestWhoSeesTheWorkspace_StillGetsForbidden_ForTheMemberList()
    {
        var response = await AsAsync(_guest, HttpMethod.Get, $"/api/v1/workspaces/{_a.WorkspaceId}/members");

        response.Status.Should().Be(HttpStatusCode.Forbidden, "a Guest is a member who lacks the permission, not a stranger");
    }
}
