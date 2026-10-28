using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Notrelix.API.Tests.Contracts;
using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Common.Requests.Security;
using Notrelix.Application.Common.Storage;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Collaboration.Attachments;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.API.Tests.Collaboration;

/// <summary>
/// HTTP contract of the Documents page attachments (<c>/api/v1/pages/{pageId}/attachments</c>)
/// through the real endpoint, CSRF, idempotency, authorization and handler pipeline. Resource
/// location and access facts are in-process fakes keyed by page id; RLS and the PostgreSQL
/// transaction belong to Notrelix.Integration.Tests.
/// </summary>
public sealed class PageAttachmentEndpointTests : IClassFixture<PageAttachmentEndpointTests.Fixture>
{
    private const string CsrfHeader = "X-CSRF-Token";
    private const int MaxPerPage = 2;

    private static readonly byte[] Png =
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4, 5, 6, 7, 8];

    private static readonly Guid TenantId = Guid.Parse("A0000000-0000-0000-0000-000000000001");
    private static readonly Guid OtherTenantId = Guid.Parse("B0000000-0000-0000-0000-000000000002");

    private readonly Fixture _fixture;

    public PageAttachmentEndpointTests(Fixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Editor_UploadsImage_ListsIt_AndReadsItInline()
    {
        var page = _fixture.NewPage(PageAccess.Editor);

        using var created = await Upload(page, "photo.png", Png);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = dto.GetProperty("id").GetGuid();
        dto.GetProperty("kind").GetString().Should().Be("File");
        dto.GetProperty("contentType").GetString().Should().Be("image/png");
        dto.GetProperty("resourceId").GetGuid().Should().Be(page);
        created.Headers.Location!.OriginalString.Should().Be($"/api/v1/pages/{page}/attachments/{id}/content");

        var list = await _fixture.Client.GetFromJsonAsync<JsonElement>($"/api/v1/pages/{page}/attachments");
        list.EnumerateArray().Select(a => a.GetProperty("id").GetGuid()).Should().Equal(id);

        using var content = await _fixture.Client.GetAsync(created.Headers.Location);
        content.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
        content.Content.Headers.ContentDisposition!.DispositionType.Should().Be("inline");
        content.Headers.GetValues("X-Content-Type-Options").Should().Equal("nosniff");
        content.Headers.CacheControl!.ToString().Should().Be("no-store, private");
        (await content.Content.ReadAsByteArrayAsync()).Should().Equal(Png);
    }

    [Fact]
    public async Task NonImageFile_IsServedAsAnAttachment()
    {
        var page = _fixture.NewPage(PageAccess.Editor);
        using var created = await Upload(page, "notes.txt", Encoding.UTF8.GetBytes("plain <script>alert(1)</script>"));
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        using var content = await _fixture.Client.GetAsync(created.Headers.Location);

        content.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
    }

    [Fact]
    public async Task BlockedExtension_Is415_AndStoresNothing()
    {
        var page = _fixture.NewPage(PageAccess.Editor);
        var before = _fixture.Storage.Objects.Count;

        using var response = await Upload(page, "run.exe", Png);

        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        _fixture.Storage.Objects.Count.Should().Be(before);
    }

    [Fact]
    public async Task BeyondThePageLimit_Is409_AndStoresNoExtraObject()
    {
        var page = _fixture.NewPage(PageAccess.Editor);
        for (var i = 0; i < MaxPerPage; i++)
        {
            (await Upload(page, $"f{i}.png", Png)).StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var before = _fixture.Storage.Objects.Count;
        using var response = await Upload(page, "extra.png", Png);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        _fixture.Storage.Objects.Count.Should().Be(before);
    }

    [Fact]
    public async Task Viewer_CanListAndRead_ButCannotUploadOrDelete()
    {
        var page = _fixture.NewPage(PageAccess.Editor);
        using var created = await Upload(page, "photo.png", Png);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        _fixture.SetAccess(page, PageAccess.Viewer);
        var before = _fixture.Storage.Objects.Count;

        using var list = await _fixture.Client.GetAsync($"/api/v1/pages/{page}/attachments");
        using var content = await _fixture.Client.GetAsync($"/api/v1/pages/{page}/attachments/{id}/content");
        using var upload = await Upload(page, "more.png", Png);
        using var link = await _fixture.SendAsync(HttpMethod.Post, $"/api/v1/pages/{page}/attachments",
            JsonContent.Create(new { filename = "site", url = "https://example.com", sizeBytes = 0, contentType = "text/html" }));
        using var delete = await _fixture.SendAsync(HttpMethod.Delete, $"/api/v1/pages/{page}/attachments/{id}", null);

        list.StatusCode.Should().Be(HttpStatusCode.OK);
        content.StatusCode.Should().Be(HttpStatusCode.OK);
        upload.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        link.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        delete.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _fixture.Storage.Objects.Count.Should().Be(before);
    }

    [Fact]
    public async Task UserWithoutAccess_CannotListReadUploadOrDelete()
    {
        var page = _fixture.NewPage(PageAccess.Editor);
        using var created = await Upload(page, "photo.png", Png);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        _fixture.SetAccess(page, PageAccess.None);

        using var list = await _fixture.Client.GetAsync($"/api/v1/pages/{page}/attachments");
        using var content = await _fixture.Client.GetAsync($"/api/v1/pages/{page}/attachments/{id}/content");
        using var upload = await Upload(page, "more.png", Png);
        using var delete = await _fixture.SendAsync(HttpMethod.Delete, $"/api/v1/pages/{page}/attachments/{id}", null);

        foreach (var response in new[] { list, content, upload, delete })
        {
            response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
            response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task PageOfAnotherWorkspace_IsHiddenAsNotFound()
    {
        var page = _fixture.NewPage(PageAccess.Editor, OtherTenantId);
        var before = _fixture.Storage.Objects.Count;

        using var list = await _fixture.Client.GetAsync($"/api/v1/pages/{page}/attachments");
        using var content = await _fixture.Client.GetAsync($"/api/v1/pages/{page}/attachments/{Guid.NewGuid()}/content");
        using var upload = await Upload(page, "a.png", Png);
        using var delete = await _fixture.SendAsync(HttpMethod.Delete, $"/api/v1/pages/{page}/attachments/{Guid.NewGuid()}", null);

        foreach (var response in new[] { list, content, upload, delete })
        {
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        _fixture.Storage.Objects.Count.Should().Be(before);
    }

    [Fact]
    public async Task UnknownPage_IsNotFound()
    {
        using var response = await _fixture.Client.GetAsync($"/api/v1/pages/{Fixture.UnknownPage}/attachments");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task BoardItemAttachment_IsNotReadableOrDeletableThroughAPageRoute()
    {
        var page = _fixture.NewPage(PageAccess.Editor);
        var item = Guid.NewGuid();
        using var created = await _fixture.SendAsync(HttpMethod.Post, $"/api/v1/board-items/{item}/attachments/files", MultipartPng("item.png"), idempotent: true);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var read = await _fixture.Client.GetAsync($"/api/v1/pages/{page}/attachments/{id}/content");
        using var delete = await _fixture.SendAsync(HttpMethod.Delete, $"/api/v1/pages/{page}/attachments/{id}", null);
        var list = await _fixture.Client.GetFromJsonAsync<JsonElement>($"/api/v1/pages/{page}/attachments");

        read.StatusCode.Should().Be(HttpStatusCode.NotFound);
        delete.StatusCode.Should().Be(HttpStatusCode.NotFound);
        list.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task PageAttachment_IsNotReadableOrDeletableThroughABoardItemRoute_NorAnotherPage()
    {
        var page = _fixture.NewPage(PageAccess.Editor);
        var otherPage = _fixture.NewPage(PageAccess.Editor);
        using var created = await Upload(page, "photo.png", Png);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var viaItem = await _fixture.Client.GetAsync($"/api/v1/board-items/{Guid.NewGuid()}/attachments/{id}/content");
        using var viaOtherPage = await _fixture.Client.GetAsync($"/api/v1/pages/{otherPage}/attachments/{id}/content");
        using var deleteViaItem = await _fixture.SendAsync(HttpMethod.Delete, $"/api/v1/board-items/{Guid.NewGuid()}/attachments/{id}", null);
        using var deleteViaOtherPage = await _fixture.SendAsync(HttpMethod.Delete, $"/api/v1/pages/{otherPage}/attachments/{id}", null);
        using var stillThere = await _fixture.Client.GetAsync($"/api/v1/pages/{page}/attachments/{id}/content");

        viaItem.StatusCode.Should().Be(HttpStatusCode.NotFound);
        viaOtherPage.StatusCode.Should().Be(HttpStatusCode.NotFound);
        deleteViaItem.StatusCode.Should().Be(HttpStatusCode.NotFound);
        deleteViaOtherPage.StatusCode.Should().Be(HttpStatusCode.NotFound);
        stillThere.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Editor_DeletesAnAttachment_AndItNoLongerListsOrReads()
    {
        var page = _fixture.NewPage(PageAccess.Editor);
        using var created = await Upload(page, "photo.png", Png);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var delete = await _fixture.SendAsync(HttpMethod.Delete, $"/api/v1/pages/{page}/attachments/{id}", null);
        var list = await _fixture.Client.GetFromJsonAsync<JsonElement>($"/api/v1/pages/{page}/attachments");
        using var read = await _fixture.Client.GetAsync($"/api/v1/pages/{page}/attachments/{id}/content");

        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);
        list.GetArrayLength().Should().Be(0);
        read.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task WithoutAuthentication_Is401()
    {
        using var client = _fixture.CreateClient(withAuth: false);

        using var response = await client.GetAsync($"/api/v1/pages/{Guid.NewGuid()}/attachments");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static MultipartFormDataContent MultipartPng(string name, byte[]? bytes = null) =>
        new() { { new ByteArrayContent(bytes ?? Png), "file", name } };

    private Task<HttpResponseMessage> Upload(Guid page, string name, byte[] bytes) =>
        _fixture.SendAsync(HttpMethod.Post, $"/api/v1/pages/{page}/attachments/files", MultipartPng(name, bytes), idempotent: true);

    public enum PageAccess
    {
        None,
        Viewer,
        Editor,
    }

    private sealed class NoopAttachmentTargetLock : IAttachmentTargetLock
    {
        public Task AcquireAsync(ResourceRef target, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeIdempotencyStore : IIdempotencyStore
    {
        private readonly Dictionary<(string Operation, string Scope, string KeyHash), (string RequestHash, string Result, string Contract)> _completed = new();

        public Task<IdempotencyBeginResult> BeginAsync(IdempotencyIdentity identity, CancellationToken cancellationToken)
        {
            var key = (identity.Operation, identity.Scope, identity.KeyHash);
            if (!_completed.TryGetValue(key, out var stored))
            {
                return Task.FromResult(new IdempotencyBeginResult(IdempotencyBeginStatus.Started, null, null));
            }

            return Task.FromResult(stored.RequestHash == identity.RequestHash
                ? new IdempotencyBeginResult(IdempotencyBeginStatus.Completed, stored.Result, stored.Contract)
                : new IdempotencyBeginResult(IdempotencyBeginStatus.PayloadMismatch, null, null));
        }

        public Task CompleteAsync(IdempotencyIdentity identity, string serializedResult, string resultContract, CancellationToken cancellationToken)
        {
            _completed[(identity.Operation, identity.Scope, identity.KeyHash)] = (identity.RequestHash, serializedResult, resultContract);
            return Task.CompletedTask;
        }
    }

    public sealed class Fixture : IDisposable
    {
        public static readonly Guid UnknownPage = Guid.Parse("00000000-0000-0000-0000-00000000dead");

        private readonly CsrfEnabledApiFactory _baseFactory = new();
        private readonly Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> _factory;
        private readonly Dictionary<Guid, (PageAccess Access, Guid Tenant)> _pages = new();
        private string? _csrfToken;
        private string? _csrfCookie;

        public AttachmentFileEndpointTests.MemoryStorage Storage { get; } = new();

        public HttpClient Client { get; }

        public Fixture()
        {
            var storage = Storage;
            var idempotency = new FakeIdempotencyStore();
            var pages = _pages;
            _factory = _baseFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IStorageService>();
                    services.AddSingleton<IStorageService>(storage);

                    services.RemoveAll<IAttachmentTargetLock>();
                    services.AddScoped<IAttachmentTargetLock, NoopAttachmentTargetLock>();

                    services.RemoveAll<IIdempotencyStore>();
                    services.AddSingleton<IIdempotencyStore>(idempotency);

                    services.RemoveAll<IResourceLocator>();
                    services.AddScoped<IResourceLocator>(_ => new FakeResourceLocator(pages));

                    services.RemoveAll<IAccessFactsProvider>();
                    services.AddSingleton<IAccessFactsProvider>(new FakeAccessFacts(pages));

                    services.RemoveAll<IOptions<AttachmentUploadOptions>>();
                    services.AddSingleton<IOptions<AttachmentUploadOptions>>(Microsoft.Extensions.Options.Options.Create(new AttachmentUploadOptions
                    {
                        MaxFileSizeBytes = 1024,
                        MaxAttachmentsPerPage = MaxPerPage,
                    }));
                });
            });
            Client = CreateClient();
        }

        public Guid NewPage(PageAccess access, Guid? tenant = null)
        {
            var id = Guid.NewGuid();
            lock (_pages)
            {
                _pages[id] = (access, tenant ?? TenantId);
            }

            return id;
        }

        public void SetAccess(Guid page, PageAccess access)
        {
            lock (_pages)
            {
                _pages[page] = (access, _pages[page].Tenant);
            }
        }

        public HttpClient CreateClient(bool withCsrf = true, bool withAuth = true)
        {
            var client = _factory.Server.CreateClient();
            if (withAuth)
            {
                client.DefaultRequestHeaders.Add("X-Test-Auth", "true");
            }

            if (withCsrf)
            {
                EnsureCsrf(client);
                client.DefaultRequestHeaders.Add(CsrfHeader, _csrfToken);
                client.DefaultRequestHeaders.Add("Cookie", _csrfCookie);
            }

            return client;
        }

        public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, HttpContent? content, bool idempotent = false)
        {
            using var request = new HttpRequestMessage(method, path) { Content = content };
            if (idempotent)
            {
                request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
            }

            return await Client.SendAsync(request);
        }

        private void EnsureCsrf(HttpClient client)
        {
            if (_csrfToken is not null)
            {
                return;
            }

            using var response = client.GetAsync("/api/v1/auth/csrf").GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            _csrfToken = response.Content.ReadFromJsonAsync<JsonElement>().GetAwaiter().GetResult()
                .GetProperty("token").GetString();
            var setCookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("csrf_token=", StringComparison.Ordinal));
            _csrfCookie = setCookie.Split(';', 2)[0];
        }

        public void Dispose()
        {
            Client.Dispose();
            _factory.Dispose();
            _baseFactory.Dispose();
        }
    }

    private sealed class FakeResourceLocator(Dictionary<Guid, (PageAccess Access, Guid Tenant)> pages) : IResourceLocator
    {
        public Task<ResourceLocation?> LocateAsync(ResourceRef resource, Guid actorUserId, CancellationToken cancellationToken)
        {
            if (resource.ResourceId == Fixture.UnknownPage)
            {
                return Task.FromResult<ResourceLocation?>(null);
            }

            var tenant = TenantId;
            lock (pages)
            {
                if (pages.TryGetValue(resource.ResourceId, out var page))
                {
                    tenant = page.Tenant;
                }
            }

            return Task.FromResult<ResourceLocation?>(new ResourceLocation(resource.Kind, resource.ResourceId, tenant, tenant));
        }
    }

    private sealed class FakeAccessFacts(Dictionary<Guid, (PageAccess Access, Guid Tenant)> pages) : IAccessFactsProvider
    {
        public Task<AccessFacts> ResolveAsync(
            RequestDescriptor descriptor, ExecutionContextSnapshot context, object request, CancellationToken cancellationToken)
        {
            var resource = (request as IRequirePermission)?.Resource;
            var access = PageAccess.Editor;
            if (resource is { } r && r.Kind.Value == "documents.page")
            {
                lock (pages)
                {
                    access = pages.TryGetValue(r.ResourceId, out var page) ? page.Access : PageAccess.None;
                }
            }

            var isPage = resource?.Kind.Value == "documents.page";
            int? rank = access switch
            {
                PageAccess.Viewer => 1,
                PageAccess.Editor => 4,
                _ => null,
            };

            return Task.FromResult(new AccessFacts(
                UserExists: true,
                EmailVerified: true,
                AccountExists: true,
                AccountMemberRole: "Member",
                WorkspaceExists: true,
                WorkspaceMemberRole: "Member",
                ResourceExists: true,
                ResourceAudience: "Private",
                ResourceMemberRole: null,
                HasExplicitResourcePermission: rank is not null,
                PermissionRules: [],
                SubscriptionRequirementSatisfied: true,
                FeatureEnabled: true,
                ActiveResourcePermissionRank: rank,
                TargetPermissionRank: null)
            {
                ParentBoardId = isPage ? null : Guid.Parse("00000000-0000-0000-0000-0000000000b0"),
                IsBoardChild = !isPage,
            });
        }
    }
}
