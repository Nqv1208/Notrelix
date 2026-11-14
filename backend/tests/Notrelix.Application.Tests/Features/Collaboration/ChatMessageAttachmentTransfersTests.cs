using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Storage;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Collaboration.Attachments;
using Notrelix.Application.Features.Collaboration.Attachments.Services;
using Notrelix.Application.Features.Collaboration.Public.Attachments;
using Notrelix.Domain.Collaboration.Attachments;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Application.Tests.Features.Collaboration;

/// <summary>
/// The two-step (pre-signed) file transfer on a Chat message at boundary level: the create records a pending upload and signs a PUT, the
/// commit verifies the bytes a client wrote and applies the shared policy, and a download is signed after the authorization of the caller.
/// The object store is in memory; the policy and the row are the real Collaboration code.
/// </summary>
public class ChatMessageAttachmentTransfersTests
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D];
    private static readonly byte[] Executable = [0x4D, 0x5A, 0x90, 0x00, 1, 2, 3, 4];

    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _user = Guid.NewGuid();
    private readonly Guid _messageId = Guid.NewGuid();
    private readonly DateTimeOffset _start = new(2026, 11, 13, 9, 0, 0, TimeSpan.Zero);
    private DateTimeOffset _now;

    private readonly Mock<ICurrentRequestContext> _context = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly List<Attachment> _rows = [];
    private readonly List<PendingAttachmentUpload> _pending = [];
    private readonly Mock<ICollaborationDbContext> _db = new();
    private readonly MemoryObjectStorage _storage = new();
    private readonly RequestRollbackCompensations _compensations = new(NullLogger<RequestRollbackCompensations>.Instance);

    public ChatMessageAttachmentTransfersTests()
    {
        _now = _start;
        _clock.Setup(c => c.UtcNow).Returns(() => _now);
        _context.Setup(c => c.RequireAccountId()).Returns(_accountId);
        _context.Setup(c => c.RequireWorkspaceId()).Returns(_workspaceId);
        _context.Setup(c => c.UserId).Returns(_user);
        _context.Setup(c => c.IsAuthenticated).Returns(true);
        _db.Setup(d => d.Attachments).Returns(() => TestDbSet.Create(_rows).Object);
        _db.Setup(d => d.PendingAttachmentUploads).Returns(() => TestDbSet.Create(_pending).Object);
    }

    private ChatMessageAttachmentTransfers Transfers(AttachmentUploadOptions? policy = null, bool supported = true)
    {
        var presigned = new MemoryObjectStorage { Supported = supported, Objects = _storage.Objects, Clock = () => _now };
        return new ChatMessageAttachmentTransfers(
            _db.Object,
            _context.Object,
            _clock.Object,
            _storage,
            presigned,
            _compensations,
            new InMemoryAttachmentTargetLock(),
            Options.Create(policy ?? new AttachmentUploadOptions()),
            Options.Create(new PresignedUrlOptions()));
    }

    /// <summary>What the client does after the signed PUT: the bytes land at the key of the pending upload.</summary>
    private void ClientUploads(Guid uploadId, byte[] bytes) =>
        _storage.Objects[_pending.Single(p => p.Id == uploadId).StorageKey] = bytes;

    private async Task<Guid> CreateAsync(string name = "photo.png", long size = 12, string type = "image/png", ChatMessageAudioUpload? audio = null, AttachmentUploadOptions? policy = null)
    {
        var ticket = await Transfers(policy).CreateUploadAsync(_messageId, name, size, type, audio, CancellationToken.None);
        return ticket.UploadId;
    }

    // ---- Create ------------------------------------------------------------------------------

    [Fact]
    public async Task Create_signs_a_put_bound_to_the_declared_type_and_length_and_writes_no_attachment_row()
    {
        var ticket = await Transfers().CreateUploadAsync(_messageId, "photo.png", 12, "image/png", null, CancellationToken.None);

        ticket.Method.Should().Be("PUT");
        ticket.ContentType.Should().Be("image/png");
        ticket.ContentLength.Should().Be(12);
        ticket.Url.Should().StartWith("https://storage.test/put/");
        ticket.ExpiresAt.Should().BeCloseTo(_start.AddSeconds(300), TimeSpan.FromSeconds(1));
        _pending.Should().ContainSingle(p => p.Id == ticket.UploadId && p.ExpiresAt == _start.AddSeconds(900));
        _rows.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_refuses_a_blocked_extension_before_anything_is_recorded()
    {
        var act = () => Transfers().CreateUploadAsync(_messageId, "tool.exe", 12, "application/octet-stream", null, CancellationToken.None);

        await act.Should().ThrowAsync<UnsupportedMediaTypeException>();
        _pending.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_refuses_a_declared_size_over_the_cap_with_413()
    {
        var act = () => Transfers(new AttachmentUploadOptions { MaxFileSizeBytes = 1024 * 1024 })
            .CreateUploadAsync(_messageId, "big.png", 2 * 1024 * 1024, "image/png", null, CancellationToken.None);

        await act.Should().ThrowAsync<PayloadTooLargeException>();
        _pending.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_is_refused_when_the_provider_cannot_sign_urls()
    {
        var act = () => Transfers(supported: false).CreateUploadAsync(_messageId, "photo.png", 12, "image/png", null, CancellationToken.None);

        await act.Should().ThrowAsync<NotSupportedException>();
        _pending.Should().BeEmpty();
    }

    // ---- Commit ------------------------------------------------------------------------------

    [Fact]
    public async Task Commit_records_the_file_with_the_type_read_from_the_stored_bytes()
    {
        var uploadId = await CreateAsync();
        ClientUploads(uploadId, Png);

        var result = await Transfers().CommitUploadAsync(_messageId, uploadId, CancellationToken.None);

        result.Replayed.Should().BeFalse();
        result.IsVoice.Should().BeFalse();
        result.Attachment.ContentType.Should().Be("image/png");
        result.Attachment.SizeBytes.Should().Be(Png.Length);
        _rows.Should().ContainSingle(a => a.Id == result.Attachment.Id && a.Metadata.StorageKey == _pending[0].StorageKey);
        _pending[0].IsCommitted.Should().BeTrue();
    }

    [Fact]
    public async Task Commit_refuses_an_executable_and_deletes_the_object()
    {
        var uploadId = await CreateAsync("photo.png", size: Executable.Length);
        ClientUploads(uploadId, Executable);

        var act = () => Transfers().CommitUploadAsync(_messageId, uploadId, CancellationToken.None);

        await act.Should().ThrowAsync<UnsupportedMediaTypeException>();
        _storage.Objects.Should().NotContainKey(_pending[0].StorageKey);
        _rows.Should().BeEmpty();
        _pending[0].IsCommitted.Should().BeFalse();
    }

    [Fact]
    public async Task Commit_refuses_bytes_that_do_not_match_the_declared_size_and_deletes_the_object()
    {
        var uploadId = await CreateAsync(size: 999);
        ClientUploads(uploadId, Png);

        var act = () => Transfers().CommitUploadAsync(_messageId, uploadId, CancellationToken.None);

        var refusal = await act.Should().ThrowAsync<ConflictException>();
        refusal.Which.ErrorCode.Should().Be("attachment-upload-size-mismatch");
        _storage.Objects.Should().NotContainKey(_pending[0].StorageKey);
    }

    [Fact]
    public async Task Commit_is_idempotent_a_repeat_returns_the_same_file_and_adds_no_row()
    {
        var uploadId = await CreateAsync();
        ClientUploads(uploadId, Png);
        var transfers = Transfers();

        var first = await transfers.CommitUploadAsync(_messageId, uploadId, CancellationToken.None);
        var second = await transfers.CommitUploadAsync(_messageId, uploadId, CancellationToken.None);

        second.Replayed.Should().BeTrue();
        second.Attachment.Id.Should().Be(first.Attachment.Id);
        _rows.Should().ContainSingle();
    }

    [Fact]
    public async Task Commit_of_an_expired_upload_is_refused_with_its_own_code()
    {
        var uploadId = await CreateAsync();
        ClientUploads(uploadId, Png);
        _now = _start.AddSeconds(901);

        var act = () => Transfers().CommitUploadAsync(_messageId, uploadId, CancellationToken.None);

        var refusal = await act.Should().ThrowAsync<ConflictException>();
        refusal.Which.ErrorCode.Should().Be("attachment-upload-expired");
        _rows.Should().BeEmpty();
    }

    [Fact]
    public async Task Commit_on_another_message_is_not_found()
    {
        var uploadId = await CreateAsync();
        ClientUploads(uploadId, Png);

        var act = () => Transfers().CommitUploadAsync(Guid.NewGuid(), uploadId, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _rows.Should().BeEmpty();
    }

    [Fact]
    public async Task Commit_before_the_bytes_arrive_is_a_conflict_not_a_file()
    {
        var uploadId = await CreateAsync();

        var act = () => Transfers().CommitUploadAsync(_messageId, uploadId, CancellationToken.None);

        var refusal = await act.Should().ThrowAsync<ConflictException>();
        refusal.Which.ErrorCode.Should().Be("attachment-upload-missing");
        _rows.Should().BeEmpty();
    }

    // ---- Download ----------------------------------------------------------------------------

    [Fact]
    public async Task Download_url_is_signed_for_a_file_of_the_message()
    {
        var uploadId = await CreateAsync();
        ClientUploads(uploadId, Png);
        var committed = await Transfers().CommitUploadAsync(_messageId, uploadId, CancellationToken.None);

        var link = await Transfers().CreateDownloadUrlAsync(_messageId, committed.Attachment.Id, CancellationToken.None);

        link.Url.Should().Contain(_pending[0].StorageKey);
        link.ContentType.Should().Be("image/png");
        link.ExpiresAt.Should().Be(_start.AddMinutes(5));
    }

    [Fact]
    public async Task Download_url_for_a_file_of_another_message_is_not_found()
    {
        var uploadId = await CreateAsync();
        ClientUploads(uploadId, Png);
        var committed = await Transfers().CommitUploadAsync(_messageId, uploadId, CancellationToken.None);

        var act = () => Transfers().CreateDownloadUrlAsync(Guid.NewGuid(), committed.Attachment.Id, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>In-memory object store that also answers the pre-signed capability, with no network.</summary>
    private sealed class MemoryObjectStorage : IStorageService, IPresignedObjectStorage
    {
        public Dictionary<string, byte[]> Objects { get; set; } = new();
        public bool Supported { get; init; } = true;

        public bool SupportsPresignedUrls => Supported;

        public async Task WriteAsync(string key, Stream content, string contentType, CancellationToken cancellationToken)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            Objects[key] = buffer.ToArray();
        }

        public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken) =>
            Task.FromResult<Stream?>(Objects.TryGetValue(key, out var bytes) ? new MemoryStream(bytes) : null);

        public Task DeleteAsync(string key, CancellationToken cancellationToken)
        {
            Objects.Remove(key);
            return Task.CompletedTask;
        }

        public async IAsyncEnumerable<StorageObjectInfo> ListAsync(string prefix, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }

        public Func<DateTimeOffset> Clock { get; init; } = () => DateTimeOffset.UtcNow;

        public Task<PresignedObjectUpload> CreateUploadUrlAsync(string key, string contentType, long contentLength, TimeSpan expiresIn, CancellationToken cancellationToken) =>
            Task.FromResult(new PresignedObjectUpload($"https://storage.test/put/{key}", "PUT", contentType, contentLength, Clock().Add(expiresIn)));

        public Task<PresignedObjectDownload> CreateDownloadUrlAsync(string key, string fileName, string contentType, bool inline, TimeSpan expiresIn, CancellationToken cancellationToken) =>
            Task.FromResult(new PresignedObjectDownload($"https://storage.test/get/{key}", Clock().Add(expiresIn)));

        public Task<StorageObjectHead?> HeadAsync(string key, CancellationToken cancellationToken) =>
            Task.FromResult(Objects.TryGetValue(key, out var bytes) ? new StorageObjectHead(bytes.Length, null) : null);
    }
}
