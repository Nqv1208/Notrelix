using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Rls;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Collaboration;

/// <summary>
/// UC-COL-B01 / UC-COL-B02 over HTTP on the application login, through the production DI graph: a comment, a reply to
/// an item comment or a reply to a page comment that mentions a person who can view the target creates a Mention for that
/// person only. A member denied the page or the item's board and a person of another account stay in the comment text
/// but get no Mention, and the comment is kept.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class CommentMentionsAppRoleTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private User _viewer = null!;
    private User _blocked = null!;
    private Guid _page;
    private Guid _item;
    private WebApplicationFactory<Program>? _factory;

    public CommentMentionsAppRoleTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "a-comment-mention@example.com", "Aaa", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "b-comment-mention@example.com", "Bbb", "hash");
        _viewer = await AppRoleMembers.AddAsync(_fixture, _a, "ZedViewer", WorkspaceRole.Member, WorkspaceMemberStatus.Active, "hash");
        _blocked = await AppRoleMembers.AddAsync(_fixture, _a, "ZedBlocked", WorkspaceRole.Member, WorkspaceMemberStatus.Active, "hash");

        var owner = _a.User.Id;
        var page = Page.Create(_a.AccountId, _a.WorkspaceId, "Roadmap notes", owner, Now);
        var group = BoardGroup.Create(_a.AccountId, _a.WorkspaceId, _a.BoardId, "Todo", Color.Create("#808080"), FractionalIndex.Create("a0"), owner, Now);
        var item = BoardItem.CreateRoot(_a.AccountId, _a.WorkspaceId, _a.BoardId, group.Id, "Task", FractionalIndex.Create("a0"), owner, Now);
        await using var ctx = OwnerContext();
        ctx.Pages.Add(page);
        ctx.BoardGroups.Add(group);
        ctx.BoardItems.Add(item);
        ctx.PermissionRules.Add(PermissionRule.Create(
            _a.AccountId, _a.WorkspaceId, PermissionScopeType.Board, ResourceKind.Create("work-management.board"), _a.BoardId,
            PermissionSubjectType.User, _blocked.Id, null, PermissionAction.ViewBoard, PermissionEffect.Deny, owner, Now));
        ctx.PermissionRules.Add(PermissionRule.Create(
            _a.AccountId, _a.WorkspaceId, PermissionScopeType.Board, ResourceKind.Create("documents.page"), page.Id,
            PermissionSubjectType.User, _blocked.Id, null, PermissionAction.ViewPage, PermissionEffect.Deny, owner, Now));
        await ctx.SaveChangesAsync();
        _page = page.Id;
        _item = item.Id;
    }

    public Task DisposeAsync()
    {
        _factory?.Dispose();
        return Task.CompletedTask;
    }

    private WebApplicationFactory<Program> Factory => _factory ??= AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);

    private Notrelix.Infrastructure.Data.ApplicationDbContext OwnerContext()
    {
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        return _fixture.CreateOwnerContext(system);
    }

    private async Task<Guid> PostCommentAsync(string route, string text, Guid? parent, params Guid[] mentioned)
    {
        var factory = Factory;
        string token;
        using (var scope = factory.Services.CreateScope())
            token = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(_a.User);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        using var request = new HttpRequestMessage(HttpMethod.Post, route);
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        request.Content = JsonContent.Create(new { contentMd = text, parentCommentId = parent, mentionedUserIds = mentioned });
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, body);
        return JsonNode.Parse(body)!.GetValue<Guid>();
    }

    private async Task AssertOnlyTheViewerIsMentionedAsync(Guid target, Guid commentId, string text)
    {
        await using var ctx = OwnerContext();
        var mentioned = await ctx.PageMentions.IgnoreQueryFilters()
            .Where(m => m.Source.ResourceId == target)
            .Select(m => new { m.MentionedId, m.MentionedByUserId, m.WorkspaceId })
            .ToListAsync();
        mentioned.Should().ContainSingle("only the person who can view the target gets a Mention");
        mentioned[0].MentionedId.Should().Be(_viewer.Id);
        mentioned[0].MentionedByUserId.Should().Be(_a.User.Id);
        mentioned[0].WorkspaceId.Should().Be(_a.WorkspaceId);
        (await ctx.Comments.IgnoreQueryFilters().SingleAsync(c => c.Id == commentId)).Content.Should().Be(text);
    }

    [Fact]
    public async Task PageComment_MentionsOnlyTheColleagueWhoCanViewThePage()
    {
        const string text = "ping @ZedViewer @ZedBlocked @Bbb";
        var id = await PostCommentAsync($"/api/v1/pages/{_page}/comments", text, null, _viewer.Id, _blocked.Id, _b.User.Id);

        await AssertOnlyTheViewerIsMentionedAsync(_page, id, text);
    }

    [Fact]
    public async Task ReplyToAnItemComment_MentionsOnlyTheColleagueWhoCanViewTheBoard()
    {
        var route = $"/api/v1/board-items/{_item}/comments";
        var parent = await PostCommentAsync(route, "first", null);

        const string text = "re @ZedViewer @ZedBlocked @Bbb";
        var id = await PostCommentAsync(route, text, parent, _viewer.Id, _blocked.Id, _b.User.Id);

        await AssertOnlyTheViewerIsMentionedAsync(_item, id, text);
    }

    [Fact]
    public async Task ReplyToAPageComment_MentionsOnlyTheColleagueWhoCanViewThePage()
    {
        var route = $"/api/v1/pages/{_page}/comments";
        var parent = await PostCommentAsync(route, "first", null);

        const string text = "re @ZedViewer @ZedBlocked @Bbb";
        var id = await PostCommentAsync(route, text, parent, _viewer.Id, _blocked.Id, _b.User.Id);

        await AssertOnlyTheViewerIsMentionedAsync(_page, id, text);
    }
}
