using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API;
using Notrelix.Domain.Collaboration.Attachments;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Rls;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Collaboration;

/// <summary>
/// UC-COL-B06..B11 over HTTP on the application login through the production host (real PostgreSQL, row-level security
/// on, real idempotency store, real pipeline), for the board-item routes and the Documents page routes: list, content,
/// link create, file upload and delete. An owner and an editor do everything and a deleted attachment disappears; a
/// Viewer lists and reads but every write is a 403 that stores and deletes nothing (and a Viewer cannot delete even an
/// upload of their own); a person of another account, a same-account outsider and a suspended member get a NotFound
/// whose body equals the body for a made-up id; a suspended or closed Account refuses every write with
/// <c>account.suspended</c> while reads stay open; an archived workspace refuses writes with <c>workspace.archived</c>;
/// the same Idempotency-Key stores one row; a concurrent double delete is one 204 and one 404, never a 500.
/// The per-target cap is 4 here (configuration), so a full target is cheap to build.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class CollaborationAttachmentAppRoleTests : IAsyncLifetime
{
    private const int Cap = 4;
    private const string BoardKind = "work-management.board";
    private const string PageKind = "documents.page";
    private const string GuidPattern = "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}|[0-9a-f]{32}|0H[0-9A-Z]{11}";
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4, 5, 6, 7, 8];

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private User _editor = null!;
    private User _viewer = null!;
    private User _suspendedMember = null!;
    private User _outsider = null!;
    private Guid _itemId;
    private Guid _pageId;
    private WebApplicationFactory<Program>? _factory;

    public CollaborationAttachmentAppRoleTests(AppRoleTestContainer fixture) => _fixture = fixture;

    private WebApplicationFactory<Program> Factory => _factory ??= AppRoleHost.CreateFactory(
        _fixture,
        _fixture.AppConnectionString,
        new Dictionary<string, string?>
        {
            ["Collaboration:Attachments:MaxAttachmentsPerItem"] = Cap.ToString(),
            ["Collaboration:Attachments:MaxAttachmentsPerPage"] = Cap.ToString(),
        });

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "a-collab-att@example.com", "Aat", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "b-collab-att@example.com", "Bat", "hash");
        _outsider = await AppRoleUsers.AddAsync(_fixture, "outsider-collab-att@example.com", "OutAt", "hash");
        _editor = await AppRoleMembers.AddAsync(_fixture, _a, "AtEditor", WorkspaceRole.Member, WorkspaceMemberStatus.Active, "hash");
        _viewer = await AppRoleMembers.AddAsync(_fixture, _a, "AtViewer", WorkspaceRole.Member, WorkspaceMemberStatus.Active, "hash");
        _suspendedMember = await AppRoleMembers.AddAsync(_fixture, _a, "AtSuspended", WorkspaceRole.Member, WorkspaceMemberStatus.Suspended, "hash");

        var owner = _a.User.Id;
        var board = Board.Create(_a.AccountId, _a.WorkspaceId, owner, "Private board", null, Now, BoardVisibility.Private);
        var group = BoardGroup.Create(_a.AccountId, _a.WorkspaceId, board.Id, "Todo", Color.Create("#808080"), FractionalIndex.Create("a0"), owner, Now);
        var item = BoardItem.CreateRoot(_a.AccountId, _a.WorkspaceId, board.Id, group.Id, "Task", FractionalIndex.Create("a0"), owner, Now);
        var page = Page.Create(_a.AccountId, _a.WorkspaceId, "Plan", owner, Now);

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.Boards.Add(board);
        ctx.BoardGroups.Add(group);
        ctx.BoardItems.Add(item);
        ctx.Pages.Add(page);
        await ctx.SaveChangesAsync();
        await ctx.Database.ExecuteSqlRawAsync("UPDATE docs.pages SET visibility = 'Private' WHERE id = {0}", page.Id);

        ResourcePermission Grant(string kind, Guid id, User who, PermissionLevel level) => ResourcePermission.Grant(
            _a.AccountId, _a.WorkspaceId, ResourceKind.Create(kind), id, PermissionSubjectType.User, who.Id, level, owner, Now);
        ctx.ResourcePermissions.AddRange(
            Grant(BoardKind, board.Id, _editor, PermissionLevel.Editor),
            Grant(BoardKind, board.Id, _viewer, PermissionLevel.Viewer),
            Grant(PageKind, page.Id, _editor, PermissionLevel.Editor),
            Grant(PageKind, page.Id, _viewer, PermissionLevel.Viewer));
        await ctx.SaveChangesAsync();

        _itemId = item.Id;
        _pageId = page.Id;
    }

    public Task DisposeAsync()
    {
        _factory?.Dispose();
        return Task.CompletedTask;
    }

    // ---------------------------------------------------------------- plumbing

    /// <summary>One attachment host: the route family of a board item or of a page.</summary>
    private sealed record Target(string Name, Guid Id, string Base);

    private Target Of(string name) => name == "item"
        ? new("item", _itemId, $"/api/v1/board-items/{_itemId}/attachments")
        : new("page", _pageId, $"/api/v1/pages/{_pageId}/attachments");

    private sealed record Reply(HttpStatusCode Status, byte[] Bytes, bool Replayed)
    {
        public string Body => Encoding.UTF8.GetString(Bytes);

        public string? ErrorCode
        {
            get
            {
                try { return JsonNode.Parse(Body)?["errorCode"]?.GetValue<string>(); }
                catch (System.Text.Json.JsonException) { return null; }
            }
        }

        public JsonNode Json => JsonNode.Parse(Body)!;

        public Guid Id => Json["id"]!.GetValue<Guid>();
    }

    private async Task<Reply> SendAsync(User who, HttpMethod method, string url, HttpContent? content = null, string? key = null, bool withKey = true)
    {
        var factory = Factory;
        string token;
        using (var scope = factory.Services.CreateScope())
            token = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(who);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        using var request = new HttpRequestMessage(method, url) { Content = content };
        if (withKey && method != HttpMethod.Get && method != HttpMethod.Delete)
            request.Headers.Add("Idempotency-Key", key ?? Guid.NewGuid().ToString("D"));
        using var response = await client.SendAsync(request);
        return new(response.StatusCode, await response.Content.ReadAsByteArrayAsync(), response.Headers.Contains("Idempotency-Replayed"));
    }

    private static MultipartFormDataContent Form(string name, byte[]? bytes = null) =>
        new() { { new ByteArrayContent(bytes ?? Png), "file", name } };

    private static StringContent LinkBody(string filename, string url) =>
        new(new JsonObject { ["filename"] = filename, ["url"] = url }.ToJsonString(), Encoding.UTF8, "application/json");

    private Task<Reply> ListAsync(User who, Target t) => SendAsync(who, HttpMethod.Get, t.Base);

    private Task<Reply> ContentAsync(User who, Target t, Guid id) => SendAsync(who, HttpMethod.Get, $"{t.Base}/{id}/content");

    private Task<Reply> UploadAsync(User who, Target t, string name = "photo.png", string? key = null) =>
        SendAsync(who, HttpMethod.Post, $"{t.Base}/files", Form(name), key);

    private Task<Reply> LinkAsync(User who, Target t, string name = "Spec", string url = "https://example.com/spec", string? key = null) =>
        SendAsync(who, HttpMethod.Post, t.Base, LinkBody(name, url), key);

    private Task<Reply> DeleteAsync(User who, Target t, Guid id) => SendAsync(who, HttpMethod.Delete, $"{t.Base}/{id}");

    private async Task<object?> ScalarAsync(string sql, params (string, object)[] parameters)
    {
        await using var connection = new Npgsql.NpgsqlConnection(_fixture.OwnerConnectionString);
        await connection.OpenAsync();
        await using var command = new Npgsql.NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
        return await command.ExecuteScalarAsync();
    }

    private async Task<long> RowsAsync(Target t, bool liveOnly = false) => (long)(await ScalarAsync(
        $"SELECT count(*) FROM collab.attachments WHERE resource_id = @t{(liveOnly ? " AND deleted_at IS NULL" : "")}", ("t", t.Id)))!;

    private async Task<long> VersionAsync(Guid attachmentId) =>
        (long)(await ScalarAsync("SELECT version FROM collab.attachments WHERE id = @a", ("a", attachmentId)))!;

    private async Task<bool> IsDeletedAsync(Guid attachmentId) =>
        await ScalarAsync("SELECT deleted_at FROM collab.attachments WHERE id = @a", ("a", attachmentId)) is not DBNull;

    private async Task SetAccountStatusAsync(string status) =>
        await ScalarAsync("UPDATE account.accounts SET status = @s WHERE id = @a", ("s", status), ("a", _a.AccountId));

    private async Task SetWorkspaceStatusAsync(string status) =>
        await ScalarAsync("UPDATE workspace.workspaces SET status = @s WHERE id = @w", ("s", status), ("w", _a.WorkspaceId));

    /// <summary>A link row written by <paramref name="by"/> straight into the table, so a person who cannot write can still own one.</summary>
    private async Task<Guid> SeedLinkAsync(Target t, User by)
    {
        var kind = ResourceKind.Create(t.Name == "item" ? "work-management.board-item" : PageKind);
        var row = Attachment.Create(
            _a.AccountId, _a.WorkspaceId, ResourceRef.Create(kind, t.Id, _a.WorkspaceId), AttachmentType.Link,
            FileMetadata.Create("seeded", 0, "text/uri-list", url: "https://example.com/seeded"), by.Id, Now);
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.Attachments.Add(row);
        await ctx.SaveChangesAsync();
        return row.Id;
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
        catch (System.Text.Json.JsonException)
        {
            // Not JSON (an empty body): compare the text as it is.
        }

        return Regex.Replace(text, GuidPattern, "<id>");
    }

    private User Who(string name) => name switch
    {
        "owner" => _a.User,
        "editor" => _editor,
        "viewer" => _viewer,
        "otherAccount" => _b.User,
        "outsider" => _outsider,
        "suspendedMember" => _suspendedMember,
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };

    // ---------------------------------------------------------------- owner and editor

    [Theory]
    [InlineData("item", "owner")]
    [InlineData("item", "editor")]
    [InlineData("page", "owner")]
    [InlineData("page", "editor")]
    public async Task OwnerAndEditor_UploadListDownloadLinkAndDelete_ThenTheDeletedRowIsGoneFromListAndContent(string target, string actor)
    {
        var t = Of(target);
        var who = Who(actor);

        var upload = await UploadAsync(who, t);
        upload.Status.Should().Be(HttpStatusCode.Created, upload.Body);
        upload.Json["kind"]!.GetValue<string>().Should().Be("File");
        upload.Json["contentType"]!.GetValue<string>().Should().Be("image/png");
        upload.Json["resourceId"]!.GetValue<Guid>().Should().Be(t.Id);
        var link = await LinkAsync(who, t);
        link.Status.Should().Be(HttpStatusCode.Created, link.Body);
        link.Json["sizeBytes"]!.GetValue<long>().Should().Be(0);
        link.Json["contentType"]!.GetValue<string>().Should().Be("text/uri-list");
        link.Json["source"]!.GetValue<string>().Should().Be("Link");

        var list = await ListAsync(who, t);
        list.Status.Should().Be(HttpStatusCode.OK);
        list.Json.AsArray().Select(a => a!["id"]!.GetValue<Guid>()).Should().BeEquivalentTo([upload.Id, link.Id]);
        var content = await ContentAsync(who, t, upload.Id);
        content.Status.Should().Be(HttpStatusCode.OK);
        content.Bytes.Should().Equal(Png);

        var versionBefore = await VersionAsync(upload.Id);
        (await DeleteAsync(who, t, upload.Id)).Status.Should().Be(HttpStatusCode.NoContent);

        (await ListAsync(who, t)).Json.AsArray().Select(a => a!["id"]!.GetValue<Guid>()).Should().Equal(link.Id);
        (await ContentAsync(who, t, upload.Id)).Status.Should().Be(HttpStatusCode.NotFound, "the content of a deleted attachment is a plain 404");
        (await IsDeletedAsync(upload.Id)).Should().BeTrue("a delete is a soft delete");
        var versionAfter = await VersionAsync(upload.Id);
        versionAfter.Should().Be(versionBefore + 1);

        var second = await DeleteAsync(who, t, upload.Id);
        second.Status.Should().Be(HttpStatusCode.NotFound, "a second delete finds nothing");
        (await VersionAsync(upload.Id)).Should().Be(versionAfter, "a second delete changes nothing");
    }

    [Theory]
    [InlineData("item")]
    [InlineData("page")]
    public async Task ARouteOfTheOtherTarget_NeverReachesTheAttachment(string target)
    {
        var t = Of(target);
        var other = Of(target == "item" ? "page" : "item");
        var upload = await UploadAsync(_a.User, t);

        var content = await ContentAsync(_a.User, other, upload.Id);
        var delete = await DeleteAsync(_a.User, other, upload.Id);
        var list = await ListAsync(_a.User, other);

        content.Status.Should().Be(HttpStatusCode.NotFound);
        delete.Status.Should().Be(HttpStatusCode.NotFound);
        list.Json.AsArray().Should().BeEmpty();
        (await IsDeletedAsync(upload.Id)).Should().BeFalse();
    }

    [Theory]
    [InlineData("item")]
    [InlineData("page")]
    public async Task ALink_StoresServerOwnedSizeTypeAndSource_AndATrimmedName(string target)
    {
        var t = Of(target);
        var body = new StringContent(
            """{"filename":"  Docs\u0000 home ","url":"https://example.com/docs","sizeBytes":9000000000000,"contentType":"text/html","source":"File"}""",
            Encoding.UTF8, "application/json");

        var created = await SendAsync(_a.User, HttpMethod.Post, t.Base, body);

        created.Status.Should().Be(HttpStatusCode.Created, created.Body);
        created.Json["filename"]!.GetValue<string>().Should().Be("Docs home");
        (await ScalarAsync("SELECT file_size FROM collab.attachments WHERE id = @a", ("a", created.Id))).Should().Be(0L);
        (await ScalarAsync("SELECT mime_type FROM collab.attachments WHERE id = @a", ("a", created.Id))).Should().Be("text/uri-list");
        (await ScalarAsync("SELECT type FROM collab.attachments WHERE id = @a", ("a", created.Id))).Should().Be("Link");
    }

    // ---------------------------------------------------------------- viewer

    [Theory]
    [InlineData("item")]
    [InlineData("page")]
    public async Task Viewer_ListsAndReads_ButEveryWriteIs403_AndNothingIsStoredOrDeleted(string target)
    {
        var t = Of(target);
        var owned = await UploadAsync(_a.User, t);
        var viewersOwn = await SeedLinkAsync(t, _viewer);
        var rows = await RowsAsync(t);

        var list = await ListAsync(_viewer, t);
        var content = await ContentAsync(_viewer, t, owned.Id);
        var link = await LinkAsync(_viewer, t);
        var upload = await UploadAsync(_viewer, t, "more.png");
        var deleteOwners = await DeleteAsync(_viewer, t, owned.Id);
        var deleteOwn = await DeleteAsync(_viewer, t, viewersOwn);

        list.Status.Should().Be(HttpStatusCode.OK);
        list.Json.AsArray().Should().HaveCount(2);
        content.Status.Should().Be(HttpStatusCode.OK);
        content.Bytes.Should().Equal(Png);
        link.Status.Should().Be(HttpStatusCode.Forbidden);
        link.ErrorCode.Should().Be("auth.forbidden", "the pipeline's Forbidden carries no attachment-specific code");
        upload.Status.Should().Be(HttpStatusCode.Forbidden);
        upload.ErrorCode.Should().Be("auth.forbidden");
        deleteOwners.Status.Should().Be(HttpStatusCode.Forbidden);
        deleteOwners.ErrorCode.Should().Be("collaboration.attachment_editor_only");
        deleteOwn.Status.Should().Be(HttpStatusCode.Forbidden, "a Viewer cannot delete even an attachment they uploaded themselves");
        deleteOwn.ErrorCode.Should().Be("collaboration.attachment_editor_only");
        (await RowsAsync(t)).Should().Be(rows, "nothing was stored");
        (await RowsAsync(t, liveOnly: true)).Should().Be(rows, "nothing was deleted");
    }

    // ---------------------------------------------------------------- outsiders see exactly what a made-up id gets

    [Theory]
    [InlineData("item", "otherAccount")]
    [InlineData("item", "outsider")]
    [InlineData("item", "suspendedMember")]
    [InlineData("page", "otherAccount")]
    [InlineData("page", "outsider")]
    [InlineData("page", "suspendedMember")]
    public async Task APersonWithoutAccess_GetsANotFound_WhoseBodyEqualsTheBodyForAMadeUpId_OnEveryRoute(string target, string actor)
    {
        var t = Of(target);
        var who = Who(actor);
        var existing = await UploadAsync(_a.User, t);
        var madeUp = target == "item"
            ? new Target("item", Guid.NewGuid(), $"/api/v1/board-items/{Guid.NewGuid()}/attachments")
            : new Target("page", Guid.NewGuid(), $"/api/v1/pages/{Guid.NewGuid()}/attachments");
        var rows = await RowsAsync(t);

        var routes = new (string Name, Func<Target, Guid, Task<Reply>> Call)[]
        {
            ("list", (x, _) => ListAsync(who, x)),
            ("content", (x, id) => ContentAsync(who, x, id)),
            ("link", (x, _) => LinkAsync(who, x)),
            ("upload", (x, _) => UploadAsync(who, x)),
            ("delete", (x, id) => DeleteAsync(who, x, id)),
        };
        foreach (var (name, call) in routes)
        {
            var found = await call(t, existing.Id);
            var made = await call(madeUp, Guid.NewGuid());

            made.Status.Should().Be(HttpStatusCode.NotFound, $"{name}: a made-up id is NotFound");
            found.Status.Should().Be(HttpStatusCode.NotFound, $"{name}: {actor} cannot tell a hidden {target} from a missing one");
            Normalize(found.Body).Should().Be(Normalize(made.Body), $"{name}: the refusal does not reveal the {target}");
        }

        (await RowsAsync(t)).Should().Be(rows, "nothing was stored");
        (await IsDeletedAsync(existing.Id)).Should().BeFalse("nothing was deleted");
    }

    // ---------------------------------------------------------------- suspended or closed account, archived workspace

    [Theory]
    [InlineData("item", "Suspended")]
    [InlineData("item", "Closed")]
    [InlineData("page", "Suspended")]
    [InlineData("page", "Closed")]
    public async Task ASuspendedOrClosedAccount_RefusesEveryWrite_ButReadsStayOpen_AndLiftingItRestoresWrites(string target, string status)
    {
        var t = Of(target);
        var existing = await UploadAsync(_a.User, t);
        var rows = await RowsAsync(t);
        await SetAccountStatusAsync(status);

        foreach (var who in new[] { _a.User, _editor })
        {
            foreach (var write in new[]
            {
                await LinkAsync(who, t),
                await UploadAsync(who, t, "late.png"),
                await DeleteAsync(who, t, existing.Id),
            })
            {
                write.Status.Should().Be(HttpStatusCode.Forbidden, write.Body);
                write.ErrorCode.Should().Be("account.suspended");
            }

            (await ListAsync(who, t)).Status.Should().Be(HttpStatusCode.OK, "reads stay open while the Account is suspended");
            var content = await ContentAsync(who, t, existing.Id);
            content.Status.Should().Be(HttpStatusCode.OK);
            content.Bytes.Should().Equal(Png);
        }

        (await RowsAsync(t)).Should().Be(rows, "nothing was stored");
        (await IsDeletedAsync(existing.Id)).Should().BeFalse("nothing was deleted");

        await SetAccountStatusAsync("Active");

        (await LinkAsync(_a.User, t)).Status.Should().Be(HttpStatusCode.Created);
        (await DeleteAsync(_a.User, t, existing.Id)).Status.Should().Be(HttpStatusCode.NoContent);
    }

    [Theory]
    [InlineData("item")]
    [InlineData("page")]
    public async Task AnArchivedWorkspace_RefusesEveryWrite_ButReadsStayOpen_AndUnarchivingRestoresWrites(string target)
    {
        var t = Of(target);
        var existing = await UploadAsync(_a.User, t);
        var rows = await RowsAsync(t);
        await SetWorkspaceStatusAsync("Archived");

        foreach (var who in new[] { _a.User, _editor })
        {
            foreach (var write in new[]
            {
                await LinkAsync(who, t),
                await UploadAsync(who, t, "late.png"),
                await DeleteAsync(who, t, existing.Id),
            })
            {
                write.Status.Should().Be(HttpStatusCode.Forbidden, write.Body);
                write.ErrorCode.Should().Be("workspace.archived");
            }

            (await ListAsync(who, t)).Status.Should().Be(HttpStatusCode.OK);
            (await ContentAsync(who, t, existing.Id)).Status.Should().Be(HttpStatusCode.OK);
        }

        (await RowsAsync(t)).Should().Be(rows);
        (await IsDeletedAsync(existing.Id)).Should().BeFalse();

        await SetWorkspaceStatusAsync("Active");

        (await LinkAsync(_a.User, t)).Status.Should().Be(HttpStatusCode.Created);
    }

    // ---------------------------------------------------------------- idempotency, cap, concurrency

    [Theory]
    [InlineData("item")]
    [InlineData("page")]
    public async Task TheSameIdempotencyKey_OnUploadAndOnLink_StoresOneRow_AndAnotherUrlIs409(string target)
    {
        var t = Of(target);
        var uploadKey = Guid.NewGuid().ToString("D");
        var linkKey = Guid.NewGuid().ToString("D");

        var firstUpload = await UploadAsync(_a.User, t, "once.png", uploadKey);
        var secondUpload = await UploadAsync(_a.User, t, "once.png", uploadKey);
        var firstLink = await LinkAsync(_a.User, t, "once", "https://example.com/once", linkKey);
        var secondLink = await LinkAsync(_a.User, t, "once", "https://example.com/once", linkKey);
        var changedLink = await LinkAsync(_a.User, t, "once", "https://example.com/other", linkKey);

        firstUpload.Status.Should().Be(HttpStatusCode.Created, firstUpload.Body);
        secondUpload.Status.Should().Be(HttpStatusCode.Created);
        secondUpload.Replayed.Should().BeTrue();
        secondUpload.Id.Should().Be(firstUpload.Id);
        firstLink.Status.Should().Be(HttpStatusCode.Created, firstLink.Body);
        firstLink.Replayed.Should().BeFalse();
        secondLink.Status.Should().Be(HttpStatusCode.Created);
        secondLink.Replayed.Should().BeTrue();
        secondLink.Id.Should().Be(firstLink.Id);
        changedLink.Status.Should().Be(HttpStatusCode.Conflict);
        changedLink.ErrorCode.Should().Be("idempotency_payload_mismatch");
        (await RowsAsync(t)).Should().Be(2, "one file and one link, however often the same key was sent");
    }

    [Theory]
    [InlineData("item")]
    [InlineData("page")]
    public async Task ALinkWithoutAnIdempotencyKey_Is400_AndStoresNothing(string target)
    {
        var t = Of(target);

        var reply = await SendAsync(_a.User, HttpMethod.Post, t.Base, LinkBody("a", "https://example.com/a"), withKey: false);

        reply.Status.Should().Be(HttpStatusCode.BadRequest);
        reply.ErrorCode.Should().Be("validation.failed");
        (await RowsAsync(t)).Should().Be(0);
    }

    [Theory]
    [InlineData("item")]
    [InlineData("page")]
    public async Task DeleteFreesASlot_SoAnUploadAtTheCapSucceedsAgain(string target)
    {
        var t = Of(target);
        var ids = new List<Guid>();
        for (var i = 0; i < Cap; i++)
        {
            var created = i % 2 == 0 ? await UploadAsync(_a.User, t, $"f{i}.png") : await LinkAsync(_a.User, t, $"l{i}", $"https://example.com/{i}");
            created.Status.Should().Be(HttpStatusCode.Created, created.Body);
            ids.Add(created.Id);
        }

        var full = await UploadAsync(_a.User, t, "over.png");
        var fullLink = await LinkAsync(_a.User, t, "over", "https://example.com/over");
        full.Status.Should().Be(HttpStatusCode.Conflict);
        full.ErrorCode.Should().Be("attachment.limit_exceeded");
        fullLink.Status.Should().Be(HttpStatusCode.Conflict);
        fullLink.ErrorCode.Should().Be("attachment.limit_exceeded");

        (await DeleteAsync(_a.User, t, ids[0])).Status.Should().Be(HttpStatusCode.NoContent);

        (await UploadAsync(_a.User, t, "again.png")).Status.Should().Be(HttpStatusCode.Created);
        (await RowsAsync(t, liveOnly: true)).Should().Be(Cap);
    }

    [Theory]
    [InlineData("item")]
    [InlineData("page")]
    public async Task AConcurrentDoubleDelete_IsOneNoContentAndOneNotFound_NeverA500_AndOneVersionBump(string target)
    {
        var t = Of(target);

        for (var round = 0; round < 3; round++)
        {
            var created = await LinkAsync(_a.User, t, $"race{round}", $"https://example.com/race{round}");
            created.Status.Should().Be(HttpStatusCode.Created, created.Body);
            var before = await VersionAsync(created.Id);

            var replies = await Task.WhenAll(DeleteAsync(_a.User, t, created.Id), DeleteAsync(_editor, t, created.Id));

            replies.Select(r => r.Status).OrderBy(s => (int)s).Should().Equal(
                [HttpStatusCode.NoContent, HttpStatusCode.NotFound],
                $"round {round}: {string.Join(" / ", replies.Select(r => r.Status + " " + r.Body))}");
            (await VersionAsync(created.Id)).Should().Be(before + 1, "exactly one delete took effect");
            (await IsDeletedAsync(created.Id)).Should().BeTrue();
        }
    }
}
