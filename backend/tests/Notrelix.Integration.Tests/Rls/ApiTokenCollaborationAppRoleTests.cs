using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API;
using Notrelix.Domain.Collaboration.Attachments;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Identity.Tokens;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.Security.ApiTokens;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// A comment, an attachment or a reaction takes the token scope of the resource it sits on: a board item needs work,
/// a page needs docs, read for a read and write for a write. Over the production graph on the application login
/// (dependency injection as in production, an app login that is not the owner, row-level security on). A request keyed
/// by a comment or attachment id (edit, resolve, reopen or delete a comment, delete an item attachment) takes the scope
/// of the host kind the resource locator returns for that id; activity, watchers and the inbox stay refused to every
/// token; the issuer's own permissions still apply.
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
    private Guid _pageCommentId;
    private Guid _itemAttachmentId;
    private Guid _pageAttachmentId;
    private Guid _otherWorkspaceCommentId;
    private Guid _otherWorkspaceAttachmentId;
    private string _hash = null!;
    private WebApplicationFactory<Program>? _factory;

    public ApiTokenCollaborationAppRoleTests(AppRoleTestContainer fixture) => _fixture = fixture;

    private WebApplicationFactory<Program> Factory => _factory ??= AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        using (var scope = Factory.Services.CreateScope())
            _hash = scope.ServiceProvider.GetRequiredService<IPasswordHasher>().HashPassword("Correct-Horse-9!");

        _a = await AppRoleTenants.SeedAsync(_fixture, "owner-collab-scopes@example.com", "Owner", _hash);
        var other = await AppRoleTenants.SeedAsync(_fixture, "owner-collab-other@example.com", "Other", _hash);

        var owner = _a.User.Id;
        var group = BoardGroup.Create(_a.AccountId, _a.WorkspaceId, _a.BoardId, "Todo", Color.Create("#808080"), FractionalIndex.Create("a0"), owner, Now);
        var item = BoardItem.CreateRoot(_a.AccountId, _a.WorkspaceId, _a.BoardId, group.Id, "Task", FractionalIndex.Create("a0"), owner, Now);
        var page = Page.Create(_a.AccountId, _a.WorkspaceId, "Plan", owner, Now);
        var comment = Comment.Create(
            _a.AccountId, _a.WorkspaceId, ResourceRef.Create(ResourceKind.Create(ItemKind), item.Id, _a.WorkspaceId), "A comment", owner, Now);

        var pageComment = Comment.Create(
            _a.AccountId, _a.WorkspaceId, ResourceRef.Create(ResourceKind.Create("documents.page"), page.Id, _a.WorkspaceId), "Secret page comment", owner, Now);

        ResourceRef On(string kind, Guid id, Guid workspace) => ResourceRef.Create(ResourceKind.Create(kind), id, workspace);
        Attachment Link(Guid account, Guid workspace, ResourceRef target) => Attachment.Create(
            account, workspace, target, AttachmentType.Link, FileMetadata.Create("spec", 0, "text/uri-list", url: "https://example.com/spec"), owner, Now);
        var itemAttachment = Link(_a.AccountId, _a.WorkspaceId, On(ItemKind, item.Id, _a.WorkspaceId));
        var pageAttachment = Link(_a.AccountId, _a.WorkspaceId, On("documents.page", page.Id, _a.WorkspaceId));
        var otherItem = Guid.NewGuid();
        var otherComment = Comment.Create(other.AccountId, other.WorkspaceId, On(ItemKind, otherItem, other.WorkspaceId), "Other workspace comment", other.User.Id, Now);
        var otherAttachment = Link(other.AccountId, other.WorkspaceId, On(ItemKind, otherItem, other.WorkspaceId));

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.Attachments.AddRange(itemAttachment, pageAttachment);
        ctx.Comments.Add(otherComment);
        ctx.Attachments.Add(otherAttachment);
        ctx.BoardGroups.Add(group);
        ctx.Comments.Add(pageComment);
        ctx.BoardItems.Add(item);
        ctx.Pages.Add(page);
        ctx.Comments.Add(comment);
        await ctx.SaveChangesAsync();

        _itemId = item.Id;
        _pageId = page.Id;
        _commentId = comment.Id;
        _pageCommentId = pageComment.Id;
        _itemAttachmentId = itemAttachment.Id;
        _pageAttachmentId = pageAttachment.Id;
        _otherWorkspaceCommentId = otherComment.Id;
        _otherWorkspaceAttachmentId = otherAttachment.Id;
    }

    public Task DisposeAsync()
    {
        _factory?.Dispose();
        return Task.CompletedTask;
    }

    private Task<HttpClient> BearerAsync(params string[] scopes) => BearerForAsync(_a.User.Id, scopes);

    private async Task<HttpClient> BearerForAsync(Guid issuerId, params string[] scopes)
    {
        var secret = new ApiTokenSecretService().Generate();
        var token = ApiToken.Create(
            _a.AccountId, _a.WorkspaceId, issuerId, $"collab-{Guid.NewGuid():N}"[..16], secret.TokenHash,
            ApiTokenScopes.Create(scopes), issuerId, DateTimeOffset.UtcNow, expiresAt: null);
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
    public async Task ARequestThatNamesNoHostResource_StaysRefusedToATokenOfEveryScope()
    {
        using var client = await BearerAsync([.. ApiTokenScopes.Vocabulary]);

        // Watchers, activity and the inbox are person-level or feeds.
        await AssertScopeMissingAsync(await client.GetAsync($"/api/v1/board-items/{_itemId}/following"));
        await AssertScopeMissingAsync(await client.SendAsync(Send(HttpMethod.Put, $"/api/v1/board-items/{_itemId}/following", idempotent: true)));
        await AssertScopeMissingAsync(await client.GetAsync($"/api/v1/board-items/{_itemId}/activity"));
    }

    [Fact]
    public async Task MentionSuggestions_AreAUiAid_AndRefusedToATokenOfAnyScope_OnAnItemAndOnAPage()
    {
        var item = $"/api/v1/board-items/{_itemId}/mention-suggestions?query=a";
        var page = $"/api/v1/pages/{_pageId}/mention-suggestions?query=a";

        foreach (var scopes in new[] { new[] { "work:read" }, ["docs:read"], ["work:read", "docs:read"], [.. ApiTokenScopes.Vocabulary] })
        {
            using var client = await BearerAsync(scopes);
            await AssertScopeMissingAsync(await client.GetAsync(item));
            await AssertScopeMissingAsync(await client.GetAsync(page));
        }
    }

    private static async Task<JsonElement> CommentAsync(HttpClient client, string url, Guid id)
    {
        var body = await client.GetStringAsync(url);
        var list = JsonDocument.Parse(body).RootElement;
        return list.EnumerateArray().Single(c => c.GetProperty("id").GetGuid() == id).Clone();
    }

    private static HttpRequestMessage Edit(Guid commentId, string text, long version) =>
        Send(HttpMethod.Patch, $"/api/v1/comments/{commentId}", $$"""{"contentMd":"{{text}}","expectedVersion":{{version}}}""");

    private static HttpRequestMessage Remove(Guid commentId, long version) =>
        Send(HttpMethod.Delete, $"/api/v1/comments/{commentId}?expectedVersion={version}");

    private static HttpRequestMessage Resolve(Guid commentId) => Send(HttpMethod.Post, $"/api/v1/comments/{commentId}/resolve");

    private static HttpRequestMessage Reopen(Guid commentId) => Send(HttpMethod.Post, $"/api/v1/comments/{commentId}/reopen");

    /// <summary>Edits, resolves, reopens and deletes one comment through its own id and checks each step landed.</summary>
    private static async Task WriteLifecycleAsync(HttpClient client, string commentsUrl, Guid commentId, string text)
    {
        var version = (await CommentAsync(client, commentsUrl, commentId)).GetProperty("version").GetInt64();
        var edited = await client.SendAsync(Edit(commentId, text, version));
        edited.IsSuccessStatusCode.Should().BeTrue(await edited.Content.ReadAsStringAsync());
        var afterEdit = await CommentAsync(client, commentsUrl, commentId);
        afterEdit.GetProperty("contentMd").GetString().Should().Be(text);

        var resolved = await client.SendAsync(Resolve(commentId));
        resolved.IsSuccessStatusCode.Should().BeTrue(await resolved.Content.ReadAsStringAsync());
        (await CommentAsync(client, commentsUrl, commentId)).GetProperty("resolvedAt").ValueKind.Should().NotBe(JsonValueKind.Null);

        var reopened = await client.SendAsync(Reopen(commentId));
        reopened.IsSuccessStatusCode.Should().BeTrue(await reopened.Content.ReadAsStringAsync());
        (await CommentAsync(client, commentsUrl, commentId)).GetProperty("resolvedAt").ValueKind.Should().Be(JsonValueKind.Null);

        var current = (await CommentAsync(client, commentsUrl, commentId)).GetProperty("version").GetInt64();
        var removed = await client.SendAsync(Remove(commentId, current));
        removed.IsSuccessStatusCode.Should().BeTrue(await removed.Content.ReadAsStringAsync());
        // A deleted comment with no replies leaves the thread.
        JsonDocument.Parse(await client.GetStringAsync(commentsUrl)).RootElement.EnumerateArray()
            .Select(c => c.GetProperty("id").GetGuid()).Should().NotContain(commentId);
    }

    private async Task AssertEveryCommentWriteRefusedAsync(HttpClient client, Guid commentId)
    {
        await AssertScopeMissingAsync(await client.SendAsync(Edit(commentId, "Must not land", 1)));
        await AssertScopeMissingAsync(await client.SendAsync(Remove(commentId, 1)));
        await AssertScopeMissingAsync(await client.SendAsync(Resolve(commentId)));
        await AssertScopeMissingAsync(await client.SendAsync(Reopen(commentId)));
    }

    private async Task<bool> CommentIsUntouchedAsync(Guid commentId, string content)
    {
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        return await ctx.Comments.AnyAsync(c => c.Id == commentId && c.Content == content && c.CommentStatus == CommentStatus.Active);
    }

    [Fact]
    public async Task AWorkWriteToken_EditsResolvesReopensAndDeletes_ACommentOnABoardItem()
    {
        using var client = await BearerAsync("work:write", "work:read");

        await WriteLifecycleAsync(client, ItemComments, _commentId, "Edited by a work token");
    }

    [Fact]
    public async Task AWorkWriteToken_IsRefusedEveryWriteOnAPageComment_AndTheCommentIsUntouched()
    {
        using var client = await BearerAsync("work:write", "work:read");

        await AssertEveryCommentWriteRefusedAsync(client, _pageCommentId);

        (await CommentIsUntouchedAsync(_pageCommentId, "Secret page comment")).Should().BeTrue();
    }

    [Fact]
    public async Task ADocsWriteToken_HandlesAPageComment_ButIsRefusedEveryWriteOnAnItemComment()
    {
        using var client = await BearerAsync("docs:write", "docs:read");

        await AssertEveryCommentWriteRefusedAsync(client, _commentId);
        (await CommentIsUntouchedAsync(_commentId, "A comment")).Should().BeTrue();

        await WriteLifecycleAsync(client, PageComments, _pageCommentId, "Edited by a docs token");
    }

    [Fact]
    public async Task AWorkReadToken_CannotEditOrDeleteAComment_ButStillReadsThem()
    {
        using var client = await BearerAsync("work:read");

        (await client.GetAsync(ItemComments)).StatusCode.Should().Be(HttpStatusCode.OK);
        await AssertEveryCommentWriteRefusedAsync(client, _commentId);

        (await CommentIsUntouchedAsync(_commentId, "A comment")).Should().BeTrue();
    }

    [Fact]
    public async Task AWorkWriteToken_DeletesAnItemAttachment_ButNeitherAWorkReadNorADocsToken()
    {
        var url = $"/api/v1/board-items/{_itemId}/attachments/{_itemAttachmentId}";

        using var reader = await BearerAsync("work:read");
        await AssertScopeMissingAsync(await reader.SendAsync(Send(HttpMethod.Delete, url)));

        using var docs = await BearerAsync("docs:write", "docs:read");
        await AssertScopeMissingAsync(await docs.SendAsync(Send(HttpMethod.Delete, url)));

        using var writer = await BearerAsync("work:write", "work:read");
        var deleted = await writer.SendAsync(Send(HttpMethod.Delete, url));
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent, await deleted.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task AnAttachmentIdOfAnotherHostKindOrWorkspaceOrARandomOne_GetsTheSame404()
    {
        using var all = await BearerAsync([.. ApiTokenScopes.Vocabulary]);
        using var work = await BearerAsync(ItemScopes);

        foreach (var client in new[] { all, work })
        {
            var missing = await SendRouteAsync(client, new("random", HttpMethod.Delete, _ => $"/api/v1/board-items/{_itemId}/attachments/{Guid.NewGuid()}", null), Guid.Empty);
            missing.Status.Should().Be(HttpStatusCode.NotFound);
            foreach (var id in new[] { _pageAttachmentId, _otherWorkspaceAttachmentId })
            {
                var spoof = await SendRouteAsync(client, new("spoof", HttpMethod.Delete, _ => $"/api/v1/board-items/{_itemId}/attachments/{id}", null), Guid.Empty);
                spoof.Status.Should().Be(HttpStatusCode.NotFound, id.ToString());
                spoof.Body.Should().Be(missing.Body, "no oracle for a page attachment or another workspace's attachment");
            }
        }

        // The page attachment is still there: only the page route removes it.
        using var docs = await BearerAsync("docs:write", "docs:read");
        (await docs.GetAsync($"/api/v1/pages/{_pageId}/attachments")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ACommentIdOfAnotherWorkspaceOrARandomOne_GetsTheSame404_ForEveryCommentWrite()
    {
        using var client = await BearerAsync("work:write", "work:read");

        foreach (var write in new Func<Guid, HttpRequestMessage>[] { id => Edit(id, "x", 1), id => Remove(id, 1), Resolve, Reopen })
        {
            var missing = await client.SendAsync(write(Guid.NewGuid()));
            var other = await client.SendAsync(write(_otherWorkspaceCommentId));
            var missingBody = Normalize(await missing.Content.ReadAsStringAsync());
            missing.StatusCode.Should().Be(HttpStatusCode.NotFound, missingBody);
            other.StatusCode.Should().Be(HttpStatusCode.NotFound);
            Normalize(await other.Content.ReadAsStringAsync()).Should().Be(missingBody, "no oracle for another workspace's comment");
        }
    }

    [Fact]
    public async Task ATokenOfAnIssuerWhoIsNotTheAuthor_StillCannotEditOrDeleteTheComment()
    {
        var member = await AppRoleMembers.AddAsync(
            _fixture, _a, "Member", WorkspaceRole.Member, WorkspaceMemberStatus.Active, _hash);
        using var client = await BearerForAsync(member.Id, "work:write", "work:read");

        // The scope allows it; the author-only rule of the issuer's own permissions does not.
        var version = (await CommentAsync(client, ItemComments, _commentId)).GetProperty("version").GetInt64();
        var edit = await client.SendAsync(Edit(_commentId, "Edited by a non-author", version));
        var remove = await client.SendAsync(Remove(_commentId, version));

        edit.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await edit.Content.ReadAsStringAsync()).Should().Contain("collaboration.comment_author_only").And.NotContain(ScopeMissing);
        remove.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await remove.Content.ReadAsStringAsync()).Should().Contain("collaboration.comment_author_only").And.NotContain(ScopeMissing);
        (await CommentIsUntouchedAsync(_commentId, "A comment")).Should().BeTrue();
    }

    /// <summary>One request that carries a resource kind: the route names the kind, the id is the one under test.</summary>
    private sealed record KindRoute(string Name, HttpMethod Method, Func<Guid, string> Url, string? Json, bool Idempotent = false, bool Multipart = false);

    private static readonly string[] ItemScopes = ["work:read", "work:write"];
    private static readonly string[] PageScopes = ["docs:read", "docs:write"];

    private IEnumerable<KindRoute> ItemKindRoutes()
    {
        const string link = """{"filename":"spec.txt","url":"https://example.com/spec.txt"}""";
        yield return new("create item comment", HttpMethod.Post, id => $"/api/v1/board-items/{id}/comments", """{"contentMd":"Spoofed on an item route"}""", Idempotent: true);
        yield return new("read item comments", HttpMethod.Get, id => $"/api/v1/board-items/{id}/comments", null);
        yield return new("read item comments page", HttpMethod.Get, id => $"/api/v1/board-items/{id}/comments/page", null);
        yield return new("react", HttpMethod.Put, id => $"/api/v1/board-items/{id}/comments/{_pageCommentId}/reactions/{ThumbsUp}", null);
        yield return new("withdraw reaction", HttpMethod.Delete, id => $"/api/v1/board-items/{id}/comments/{_pageCommentId}/reactions/{ThumbsUp}", null);
        yield return new("list item attachments", HttpMethod.Get, id => $"/api/v1/board-items/{id}/attachments", null);
        yield return new("link item attachment", HttpMethod.Post, id => $"/api/v1/board-items/{id}/attachments", link);
        yield return new("upload item attachment", HttpMethod.Post, id => $"/api/v1/board-items/{id}/attachments/files", null, Idempotent: true, Multipart: true);
        yield return new("read item attachment content", HttpMethod.Get, id => $"/api/v1/board-items/{id}/attachments/{Guid.Empty}/content", null);
    }

    private IEnumerable<KindRoute> PageKindRoutes()
    {
        const string link = """{"filename":"spec.txt","url":"https://example.com/spec.txt"}""";
        yield return new("create page comment", HttpMethod.Post, id => $"/api/v1/pages/{id}/comments", """{"contentMd":"Spoofed on a page route"}""", Idempotent: true);
        yield return new("read page comments", HttpMethod.Get, id => $"/api/v1/pages/{id}/comments", null);
        yield return new("read page comments page", HttpMethod.Get, id => $"/api/v1/pages/{id}/comments/page", null);
        yield return new("list page attachments", HttpMethod.Get, id => $"/api/v1/pages/{id}/attachments", null);
        yield return new("link page attachment", HttpMethod.Post, id => $"/api/v1/pages/{id}/attachments", link);
        yield return new("upload page attachment", HttpMethod.Post, id => $"/api/v1/pages/{id}/attachments/files", null, Idempotent: true, Multipart: true);
        yield return new("read page attachment content", HttpMethod.Get, id => $"/api/v1/pages/{id}/attachments/{Guid.Empty}/content", null);
        yield return new("delete page attachment", HttpMethod.Delete, id => $"/api/v1/pages/{id}/attachments/{Guid.Empty}", null);
    }

    private static async Task<(HttpStatusCode Status, string Body)> SendRouteAsync(HttpClient client, KindRoute route, Guid id)
    {
        var request = Send(route.Method, route.Url(id), route.Json, route.Idempotent);
        if (route.Multipart)
        {
            var file = new ByteArrayContent([0x25, 0x50, 0x44, 0x46]);
            file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
            request.Content = new MultipartFormDataContent { { file, "file", "report.pdf" } };
        }

        var response = await client.SendAsync(request);
        return (response.StatusCode, Normalize(await response.Content.ReadAsStringAsync()));
    }

    private static string Normalize(string body)
    {
        var text = body;
        try
        {
            if (JsonNode.Parse(body) is JsonObject obj)
            {
                foreach (var key in obj.Select(kv => kv.Key).Where(k => k is "traceId" or "correlationId" or "instance" or "requestId").ToList())
                    obj.Remove(key);
                text = obj.ToJsonString();
            }
        }
        catch (JsonException)
        {
            // Not JSON (an empty body): compare the text as it is.
        }

        return Regex.Replace(text, "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}|[0-9a-f]{32}|0H[0-9A-Z]{11}", "<id>");
    }

    /// <summary>
    /// The scope comes from the kind the route names, so the id must exist AS that kind: the resource locator answers
    /// NotFound for an id of another kind before the scope or any handler runs. For every request that carries a kind,
    /// in both directions, the answer for a wrong-kind id (a page id sent as an item, an item id sent as a page) is the
    /// same 404 as for a random id: same status, same body, so the refusal is no oracle for the other resource.
    /// </summary>
    [Fact]
    public async Task AWrongKindForAnId_IsRefusedLikeAMissingResource_InBothDirections_ForEveryKindCarryingRequest()
    {
        using var work = await BearerAsync(ItemScopes);
        using var docs = await BearerAsync(PageScopes);

        foreach (var route in ItemKindRoutes())
        {
            var wrong = await SendRouteAsync(work, route, _pageId);
            var missing = await SendRouteAsync(work, route, Guid.NewGuid());
            missing.Status.Should().Be(HttpStatusCode.NotFound, $"{route.Name}: a random id");
            wrong.Status.Should().Be(HttpStatusCode.NotFound, $"{route.Name}: a page id sent as an item");
            wrong.Body.Should().Be(missing.Body, $"{route.Name}: no oracle for the page");
        }

        foreach (var route in PageKindRoutes())
        {
            var wrong = await SendRouteAsync(docs, route, _itemId);
            var missing = await SendRouteAsync(docs, route, Guid.NewGuid());
            missing.Status.Should().Be(HttpStatusCode.NotFound, $"{route.Name}: a random id");
            wrong.Status.Should().Be(HttpStatusCode.NotFound, $"{route.Name}: an item id sent as a page");
            wrong.Body.Should().Be(missing.Body, $"{route.Name}: no oracle for the item");
        }

        (await CommentCountAsync("Spoofed on an item route")).Should().Be(0);
        (await CommentCountAsync("Spoofed on a page route")).Should().Be(0);
    }

    [Fact]
    public async Task AWrongKindForAnId_DoesNotReturnTheOtherResourcesComments()
    {
        using var work = await BearerAsync(ItemScopes);
        using var docs = await BearerAsync(PageScopes);

        (await (await work.GetAsync($"/api/v1/board-items/{_pageId}/comments")).Content.ReadAsStringAsync()).Should().NotContain("Secret page comment");
        (await (await docs.GetAsync($"/api/v1/pages/{_itemId}/comments")).Content.ReadAsStringAsync()).Should().NotContain("A comment");
    }
}
