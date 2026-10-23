using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Notrelix.API.Tests.Contracts;
using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Common.Requests.Security;
using Notrelix.Application.Common.Storage;
using Notrelix.Application.Features.Collaboration.Attachments;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.API.Tests.Collaboration;

/// <summary>
/// HTTP contract of the item-attachment file upload and download (api-and-contracts.md
/// section 24A) through the real endpoint, CSRF, idempotency, authorization and handler
/// pipeline, with real multipart bodies. Object storage, the idempotency store, resource
/// location and access facts are replaced by in-process fakes; the transaction, RLS and
/// provider mechanics belong to Notrelix.Integration.Tests.
///
/// Limits of this host: under TestServer <c>IHttpMaxRequestBodySizeFeature</c> is absent, so the
/// Kestrel request-body limit cannot be exercised here. The 413 cases below therefore prove the
/// declared-length pre-check and the counting stream that enforces the cap while streaming.
/// </summary>
public sealed class AttachmentFileEndpointTests : IClassFixture<AttachmentFileEndpointTests.Fixture>
{
    private const string CsrfHeader = "X-CSRF-Token";
    private const long Cap = 1024;
    private const int MaxPerItem = 2;

    private static readonly byte[] Png =
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4, 5, 6, 7, 8];

    private static readonly Guid UnknownItem = Guid.Parse("00000000-0000-0000-0000-00000000dead");
    private static readonly Guid ViewerOnlyItem = Guid.Parse("00000000-0000-0000-0000-000000000b1e");

    private static readonly Regex StorageKeyShape =
        new("^a0000000000000000000000000000001/a0000000000000000000000000000001/attachments/[0-9a-f]{32}$");

    private readonly Fixture _fixture;

    public AttachmentFileEndpointTests(Fixture fixture) => _fixture = fixture;

    private MemoryStorage Storage => _fixture.Storage;

    // ---------------------------------------------------------------- upload: success

    [Fact]
    public async Task Upload_Succeeds_With201_Location_AndAServerOwnedStorageKey()
    {
        var item = Guid.NewGuid();
        var before = Storage.Objects.Count;

        // The part Content-Type is client-declared and must be ignored.
        using var response = await Upload(item, Form("photo.png", Png, partContentType: "text/html"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Contains("Idempotency-Replayed").Should().BeFalse();
        var dto = await response.Content.ReadFromJsonAsync<JsonElement>();
        dto.GetProperty("kind").GetString().Should().Be("File");
        dto.GetProperty("filename").GetString().Should().Be("photo.png");
        dto.GetProperty("contentType").GetString().Should().Be("image/png");
        dto.GetProperty("sizeBytes").GetInt64().Should().Be(Png.Length);
        dto.GetProperty("url").GetString().Should().BeEmpty();
        dto.GetProperty("resourceId").GetGuid().Should().Be(item);

        var id = dto.GetProperty("id").GetGuid();
        response.Headers.Location!.OriginalString.Should().Be($"/api/v1/board-items/{item}/attachments/{id}/content");

        Storage.Objects.Count.Should().Be(before + 1);
        Storage.Objects.Keys.Should().OnlyContain(k => StorageKeyShape.IsMatch(k));
    }

    [Fact]
    public async Task Upload_StreamedBodyWithoutContentLength_RoundTripsThroughDownload()
    {
        var item = Guid.NewGuid();
        var bytes = Png.Concat(Enumerable.Range(0, 600).Select(i => (byte)(i % 251))).ToArray();

        using var form = Form("chunked.png", bytes, nonSeekable: true);
        form.Headers.ContentLength.Should().BeNull("a non-seekable part makes the multipart body unbounded (chunked)");

        using var response = await Upload(item, form);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using var download = await _fixture.Client.GetAsync(response.Headers.Location);
        download.StatusCode.Should().Be(HttpStatusCode.OK);
        (await download.Content.ReadAsByteArrayAsync()).Should().Equal(bytes);
    }

    [Fact]
    public async Task Upload_ReplayWithSameKey_ReturnsTheSameBody_FlagsReplay_AndStoresNoSecondObject()
    {
        var item = Guid.NewGuid();
        var key = NewKey();

        using var first = await Upload(item, Form("once.png", Png), key);
        var afterFirst = Storage.Objects.Count;
        var writesAfterFirst = Storage.Writes;
        using var second = await Upload(item, Form("once.png", Png), key);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Created);
        second.Headers.GetValues("Idempotency-Replayed").Should().Equal("true");
        (await second.Content.ReadAsStringAsync()).Should().Be(await first.Content.ReadAsStringAsync());
        second.Headers.Location.Should().Be(first.Headers.Location);

        Storage.Objects.Count.Should().Be(afterFirst, "a replay must not store a second object");
        Storage.Writes.Should().Be(writesAfterFirst, "a replay must not even start a second write");
    }

    [Fact]
    public async Task Upload_SameKeyWithDifferentFileName_Is409IdempotencyPayloadMismatch()
    {
        var item = Guid.NewGuid();
        var key = NewKey();
        (await Upload(item, Form("a.png", Png), key)).StatusCode.Should().Be(HttpStatusCode.Created);

        using var response = await Upload(item, Form("b.png", Png), key);

        await ExpectProblem(response, HttpStatusCode.Conflict, "idempotency_payload_mismatch");
    }

    // ---------------------------------------------------------------- upload: filename handling

    [Theory]
    [InlineData("../../etc/passwd.png", "passwd.png")]
    [InlineData("..\\..\\windows\\evil.png", "evil.png")]
    [InlineData("C:\\temp\\report.png", "report.png")]
    [InlineData("we:ird*na?me.png", "we_ird_na_me.png")]
    public async Task Upload_SanitisesTheFileName_AndTheStorageKeyNeverCarriesIt(string sent, string expected)
    {
        var item = Guid.NewGuid();
        var before = Storage.Objects.Keys.ToHashSet();

        using var response = await Upload(item, Form(sent, Png));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<JsonElement>();
        dto.GetProperty("filename").GetString().Should().Be(expected);

        var added = Storage.Objects.Keys.Except(before).Single();
        StorageKeyShape.IsMatch(added).Should().BeTrue();
        added.Should().NotContain("passwd").And.NotContain("evil").And.NotContain("..").And.NotContain("\\");
    }

    // ---------------------------------------------------------------- upload: rejections

    [Fact]
    public async Task Upload_OverTheCap_WithKnownLength_Is413_AndStoresNothing()
    {
        var item = Guid.NewGuid();
        var before = Storage.Objects.Count;
        var body = Png.Concat(new byte[Cap + 100]).ToArray();

        using var response = await Upload(item, Form("big.png", body));

        await ExpectProblem(response, HttpStatusCode.RequestEntityTooLarge, "attachment.payload_too_large");
        Storage.Objects.Count.Should().Be(before, "the counting stream aborts the write; no object may remain");
    }

    [Fact]
    public async Task Upload_OverTheCap_WithUnknownLength_Is413_AndStoresNothing()
    {
        var item = Guid.NewGuid();
        var before = Storage.Objects.Count;
        var body = Png.Concat(new byte[Cap + 100]).ToArray();

        using var form = Form("big-chunked.png", body, nonSeekable: true);
        using var response = await Upload(item, form);

        await ExpectProblem(response, HttpStatusCode.RequestEntityTooLarge, "attachment.payload_too_large");
        Storage.Objects.Count.Should().Be(before);
    }

    [Fact]
    public async Task Upload_DeclaredLengthBeyondCapPlusFraming_IsRejectedEarlyWith413()
    {
        var item = Guid.NewGuid();
        var before = Storage.Objects.Count;
        var body = new byte[Cap + 1024 * 1024 + 4096];
        Array.Copy(Png, body, Png.Length);

        using var response = await Upload(item, Form("huge.png", body));

        await ExpectProblem(response, HttpStatusCode.RequestEntityTooLarge, "attachment.payload_too_large");
        Storage.Objects.Count.Should().Be(before);
    }

    [Fact]
    public async Task Upload_NotMultipart_Is415_AndStoresNothing()
    {
        var before = Storage.Objects.Count;

        using var response = await Upload(Guid.NewGuid(), JsonContent.Create(new { file = "x" }));

        // The routing layer rejects a request whose Content-Type is not the declared
        // multipart/form-data before the endpoint runs, so this 415 has no problem body.
        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        Storage.Objects.Count.Should().Be(before);
    }

    [Fact]
    public async Task Upload_MultipartWithoutABoundary_Is415WithTheStableErrorCode()
    {
        using var content = new ByteArrayContent(Png);
        content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("multipart/form-data");

        using var response = await Upload(Guid.NewGuid(), content);

        await ExpectProblem(response, HttpStatusCode.UnsupportedMediaType, "attachment.unsupported_media_type");
    }

    [Theory]
    [InlineData("setup.exe")]
    [InlineData("page.html")]
    [InlineData("script.js")]
    [InlineData("RUN.SH")]
    public async Task Upload_BlockedExtension_Is415_AndStoresNothing(string name)
    {
        var before = Storage.Objects.Count;

        using var response = await Upload(Guid.NewGuid(), Form(name, Png));

        await ExpectProblem(response, HttpStatusCode.UnsupportedMediaType, "attachment.unsupported_media_type");
        Storage.Objects.Count.Should().Be(before);
    }

    [Theory]
    [InlineData("pe")]
    [InlineData("elf")]
    [InlineData("shebang")]
    public async Task Upload_ExecutableSignature_Is415_EvenWithAnInnocentName(string kind)
    {
        var before = Storage.Objects.Count;
        byte[] bytes = kind switch
        {
            "pe" => [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00],
            "elf" => [0x7F, 0x45, 0x4C, 0x46, 0x02, 0x01],
            _ => Encoding.ASCII.GetBytes("#!/bin/sh\necho hi\n"),
        };

        using var response = await Upload(Guid.NewGuid(), Form("holiday.png", bytes));

        await ExpectProblem(response, HttpStatusCode.UnsupportedMediaType, "attachment.unsupported_media_type");
        Storage.Objects.Count.Should().Be(before);
    }

    [Fact]
    public async Task Upload_WithoutAFilePart_Is400()
    {
        using var form = new MultipartFormDataContent { { new StringContent("hello"), "note" } };

        using var response = await Upload(Guid.NewGuid(), form);

        await ExpectProblem(response, HttpStatusCode.BadRequest, "validation.failed");
    }

    [Fact]
    public async Task Upload_FilePartWithoutAFileName_Is400()
    {
        using var form = new MultipartFormDataContent { { new ByteArrayContent(Png), "file" } };

        using var response = await Upload(Guid.NewGuid(), form);

        await ExpectProblem(response, HttpStatusCode.BadRequest, "validation.failed");
    }

    [Fact]
    public async Task Upload_EmptyFile_Is400_AndStoresNothing()
    {
        var before = Storage.Objects.Count;

        using var response = await Upload(Guid.NewGuid(), Form("empty.png", []));

        await ExpectProblem(response, HttpStatusCode.BadRequest, "business_rule.violation");
        Storage.Objects.Count.Should().Be(before);
    }

    [Fact]
    public async Task Upload_MalformedMultipartBody_Is400()
    {
        using var content = new StringContent("--nope\r\nbroken", Encoding.ASCII);
        content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("multipart/form-data; boundary=zzz");

        using var response = await Upload(Guid.NewGuid(), content);

        await ExpectProblem(response, HttpStatusCode.BadRequest, "validation.failed");
    }

    [Fact]
    public async Task Upload_BodyTruncatedInsideTheFilePart_Is400_AndStoresNothing()
    {
        var before = Storage.Objects.Count;
        var head = Encoding.ASCII.GetBytes(
            "--b\r\nContent-Disposition: form-data; name=\"file\"; filename=\"cut.png\"\r\n\r\n");
        using var content = new ByteArrayContent(head.Concat(Png).ToArray()); // no closing boundary
        content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("multipart/form-data; boundary=b");

        using var response = await Upload(Guid.NewGuid(), content);

        await ExpectProblem(response, HttpStatusCode.BadRequest, "validation.failed");
        Storage.Objects.Count.Should().Be(before, "a truncated body must not leave a partial object");
    }

    [Fact]
    public async Task Upload_BeyondTheItemLimit_Is409LimitExceeded_AndStoresNoExtraObject()
    {
        var item = Guid.NewGuid();
        for (var i = 0; i < MaxPerItem; i++)
        {
            (await Upload(item, Form($"f{i}.png", Png))).StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var before = Storage.Objects.Count;
        using var response = await Upload(item, Form("one-too-many.png", Png));

        await ExpectProblem(response, HttpStatusCode.Conflict, "attachment.limit_exceeded");
        Storage.Objects.Count.Should().Be(before);
    }

    [Fact]
    public async Task Upload_ToAnUnknownItem_Is404_AndStoresNothing()
    {
        var before = Storage.Objects.Count;

        using var response = await Upload(UnknownItem, Form("a.png", Png));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        Storage.Objects.Count.Should().Be(before);
    }

    [Fact]
    public async Task Upload_ToAVisibleButNotEditableItem_Is403_AndStoresNothing()
    {
        var before = Storage.Objects.Count;

        using var response = await Upload(ViewerOnlyItem, Form("a.png", Png));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        Storage.Objects.Count.Should().Be(before);
    }

    [Fact]
    public async Task Upload_WithoutTheCsrfPair_Is403_AndStoresNothing()
    {
        var before = Storage.Objects.Count;
        using var client = _fixture.CreateClient(withCsrf: false);
        using var request = new HttpRequestMessage(HttpMethod.Post, UploadPath(Guid.NewGuid())) { Content = Form("a.png", Png) };
        request.Headers.Add("Idempotency-Key", NewKey());

        using var response = await client.SendAsync(request);

        await ExpectProblem(response, HttpStatusCode.Forbidden, "security.csrf_validation_failed");
        Storage.Objects.Count.Should().Be(before);
    }

    [Fact]
    public async Task Upload_WithoutAuthentication_Is401()
    {
        using var client = _fixture.CreateClient(withAuth: false);
        using var request = new HttpRequestMessage(HttpMethod.Post, UploadPath(Guid.NewGuid())) { Content = Form("a.png", Png) };
        request.Headers.Add("Idempotency-Key", NewKey());

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---------------------------------------------------------------- download

    [Fact]
    public async Task Download_OfAnImage_IsInline_WithTheHardeningHeaders()
    {
        var (item, id) = await UploadOk("pic.png", Png);

        using var response = await _fixture.Client.GetAsync(ContentPath(item, id));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
        response.Content.Headers.ContentDisposition!.DispositionType.Should().Be("inline");
        response.Content.Headers.ContentDisposition.FileName.Should().Be("pic.png");
        AssertHardeningHeaders(response);
        (await response.Content.ReadAsByteArrayAsync()).Should().Equal(Png);
    }

    [Fact]
    public async Task Download_OfANonImage_IsAnAttachment_WithTheHardeningHeaders()
    {
        var text = Encoding.UTF8.GetBytes("plain text <script>alert(1)</script>");
        var (item, id) = await UploadOk("notes.txt", text);

        using var response = await _fixture.Client.GetAsync(ContentPath(item, id));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
        response.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
        AssertHardeningHeaders(response);
    }

    [Fact]
    public async Task Download_OfAPdf_IsAnAttachment()
    {
        var (item, id) = await UploadOk("doc.pdf", Encoding.ASCII.GetBytes("%PDF-1.7\n1 0 obj\n"));

        using var response = await _fixture.Client.GetAsync(ContentPath(item, id));

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        response.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
    }

    [Fact]
    public async Task Download_OfALinkAttachment_Is404()
    {
        var item = Guid.NewGuid();
        using var created = await _fixture.SendAsync(HttpMethod.Post, $"/api/v1/board-items/{item}/attachments", NewKey(),
            JsonContent.Create(new { filename = "site", url = "https://example.com/x", sizeBytes = 0, contentType = "text/html", source = "Link" }));
        created.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var response = await _fixture.Client.GetAsync(ContentPath(item, id));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Download_ThroughAnotherItemsRoute_Is404()
    {
        var (_, id) = await UploadOk("mine.png", Png);

        using var response = await _fixture.Client.GetAsync(ContentPath(Guid.NewGuid(), id));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Download_OfAnUnknownAttachment_Is404()
    {
        using var response = await _fixture.Client.GetAsync(ContentPath(Guid.NewGuid(), Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Download_WhenTheStoredObjectIsMissing_Is404()
    {
        var before = Storage.Objects.Keys.ToHashSet();
        var (item, id) = await UploadOk("gone.png", Png);
        Storage.Objects.Remove(Storage.Objects.Keys.Except(before).Single());

        using var response = await _fixture.Client.GetAsync(ContentPath(item, id));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Download_FromAnUnknownItem_Is404()
    {
        using var response = await _fixture.Client.GetAsync(ContentPath(UnknownItem, Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Download_FromAViewerOnlyItem_IsAllowed_BecauseViewingNeedsNoEditRight()
    {
        // The viewer-only item holds no attachment, so the authorised read reaches the handler
        // and ends in 404 for the unknown attachment, not in 403.
        using var response = await _fixture.Client.GetAsync(ContentPath(ViewerOnlyItem, Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---------------------------------------------------------------- helpers

    private static void AssertHardeningHeaders(HttpResponseMessage response)
    {
        response.Headers.GetValues("X-Content-Type-Options").Should().Equal("nosniff");
        response.Headers.CacheControl!.ToString().Should().Be("no-store, private");
        response.Headers.GetValues("Content-Security-Policy").Should().Equal("default-src 'none'; sandbox");
    }

    private static string UploadPath(Guid item) => $"/api/v1/board-items/{item}/attachments/files";

    private static string ContentPath(Guid item, Guid id) => $"/api/v1/board-items/{item}/attachments/{id}/content";

    private static string NewKey() => Guid.NewGuid().ToString("N");

    private Task<HttpResponseMessage> Upload(Guid item, HttpContent content, string? key = null) =>
        _fixture.SendAsync(HttpMethod.Post, UploadPath(item), key ?? NewKey(), content);

    private async Task<(Guid Item, Guid Id)> UploadOk(string name, byte[] bytes)
    {
        var item = Guid.NewGuid();
        using var response = await Upload(item, Form(name, bytes));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        return (item, id);
    }

    private static async Task ExpectProblem(HttpResponseMessage response, HttpStatusCode status, string errorCode)
    {
        response.StatusCode.Should().Be(status);
        var body = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<JsonElement>(body);
        problem.GetProperty("errorCode").GetString().Should().Be(errorCode);
    }

    private static MultipartFormDataContent Form(
        string fileName, byte[] bytes, string? partContentType = null, bool nonSeekable = false)
    {
        HttpContent part = nonSeekable
            ? new StreamContent(new NonSeekableStream(bytes))
            : new ByteArrayContent(bytes);
        if (partContentType is not null)
        {
            part.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(partContentType);
        }

        return new MultipartFormDataContent { { part, "file", fileName } };
    }

    /// <summary>A forward-only stream, so the request body has no known length and is sent chunked.</summary>
    private sealed class NonSeekableStream(byte[] bytes) : Stream
    {
        private readonly MemoryStream _inner = new(bytes);
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    // ---------------------------------------------------------------- fixture and fakes

    public sealed class MemoryStorage : IStorageService
    {
        public Dictionary<string, byte[]> Objects { get; } = new();
        public int Writes { get; private set; }

        public async Task WriteAsync(string key, Stream content, string contentType, CancellationToken cancellationToken)
        {
            Writes++;
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken); // a failing stream leaves nothing behind
            Objects.Add(key, buffer.ToArray());
        }

        public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken) =>
            Task.FromResult<Stream?>(Objects.TryGetValue(key, out var bytes) ? new MemoryStream(bytes) : null);

        public Task DeleteAsync(string key, CancellationToken cancellationToken)
        {
            Objects.Remove(key);
            return Task.CompletedTask;
        }

        public async IAsyncEnumerable<StorageObjectInfo> ListAsync(
            string prefix, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            foreach (var key in Objects.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToArray())
            {
                yield return new StorageObjectInfo(key, DateTimeOffset.UtcNow);
            }
        }
    }

    /// <summary>Behaves like the real store from the caller's view: a key completes once, a failed run leaves it free.</summary>
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

    private sealed class NoopAttachmentTargetLock : Notrelix.Application.Features.Collaboration.Abstractions.IAttachmentTargetLock
    {
        public Task AcquireAsync(ResourceRef target, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeResourceLocator : IResourceLocator
    {
        public Task<ResourceLocation?> LocateAsync(ResourceRef resource, Guid actorUserId, CancellationToken cancellationToken) =>
            Task.FromResult<ResourceLocation?>(resource.ResourceId == UnknownItem
                ? null
                : new ResourceLocation(resource.Kind, resource.ResourceId, TenantId, TenantId));
    }

    private sealed class FakeAccessFacts : IAccessFactsProvider
    {
        public Task<AccessFacts> ResolveAsync(
            RequestDescriptor descriptor, ExecutionContextSnapshot context, object request, CancellationToken cancellationToken)
        {
            var itemId = (request as IRequirePermission)?.Resource?.ResourceId;
            var rank = itemId == ViewerOnlyItem ? 1 : 4;
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
                HasExplicitResourcePermission: true,
                PermissionRules: [],
                SubscriptionRequirementSatisfied: true,
                FeatureEnabled: true,
                ActiveResourcePermissionRank: rank,
                TargetPermissionRank: null)
            {
                ParentBoardId = Guid.Parse("00000000-0000-0000-0000-0000000000b0"),
                IsBoardChild = true,
            });
        }
    }

    private static readonly Guid TenantId = Guid.Parse("A0000000-0000-0000-0000-000000000001");

    public sealed class Fixture : IDisposable
    {
        private readonly CsrfEnabledApiFactory _baseFactory = new();
        private readonly WebApplicationFactoryHolder _holder;
        private string? _csrfToken;
        private string? _csrfCookie;

        public MemoryStorage Storage { get; } = new();

        public HttpClient Client { get; }

        public Fixture()
        {
            var storage = Storage;
            var idempotency = new FakeIdempotencyStore();
            var factory = _baseFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IStorageService>();
                    services.AddSingleton<IStorageService>(storage);

                    // The commit-point cap lock needs a PostgreSQL transaction; the serialisation itself is
                    // proven against real PostgreSQL in the integration tests, so this host uses a no-op lock.
                    services.RemoveAll<Notrelix.Application.Features.Collaboration.Abstractions.IAttachmentTargetLock>();
                    services.AddScoped<Notrelix.Application.Features.Collaboration.Abstractions.IAttachmentTargetLock, NoopAttachmentTargetLock>();

                    services.RemoveAll<IIdempotencyStore>();
                    services.AddSingleton<IIdempotencyStore>(idempotency);

                    services.RemoveAll<IResourceLocator>();
                    services.AddScoped<IResourceLocator, FakeResourceLocator>();

                    services.RemoveAll<IAccessFactsProvider>();
                    services.AddSingleton<IAccessFactsProvider, FakeAccessFacts>();

                    // A small configured cap and per-item limit; the options are replaced as a whole
                    // because their members are init-only.
                    services.RemoveAll<IOptions<AttachmentUploadOptions>>();
                    services.AddSingleton<IOptions<AttachmentUploadOptions>>(Microsoft.Extensions.Options.Options.Create(new AttachmentUploadOptions
                    {
                        MaxFileSizeBytes = Cap,
                        MaxAttachmentsPerItem = MaxPerItem,
                    }));
                });
            });
            _holder = new WebApplicationFactoryHolder(factory);
            Client = CreateClient();
        }

        public HttpClient CreateClient(bool withCsrf = true, bool withAuth = true)
        {
            var client = _holder.Factory.Server.CreateClient();
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

        public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string idempotencyKey, HttpContent content)
        {
            using var request = new HttpRequestMessage(method, path) { Content = content };
            request.Headers.Add("Idempotency-Key", idempotencyKey);
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
            _holder.Factory.Dispose();
            _baseFactory.Dispose();
        }

        private sealed record WebApplicationFactoryHolder(Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> Factory);
    }
}
