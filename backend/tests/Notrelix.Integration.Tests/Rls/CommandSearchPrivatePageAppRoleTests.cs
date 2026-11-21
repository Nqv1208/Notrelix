using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Infrastructure.Data.Workspaces;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// The command palette's Page search over HTTP on the application login (ADR-010): a Private Page (placed in a Private
/// Space) the caller cannot read is neither listed nor disclosed in the answer, a Guest finds only the Pages shared with
/// them, a Page shared afterwards shows on the next search, an unreadable parent never names itself in a readable
/// Page's location, and trashed or deleted Pages are never found, not even by the person who owns them.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class CommandSearchPrivatePageAppRoleTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private User _member = null!;
    private User _guest = null!;
    private Guid _open;
    private Guid _secret;
    private WebApplicationFactory<Program>? _factory;

    public CommandSearchPrivatePageAppRoleTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "a-page-search@example.com", "Aaa", "hash");
        _member = await AppRoleMembers.AddAsync(_fixture, _a, "PageMember", WorkspaceRole.Member, WorkspaceMemberStatus.Active, "hash");
        _guest = await AppRoleMembers.AddAsync(_fixture, _a, "PageGuest", WorkspaceRole.Guest, WorkspaceMemberStatus.Active, "hash");

        var owner = _a.User.Id;
        var privateSpace = Space.Create(_a.AccountId, _a.WorkspaceId, "Finance", SpaceVisibility.Private, owner, Now);
        var open = Page.Create(_a.AccountId, _a.WorkspaceId, "Zeta Handbook", owner, Now);
        var secret = Page.Create(_a.AccountId, _a.WorkspaceId, "Zeta Secret plan", owner, Now, spaceId: privateSpace.Id);
        var hiddenParent = Page.Create(_a.AccountId, _a.WorkspaceId, "Zeta Hidden parent", owner, Now);
        var child = Page.Create(_a.AccountId, _a.WorkspaceId, "Zeta Child notes", owner, Now, parentId: hiddenParent.Id);
        var trashed = Page.Create(_a.AccountId, _a.WorkspaceId, "Zeta Trashed draft", owner, Now);
        trashed.Archive(owner, Now);
        var deleted = Page.Create(_a.AccountId, _a.WorkspaceId, "Zeta Deleted draft", owner, Now);
        deleted.Delete(owner, Now);

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.Spaces.Add(privateSpace);
        ctx.SpacePaths.Add(new SpacePathRecord(_a.AccountId, _a.WorkspaceId, privateSpace.Id, privateSpace.Id, 0));
        ctx.Pages.AddRange(open, secret, hiddenParent, child, trashed, deleted);
        ctx.PermissionRules.Add(PermissionRule.Create(
            _a.AccountId, _a.WorkspaceId, PermissionScopeType.Board, ResourceKind.Create("documents.page"), hiddenParent.Id,
            PermissionSubjectType.User, _member.Id, null, PermissionAction.ViewPage, PermissionEffect.Deny, owner, Now));
        await ctx.SaveChangesAsync();

        _open = open.Id;
        _secret = secret.Id;
    }

    public Task DisposeAsync()
    {
        _factory?.Dispose();
        return Task.CompletedTask;
    }

    private WebApplicationFactory<Program> Factory => _factory ??= AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);

    private async Task<(HttpStatusCode Status, string Body, JsonNode Json)> SearchAsync(User who, string text)
    {
        var factory = Factory;
        string token;
        using (var scope = factory.Services.CreateScope())
            token = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(who);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        using var response = await client.GetAsync($"/api/v1/workspaces/{_a.WorkspaceId}/command-search?q={Uri.EscapeDataString(text)}");
        var body = await response.Content.ReadAsStringAsync();
        return (response.StatusCode, body, JsonNode.Parse(body)!);
    }

    private static string[] Titles(JsonNode json) =>
        json["docs"]!.AsArray().Select(d => d!["title"]!.GetValue<string>()).OrderBy(t => t).ToArray();

    private async Task GrantPageAsync(Guid pageId, Guid userId)
    {
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.ResourcePermissions.Add(ResourcePermission.Grant(
            _a.AccountId, _a.WorkspaceId, ResourceKind.Create("documents.page"), pageId,
            PermissionSubjectType.User, userId, PermissionLevel.Viewer, _a.User.Id, Now));
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task APrivatePageTheMemberCannotRead_IsNeitherListedNorDisclosed_ButTheOwnerFindsIt()
    {
        var asMember = await SearchAsync(_member, "zeta");

        asMember.Status.Should().Be(HttpStatusCode.OK);
        Titles(asMember.Json).Should().Equal(["Zeta Child notes", "Zeta Handbook"], "the Private Space's Page and the denied parent are not the member's to find");
        asMember.Body.Should().NotContain("Secret").And.NotContain("Hidden").And.NotContain("Finance");

        var asOwner = await SearchAsync(_a.User, "zeta");
        Titles(asOwner.Json).Should().Equal("Zeta Child notes", "Zeta Handbook", "Zeta Hidden parent", "Zeta Secret plan");
    }

    [Fact]
    public async Task AReadablePageUnderAnUnreadableParent_NeverNamesTheParentInItsLocation()
    {
        var asMember = await SearchAsync(_member, "child notes");

        var doc = asMember.Json["docs"]!.AsArray().Single()!;
        doc["title"]!.GetValue<string>().Should().Be("Zeta Child notes");
        doc["location"].Should().BeNull("the parent's title is not readable by this member");
        asMember.Body.Should().NotContain("Hidden parent");
    }

    [Fact]
    public async Task AGuestFindsNoPageUntilOneIsSharedWithThem_ThenOnlyThatPage()
    {
        var before = await SearchAsync(_guest, "zeta");
        Titles(before.Json).Should().BeEmpty("a Guest has no Page but those shared with them");
        before.Body.Should().NotContain("Zeta ");

        await GrantPageAsync(_open, _guest.Id);

        var after = await SearchAsync(_guest, "zeta");
        Titles(after.Json).Should().Equal("Zeta Handbook");
    }

    [Fact]
    public async Task APageSharedWithTheMemberAfterwards_AppearsOnTheNextSearch()
    {
        (await SearchAsync(_member, "secret plan")).Json["docs"]!.AsArray().Should().BeEmpty();

        await GrantPageAsync(_secret, _member.Id);

        Titles((await SearchAsync(_member, "secret plan")).Json).Should().Equal("Zeta Secret plan");
    }

    [Fact]
    public async Task TrashedAndDeletedPages_AreNeverFound_EvenByTheOwner()
    {
        var asOwner = await SearchAsync(_a.User, "draft");

        asOwner.Status.Should().Be(HttpStatusCode.OK);
        Titles(asOwner.Json).Should().BeEmpty("only active Pages are searched; the trash and deleted Pages are not part of the palette");
        asOwner.Body.Should().NotContain("Zeta Trashed").And.NotContain("Zeta Deleted");
    }
}
