using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Identity.Tokens;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Infrastructure.Security.ApiTokens;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// A comment, an attachment or a reaction takes the token scope of the resource it sits on: a board item needs work,
/// a page needs docs, read for a read and write for a write. Over the production graph on the application login
/// (dependency injection as in production, an app login that is not the owner, row-level security on). A comment or
/// attachment id alone does not name its host, so editing, deleting or resolving a comment, deleting an item
/// attachment, activity, watchers and the inbox stay refused to every token; the issuer's own permissions still apply.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class ApiTokenCollaborationAppRoleTests : IAsyncLifetime
{
    private const string ScopeMissing = "identity.token.scope-missing";
    private const string ItemKind = "work-management.board-item";
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly string ThumbsUp = Uri.EscapeDataString("\U0001F44D");

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private Guid _itemId;
    private Guid _pageId;
    private Guid _commentId;
    private WebApplicationFactory<Program>? _factory;

    public ApiTokenCollaborationAppRoleTests(AppRoleTestContainer fixture) => _fixture = fixture;

    private WebApplicationFactory<Program> Factory => _factory ??= AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        string hash;
        using (var scope = Factory.Services.CreateScope())
            hash = scope.ServiceProvider.GetRequiredService<IPasswordHasher>().HashPassword("Correct-Horse-9!");

        _a = await AppRoleTenants.SeedAsync(_fixture, "owner-collab-scopes@example.com", "Owner", hash);

        var owner = _a.User.Id;
        var group = BoardGroup.Create(_a.AccountId, _a.WorkspaceId, _a.BoardId, "Todo", Color.Create("#808080"), FractionalIndex.Create("a0"), owner, Now);
        var item = BoardItem.CreateRoot(_a.AccountId, _a.WorkspaceId, _a.BoardId, group.Id, "Task", FractionalIndex.Create("a0"), owner, Now);
        var page = Page.Create(_a.AccountId, _a.WorkspaceId, "Plan", owner, Now);
        var comment = Comment.Create(
            _a.AccountId, _a.WorkspaceId, ResourceRef.Create(ResourceKind.Create(ItemKind), item.Id, _a.WorkspaceId), "A comment", owner, Now);

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.BoardGroups.Add(group);
        ctx.BoardItems.Add(item);
        ctx.Pages.Add(page);
        ctx.Comments.Add(comment);
        await ctx.SaveChangesAsync();

        _itemId = item.Id;
        _pageId = page.Id;
        _commentId = comment.Id;
    }

    public Task DisposeAsync()
    {
        _factory?.Dispose();
        return Task.CompletedTask;
    }

    private async Task<HttpClient> BearerAsync(params string[] scopes)
    {
        var secret = new ApiTokenSecretService().Generate();
        var token = ApiToken.Create(
            _a.AccountId, _a.WorkspaceId, _a.User.Id, $"collab-{Guid.NewGuid():N}"[..16], secret.TokenHash,
            ApiTokenScopes.Create(scopes), _a.User.Id, DateTimeOffset.UtcNow, expiresAt: null);
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using (var ctx = _fixture.CreateOwnerContext(system))
        {
            ctx.ApiTokens.Add(token);
            await ctx.SaveChangesAsync();
        }

        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", secret.RawToken);
        return client;
    }

    private static HttpRequestMessage Send(HttpMethod method, string url, string? json = null, bool idempotent = false)
    {
        var request = new HttpRequestMessage(method, url);
        if (json is not null)
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        if (idempotent)
            request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        return request;
    }

    private string ItemComments => $"/api/v1/board-items/{_itemId}/comments";
    private string PageComments => $"/api/v1/pages/{_pageId}/comments";

    private static async Task AssertScopeMissingAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, body);
        body.Should().Contain(ScopeMissing);
    }

    private async Task<int> CommentCountAsync(string content)
    {
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        return await ctx.Comments.CountAsync(c => c.Content == content);
    }

    [Fact]
    public async Task AWorkWriteToken_CommentsOnABoardItem()
    {
        using var client = await BearerAsync("work:write");

        var created = await client.SendAsync(Send(HttpMethod.Post, ItemComments, """{"contentMd":"Written by a work token"}""", idempotent: true));

        created.StatusCode.Should().Be(HttpStatusCode.Created, await created.Content.ReadAsStringAsync());
        (await CommentCountAsync("Written by a work token")).Should().Be(1);
    }

    [Fact]
    public async Task AWorkWriteToken_IsRefusedOnAPageComment_AndStoresNothing()
    {
        using var client = await BearerAsync("work:write", "work:read");

        await AssertScopeMissingAsync(await client.SendAsync(
            Send(HttpMethod.Post, PageComments, """{"contentMd":"Page comment by a work token"}""", idempotent: true)));

        (await CommentCountAsync("Page comment by a work token")).Should().Be(0);
    }

    [Fact]
    public async Task ADocsReadToken_ReadsPageComments_ButCannotWriteOne()
    {
        using var client = await BearerAsync("docs:read");

        (await client.GetAsync(PageComments)).StatusCode.Should().Be(HttpStatusCode.OK);
        await AssertScopeMissingAsync(await client.SendAsync(
            Send(HttpMethod.Post, PageComments, """{"contentMd":"Page comment by a docs reader"}""", idempotent: true)));
        await AssertScopeMissingAsync(await client.GetAsync(ItemComments));

        (await CommentCountAsync("Page comment by a docs reader")).Should().Be(0);
    }

    [Fact]
    public async Task ADocsWriteToken_CommentsOnAPage_ButNotOnAnItem()
    {
        using var client = await BearerAsync("docs:write");

        var created = await client.SendAsync(Send(HttpMethod.Post, PageComments, """{"contentMd":"Written by a docs token"}""", idempotent: true));
        created.StatusCode.Should().Be(HttpStatusCode.Created, await created.Content.ReadAsStringAsync());

        await AssertScopeMissingAsync(await client.SendAsync(
            Send(HttpMethod.Post, ItemComments, """{"contentMd":"Item comment by a docs token"}""", idempotent: true)));
    }

    [Fact]
    public async Task AWorkReadToken_ReadsItemComments_ButCannotWriteOne()
    {
        using var client = await BearerAsync("work:read");

        (await client.GetAsync(ItemComments)).StatusCode.Should().Be(HttpStatusCode.OK);
        await AssertScopeMissingAsync(await client.SendAsync(
            Send(HttpMethod.Post, ItemComments, """{"contentMd":"Item comment by a reader"}""", idempotent: true)));

        (await CommentCountAsync("Item comment by a reader")).Should().Be(0);
    }

    [Fact]
    public async Task AReactionAndAnAttachment_FollowTheScopeOfTheItemTheySitOn()
    {
        var react = $"{ItemComments}/{_commentId}/reactions/{ThumbsUp}";
        var attachments = $"/api/v1/board-items/{_itemId}/attachments";
        const string link = """{"filename":"spec.txt","url":"https://example.com/spec.txt"}""";

        using var worker = await BearerAsync("work:write", "work:read");
        var reacted = await worker.SendAsync(Send(HttpMethod.Put, react));
        reacted.IsSuccessStatusCode.Should().BeTrue(await reacted.Content.ReadAsStringAsync());
        var attached = await worker.SendAsync(Send(HttpMethod.Post, attachments, link));
        attached.StatusCode.Should().Be(HttpStatusCode.Created, await attached.Content.ReadAsStringAsync());
        (await worker.GetAsync(attachments)).StatusCode.Should().Be(HttpStatusCode.OK);

        using var reader = await BearerAsync("work:read");
        (await reader.GetAsync(attachments)).StatusCode.Should().Be(HttpStatusCode.OK);
        await AssertScopeMissingAsync(await reader.SendAsync(Send(HttpMethod.Put, react)));
        await AssertScopeMissingAsync(await reader.SendAsync(Send(HttpMethod.Post, attachments, link)));

        using var docs = await BearerAsync("docs:read", "docs:write");
        await AssertScopeMissingAsync(await docs.SendAsync(Send(HttpMethod.Put, react)));
        await AssertScopeMissingAsync(await docs.GetAsync(attachments));
        await AssertScopeMissingAsync(await docs.SendAsync(Send(HttpMethod.Post, attachments, link)));
    }

    [Fact]
    public async Task ARequestThatDoesNotNameItsHostKind_StaysRefusedToATokenOfEveryScope()
    {
        using var client = await BearerAsync([.. ApiTokenScopes.Vocabulary]);

        // The comment id alone does not say what the comment sits on, so these need a database read and are refused.
        await AssertScopeMissingAsync(await client.SendAsync(
            Send(HttpMethod.Patch, $"/api/v1/comments/{_commentId}", """{"contentMd":"x","expectedVersion":1}""")));
        await AssertScopeMissingAsync(await client.SendAsync(Send(HttpMethod.Delete, $"/api/v1/comments/{_commentId}?expectedVersion=1")));
        await AssertScopeMissingAsync(await client.SendAsync(Send(HttpMethod.Post, $"/api/v1/comments/{_commentId}/resolve")));

        // Watchers, activity and the inbox are person-level or feeds.
        await AssertScopeMissingAsync(await client.GetAsync($"/api/v1/board-items/{_itemId}/following"));
        await AssertScopeMissingAsync(await client.SendAsync(Send(HttpMethod.Put, $"/api/v1/board-items/{_itemId}/following", idempotent: true)));
        await AssertScopeMissingAsync(await client.GetAsync($"/api/v1/board-items/{_itemId}/activity"));
    }
}
