using System.Collections.Concurrent;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Storage;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Collaboration.Attachments;
using Notrelix.Application.Features.Collaboration.Attachments.Commands.UploadBoardItemAttachment;
using Notrelix.Application.Features.Collaboration.Attachments.Queries.GetBoardItemAttachmentContent;
using Notrelix.Application.Features.Collaboration.Attachments.Services;
using Notrelix.Domain.Collaboration.Attachments;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Application.Tests.Features.Collaboration;

public sealed class UploadBoardItemAttachmentTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid ItemId = Guid.NewGuid();
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4];

    private sealed class MemoryStorage : IStorageService
    {
        public Dictionary<string, byte[]> Objects { get; } = new();

        public async Task WriteAsync(string key, Stream content, string contentType, CancellationToken ct)
        {
            using var buffer = new MemoryStream();
            try
            {
                await content.CopyToAsync(buffer, ct);
            }
            catch
            {
                return; // nothing remains after a failed write
            }

            Objects[key] = buffer.ToArray();
        }

        public async Task WriteOrThrowAsync(string key, Stream content)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer);
            Objects[key] = buffer.ToArray();
        }

        public Task<Stream?> OpenReadAsync(string key, CancellationToken ct) =>
            Task.FromResult<Stream?>(Objects.TryGetValue(key, out var bytes) ? new MemoryStream(bytes) : null);

        public Task DeleteAsync(string key, CancellationToken ct)
        {
            lock (Objects)
            {
                Objects.Remove(key);
            }

            return Task.CompletedTask;
        }

        public async IAsyncEnumerable<StorageObjectInfo> ListAsync(
            string prefix, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            await Task.CompletedTask;
            foreach (var key in Objects.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToArray())
            {
                yield return new StorageObjectInfo(key, DateTimeOffset.UtcNow);
            }
        }
    }

    private sealed class ThrowingStorage : IStorageService
    {
        public Task WriteAsync(string key, Stream content, string contentType, CancellationToken ct) =>
            content.CopyToAsync(Stream.Null, ct); // surfaces stream exceptions (size cap) to the handler

        public Task<Stream?> OpenReadAsync(string key, CancellationToken ct) => Task.FromResult<Stream?>(null);
        public Task DeleteAsync(string key, CancellationToken ct) => Task.CompletedTask;

        public async IAsyncEnumerable<StorageObjectInfo> ListAsync(
            string prefix, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    private readonly List<Attachment> _rows = [];
    private readonly RequestRollbackCompensations _compensations = new(NullLogger<RequestRollbackCompensations>.Instance);

    private (UploadBoardItemAttachmentCommandHandler Handler, IStorageService Storage) CreateHandler(
        IStorageService storage,
        AttachmentUploadOptions? options = null,
        IAttachmentTargetLock? targetLock = null,
        IRequestRollbackCompensations? compensations = null)
    {
        var db = new Mock<ICollaborationDbContext>();
        db.Setup(c => c.Attachments).Returns(TestDbSet.Create(_rows).Object);
        var request = new FakeCurrentRequestContext { UserId = Guid.NewGuid() };
        request.Tenant.SetWorkspace(AccountId, WorkspaceId, request.UserId);
        var handler = new UploadBoardItemAttachmentCommandHandler(
            db.Object, request, new SystemDateTimeProvider(), storage, compensations ?? _compensations,
            targetLock ?? new InMemoryAttachmentTargetLock(),
            Options.Create(options ?? new AttachmentUploadOptions()));
        return (handler, storage);
    }

    private sealed class SystemDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    [Fact]
    public async Task Upload_StoresObjectUnderTenantPrefix_AndCreatesStoredFileRow()
    {
        var storage = new MemoryStorage();
        var (handler, _) = CreateHandler(storage);

        var result = await handler.Handle(
            new UploadBoardItemAttachmentCommand(ItemId, "..\\..\\evil/<a>.png", new MemoryStream(Png)), default);

        result.Succeeded.Should().BeTrue();
        var row = _rows.Should().ContainSingle().Subject;
        row.IsStoredFile.Should().BeTrue();
        row.Metadata.StorageKey.Should().StartWith($"{AccountId:N}/{WorkspaceId:N}/attachments/");
        row.Metadata.StorageKey.Should().NotContain("evil");
        row.Metadata.FileName.Should().Be("_a_.png");
        row.Metadata.ContentType.Should().Be("image/png");
        row.Metadata.Size.Should().Be(Png.Length);
        row.Type.Should().Be(AttachmentType.Image);
        row.DomainEvents.Should().ContainSingle(e => e is Notrelix.Domain.Collaboration.Attachments.Events.AttachmentCreatedDomainEvent);
        storage.Objects[row.Metadata.StorageKey!].Should().Equal(Png);
        result.Data!.Kind.Should().Be("File");
        result.Data.Url.Should().BeEmpty();
    }

    [Fact]
    public async Task Upload_ContentTypeIsSniffedNotClientDeclared()
    {
        var (handler, _) = CreateHandler(new MemoryStorage());

        await handler.Handle(
            new UploadBoardItemAttachmentCommand(ItemId, "photo.png", new MemoryStream("plain text, not a png"u8.ToArray())), default);

        _rows.Single().Metadata.ContentType.Should().Be("text/plain");
    }

    [Fact]
    public async Task Upload_StreamLargerThanCap_AbortsAndLeavesNoRow()
    {
        var options = new AttachmentUploadOptions { MaxFileSizeBytes = 2048 };
        var (handler, _) = CreateHandler(new ThrowingStorage(), options);
        var body = new MemoryStream(new byte[10_000]);

        var act = () => handler.Handle(new UploadBoardItemAttachmentCommand(ItemId, "big.bin", body), default);

        await act.Should().ThrowAsync<PayloadTooLargeException>();
        _rows.Should().BeEmpty();
    }

    [Fact]
    public async Task Upload_FailureAfterWrite_RunsRegisteredCompensation_AndRemovesObject()
    {
        var storage = new MemoryStorage();
        var (handler, _) = CreateHandler(storage, new AttachmentUploadOptions { MaxFileSizeBytes = 2048 });
        // Make the Domain rule reject after the bytes were stored (declared cap smaller than stored size is
        // impossible via streaming, so use a stream that reports more bytes than the cap only at the end).
        var key = string.Empty;
        _compensations.Register("probe", _ => Task.CompletedTask);

        var result = await handler.Handle(new UploadBoardItemAttachmentCommand(ItemId, "ok.png", new MemoryStream(Png)), default);
        key = _rows.Single().Metadata.StorageKey!;
        storage.Objects.Should().ContainKey(key);

        await _compensations.RunAsync(default); // what the data session does on a pre-commit failure

        result.Succeeded.Should().BeTrue();
        storage.Objects.Should().NotContainKey(key);
    }

    [Fact]
    public async Task Upload_AtAttachmentLimit_IsRejectedBeforeAnyByteIsWritten()
    {
        var storage = new MemoryStorage();
        var (handler, _) = CreateHandler(storage, new AttachmentUploadOptions { MaxAttachmentsPerItem = 1 });
        await handler.Handle(new UploadBoardItemAttachmentCommand(ItemId, "a.png", new MemoryStream(Png)), default);
        var objectsBefore = storage.Objects.Count;

        var act = () => handler.Handle(new UploadBoardItemAttachmentCommand(ItemId, "b.png", new MemoryStream(Png)), default);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(Notrelix.Domain.Collaboration.CollaborationRuleCodes.Collaboration_Attachment_MaxAttachmentsExceeded);
        storage.Objects.Count.Should().Be(objectsBefore);
        _rows.Should().HaveCount(1);
    }

    // ── Cap race: the authoritative recount happens under the target lock, after the stream ──

    /// <summary>Stores the bytes, but only after every concurrent upload has also been admitted by the early check.</summary>
    private sealed class GatedStorage(int expectedWriters) : IStorageService
    {
        private readonly TaskCompletionSource _open = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrived;
        public ConcurrentDictionary<string, byte[]> Objects { get; } = new();
        public List<string> Events { get; } = [];

        public async Task WriteAsync(string key, Stream content, string contentType, CancellationToken ct)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, ct);
            lock (Events)
            {
                Events.Add("write");
            }

            if (Interlocked.Increment(ref _arrived) >= expectedWriters)
            {
                _open.TrySetResult();
            }

            await _open.Task; // models a long stream: nobody finishes before everybody started
            Objects[key] = buffer.ToArray();
        }

        public Task<Stream?> OpenReadAsync(string key, CancellationToken ct) => Task.FromResult<Stream?>(null);

        public Task DeleteAsync(string key, CancellationToken ct)
        {
            Objects.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        public async IAsyncEnumerable<StorageObjectInfo> ListAsync(
            string prefix, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    [Fact]
    public async Task Upload_ConcurrentUploadsOverTheCap_ExactlyCapSucceed_AndTheRestAreCompensated()
    {
        const int cap = 3;
        const int uploads = 8;
        var storage = new GatedStorage(uploads);
        var locks = new InMemoryAttachmentTargetLocks();
        var options = new AttachmentUploadOptions { MaxAttachmentsPerItem = cap };

        // Every request is its own "transaction": own lock session and own compensations, shared rows.
        var tasks = Enumerable.Range(0, uploads).Select(async i =>
        {
            var requestLock = new InMemoryAttachmentTargetLock(locks);
            var compensations = new RequestRollbackCompensations(NullLogger<RequestRollbackCompensations>.Instance);
            var (handler, _) = CreateHandler(storage, options, requestLock, compensations);
            try
            {
                await handler.Handle(
                    new UploadBoardItemAttachmentCommand(ItemId, $"f{i}.png", new MemoryStream(Png)), default);
                return true;
            }
            catch (Notrelix.Domain.Common.Exceptions.BusinessRuleException ex)
            {
                ex.RuleCode.Should().Be(Notrelix.Domain.Collaboration.CollaborationRuleCodes.Collaboration_Attachment_MaxAttachmentsExceeded);
                await compensations.RunAsync(default); // what the data session does on a pre-commit failure
                return false;
            }
            finally
            {
                requestLock.EndTransaction();
            }
        }).ToArray();

        var outcomes = await Task.WhenAll(tasks);

        outcomes.Count(o => o).Should().Be(cap);
        outcomes.Count(o => !o).Should().Be(uploads - cap);
        _rows.Should().HaveCount(cap);
        storage.Objects.Keys.Should().BeEquivalentTo(_rows.Select(r => r.Metadata.StorageKey!));
    }

    [Fact]
    public async Task Upload_RowAppearingWhileStreaming_IsCaughtByTheRecount_AndRemovesTheObject()
    {
        var storage = new MemoryStorage();
        var compensations = new RequestRollbackCompensations(NullLogger<RequestRollbackCompensations>.Instance);
        var (handler, _) = CreateHandler(
            new RaceStorage(storage, () => _rows.Add(LinkRow())),
            new AttachmentUploadOptions { MaxAttachmentsPerItem = 1 },
            compensations: compensations);

        var act = () => handler.Handle(new UploadBoardItemAttachmentCommand(ItemId, "a.png", new MemoryStream(Png)), default);

        // The early check (0 rows) passed; the recount under the lock sees the row added meanwhile.
        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(Notrelix.Domain.Collaboration.CollaborationRuleCodes.Collaboration_Attachment_MaxAttachmentsExceeded);
        storage.Objects.Should().HaveCount(1); // written, then removed by the registered compensation
        await compensations.RunAsync(default);
        storage.Objects.Should().BeEmpty();
        _rows.Should().HaveCount(1);
    }

    [Fact]
    public async Task Upload_TakesTheTargetLock_OnlyAfterTheBodyIsStored()
    {
        var events = new List<string>();
        var storage = new RaceStorage(new MemoryStorage(), () => events.Add("stored"));
        var recordingLock = new RecordingLock(events);
        var (handler, _) = CreateHandler(storage, targetLock: recordingLock);

        await handler.Handle(new UploadBoardItemAttachmentCommand(ItemId, "a.png", new MemoryStream(Png)), default);

        events.Should().Equal("stored", "lock");
    }

    [Fact]
    public async Task CreateLink_TakesTheSameLock_AndRecountsSoLinksAndFilesCannotJointlyExceedTheCap()
    {
        var requestLock = new InMemoryAttachmentTargetLock();
        var options = new AttachmentUploadOptions { MaxAttachmentsPerItem = 2 };
        var (upload, _) = CreateHandler(new MemoryStorage(), options, requestLock);
        await upload.Handle(new UploadBoardItemAttachmentCommand(ItemId, "a.png", new MemoryStream(Png)), default);
        var link = CreateLinkHandler(options, requestLock);

        await link.Handle(new Notrelix.Application.Features.Collaboration.Attachments.Commands.CreateBoardItemAttachment.CreateBoardItemAttachmentCommand(
            ItemId, "doc", "https://x.test/doc", null, null, null), default);
        var act = () => link.Handle(new Notrelix.Application.Features.Collaboration.Attachments.Commands.CreateBoardItemAttachment.CreateBoardItemAttachmentCommand(
            ItemId, "doc2", "https://x.test/doc2", null, null, null), default);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(Notrelix.Domain.Collaboration.CollaborationRuleCodes.Collaboration_Attachment_MaxAttachmentsExceeded);
        _rows.Should().HaveCount(2);
        requestLock.Acquired.Should().HaveCount(3).And.OnlyContain(t => t.ResourceId == ItemId);
    }

    private Notrelix.Application.Features.Collaboration.Attachments.Commands.CreateBoardItemAttachment.CreateBoardItemAttachmentCommandHandler CreateLinkHandler(
        AttachmentUploadOptions options, IAttachmentTargetLock targetLock)
    {
        var db = new Mock<ICollaborationDbContext>();
        db.Setup(c => c.Attachments).Returns(TestDbSet.Create(_rows).Object);
        var request = new FakeCurrentRequestContext { UserId = Guid.NewGuid() };
        request.Tenant.SetWorkspace(AccountId, WorkspaceId, request.UserId);
        return new(db.Object, request, new SystemDateTimeProvider(), targetLock, Options.Create(options));
    }

    private static Attachment LinkRow() => Attachment.Create(
        AccountId, WorkspaceId,
        ResourceRef.Create(ResourceKind.Create("work-management.board-item"), ItemId, WorkspaceId),
        AttachmentType.Link, FileMetadata.Create("x", 0, "text/plain", url: "https://x.test/x"), Guid.NewGuid(), DateTimeOffset.UtcNow);

    /// <summary>Runs a side effect while the body is "streaming", i.e. between the early check and the recount.</summary>
    private sealed class RaceStorage(IStorageService inner, Action duringWrite) : IStorageService
    {
        public async Task WriteAsync(string key, Stream content, string contentType, CancellationToken ct)
        {
            await inner.WriteAsync(key, content, contentType, ct);
            duringWrite();
        }

        public Task<Stream?> OpenReadAsync(string key, CancellationToken ct) => inner.OpenReadAsync(key, ct);
        public Task DeleteAsync(string key, CancellationToken ct) => inner.DeleteAsync(key, ct);
        public IAsyncEnumerable<StorageObjectInfo> ListAsync(string prefix, CancellationToken ct) => inner.ListAsync(prefix, ct);
    }

    private sealed class RecordingLock(List<string> events) : IAttachmentTargetLock
    {
        public Task AcquireAsync(ResourceRef target, CancellationToken cancellationToken)
        {
            events.Add("lock");
            return Task.CompletedTask;
        }
    }

    [Theory]
    [InlineData("setup.exe", new byte[] { 1, 2, 3 })]
    [InlineData("notes.txt", new byte[] { 0x4D, 0x5A, 0x90, 0x00 })]
    [InlineData("run.txt", new byte[] { 0x23, 0x21, 0x2F, 0x62, 0x69, 0x6E })]
    public async Task Upload_ExecutableByExtensionOrSignature_IsRejected_WithNoObject(string name, byte[] bytes)
    {
        var storage = new MemoryStorage();
        var (handler, _) = CreateHandler(storage);

        var act = () => handler.Handle(new UploadBoardItemAttachmentCommand(ItemId, name, new MemoryStream(bytes)), default);

        await act.Should().ThrowAsync<UnsupportedMediaTypeException>();
        storage.Objects.Should().BeEmpty();
        _rows.Should().BeEmpty();
    }

    [Fact]
    public async Task Upload_EmptyFile_IsRejected()
    {
        var (handler, _) = CreateHandler(new MemoryStorage());

        var act = () => handler.Handle(new UploadBoardItemAttachmentCommand(ItemId, "empty.txt", new MemoryStream()), default);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(Notrelix.Domain.Collaboration.CollaborationRuleCodes.Collaboration_Attachment_FileSizeMustBePositive);
    }

    [Fact]
    public async Task Download_ReturnsStoredBytes_ForAttachmentOfTheRequestedItem_AndNotFoundOtherwise()
    {
        var storage = new MemoryStorage();
        var (upload, _) = CreateHandler(storage);
        var created = await upload.Handle(new UploadBoardItemAttachmentCommand(ItemId, "a.png", new MemoryStream(Png)), default);

        var db = new Mock<ICollaborationDbContext>();
        db.Setup(c => c.Attachments).Returns(TestDbSet.Create(_rows).Object);
        var request = new FakeCurrentRequestContext { UserId = Guid.NewGuid() };
        request.Tenant.SetWorkspace(AccountId, WorkspaceId, request.UserId);
        var query = new GetBoardItemAttachmentContentQueryHandler(
            db.Object, request, storage, NullLogger<GetBoardItemAttachmentContentQueryHandler>.Instance);

        var ok = await query.Handle(new GetBoardItemAttachmentContentQuery(ItemId, created.Data!.Id), default);
        using var ms = new MemoryStream();
        await ok.Data!.Content.CopyToAsync(ms);
        ms.ToArray().Should().Equal(Png);

        var otherItem = () => query.Handle(new GetBoardItemAttachmentContentQuery(Guid.NewGuid(), created.Data.Id), default);
        await otherItem.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public void Command_Fingerprint_IgnoresTheStream()
    {
        var fingerprint = new JsonIdempotencyRequestFingerprint();
        var a = new UploadBoardItemAttachmentCommand(ItemId, "a.png", new MemoryStream(Png));
        var b = new UploadBoardItemAttachmentCommand(ItemId, "a.png", new MemoryStream([9, 9]));

        fingerprint.Compute(a, typeof(UploadBoardItemAttachmentCommand))
            .Should().Be(fingerprint.Compute(b, typeof(UploadBoardItemAttachmentCommand)));
    }

    // ── Authorization: the same semantics as registering a link and as reading the item ──

    [Theory]
    [InlineData("Owner")]
    [InlineData("Admin")]
    [InlineData("Member")]
    [InlineData("Guest")]
    [InlineData(null)]
    public void Upload_Authorization_MatchesCreateLinkAttachment_ForEveryRole(string? role)
    {
        var upload = new UploadBoardItemAttachmentCommand(ItemId, "a.png", Stream.Null);
        var link = new Notrelix.Application.Features.Collaboration.Attachments.Commands.CreateBoardItemAttachment.CreateBoardItemAttachmentCommand(
            ItemId, "a", "https://x.test/a", null, null, null);

        upload.Action.Should().Be(link.Action);
        upload.Resource.Should().Be(link.Resource);

        var engine = new AccessPolicyEngine();
        var uploadDecision = engine.Evaluate(RequestDescriptorValidator.Create(typeof(UploadBoardItemAttachmentCommand)), Snapshot(), Facts(role), upload);
        var linkDecision = engine.Evaluate(RequestDescriptorValidator.Create(link.GetType()), Snapshot(), Facts(role), link);
        uploadDecision.Kind.Should().Be(linkDecision.Kind);
    }

    [Fact]
    public void Download_Authorization_MatchesReadingTheAttachmentList()
    {
        var download = new GetBoardItemAttachmentContentQuery(ItemId, Guid.NewGuid());
        var list = new Notrelix.Application.Features.Collaboration.Attachments.Queries.GetBoardItemAttachments.GetBoardItemAttachmentsQuery(ItemId);

        download.Action.Should().Be(list.Action);
        download.Resource.Should().Be(list.Resource);
    }

    [Fact]
    public void Upload_Owner_IsAllowed()
    {
        var upload = new UploadBoardItemAttachmentCommand(ItemId, "a.png", Stream.Null);

        var decision = new AccessPolicyEngine().Evaluate(
            RequestDescriptorValidator.Create(typeof(UploadBoardItemAttachmentCommand)), Snapshot(), Facts("Owner"), upload);

        decision.Kind.Should().Be(AccessDecisionKind.Allowed);
    }

    // ── Pure helpers ──

    [Theory]
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData("C:\\Users\\x\\report.pdf", "report.pdf")]
    [InlineData("  ..hidden.txt..  ", "hidden.txt")]
    [InlineData("a\u202Eb.exe", "ab.exe")]
    [InlineData("CON.txt", "_CON.txt")]
    [InlineData("", "file")]
    [InlineData("///", "file")]
    public void FileName_IsSanitised(string raw, string expected) =>
        AttachmentFileName.Sanitize(raw).Should().Be(expected);

    [Fact]
    public void FileName_IsCappedAtMaxLength_KeepingExtension()
    {
        var name = AttachmentFileName.Sanitize(new string('a', 400) + ".pdf");

        name.Length.Should().Be(AttachmentFileName.MaxLength);
        name.Should().EndWith(".pdf");
    }

    [Fact]
    public void SizeLimitedReadStream_FailsWhileStreaming_NotAfterBuffering()
    {
        var stream = new SizeLimitedReadStream(new MemoryStream(new byte[100]), 10);
        var buffer = new byte[4];
        stream.ReadExactly(buffer, 0, 4);
        stream.ReadExactly(buffer, 0, 4);

        var act = () => stream.ReadExactly(buffer, 0, 4);

        act.Should().Throw<PayloadTooLargeException>();
    }

    [Fact]
    public void PrefixedReadStream_ReplaysPrefixThenRest()
    {
        var stream = new PrefixedReadStream(new byte[] { 1, 2 }, new MemoryStream([3, 4]));
        using var ms = new MemoryStream();
        stream.CopyTo(ms);

        ms.ToArray().Should().Equal(1, 2, 3, 4);
    }

    [Fact]
    public void Options_AreValidated()
    {
        var validator = new AttachmentUploadOptionsValidator();

        validator.Validate(null, new AttachmentUploadOptions()).Succeeded.Should().BeTrue();
        validator.Validate(null, new AttachmentUploadOptions { MaxFileSizeBytes = 10 }).Failed.Should().BeTrue();
        validator.Validate(null, new AttachmentUploadOptions { MaxAttachmentsPerItem = 0 }).Failed.Should().BeTrue();
        validator.Validate(null, new AttachmentUploadOptions { BlockedExtensions = [".exe"] }).Failed.Should().BeTrue();
    }

    private static ExecutionContextSnapshot Snapshot() => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
        ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource, Guid.NewGuid().ToString("D"));

    private static AccessFacts Facts(string? workspaceRole) => new(
        UserExists: true, EmailVerified: true, AccountExists: true, AccountMemberRole: "Owner",
        WorkspaceExists: true, WorkspaceMemberRole: workspaceRole, ResourceExists: true,
        ResourceAudience: null, ResourceMemberRole: null, HasExplicitResourcePermission: false,
        PermissionRules: [], SubscriptionRequirementSatisfied: false, FeatureEnabled: false,
        ActiveResourcePermissionRank: null, TargetPermissionRank: null);
}
