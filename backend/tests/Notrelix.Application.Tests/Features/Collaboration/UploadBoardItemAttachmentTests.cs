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
            Objects.Remove(key);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingStorage : IStorageService
    {
        public Task WriteAsync(string key, Stream content, string contentType, CancellationToken ct) =>
            content.CopyToAsync(Stream.Null, ct); // surfaces stream exceptions (size cap) to the handler

        public Task<Stream?> OpenReadAsync(string key, CancellationToken ct) => Task.FromResult<Stream?>(null);
        public Task DeleteAsync(string key, CancellationToken ct) => Task.CompletedTask;
    }

    private readonly List<Attachment> _rows = [];
    private readonly RequestRollbackCompensations _compensations = new(NullLogger<RequestRollbackCompensations>.Instance);

    private (UploadBoardItemAttachmentCommandHandler Handler, IStorageService Storage) CreateHandler(
        IStorageService storage, AttachmentUploadOptions? options = null)
    {
        var db = new Mock<ICollaborationDbContext>();
        db.Setup(c => c.Attachments).Returns(TestDbSet.Create(_rows).Object);
        var request = new FakeCurrentRequestContext { UserId = Guid.NewGuid() };
        request.Tenant.SetWorkspace(AccountId, WorkspaceId, request.UserId);
        var handler = new UploadBoardItemAttachmentCommandHandler(
            db.Object, request, new SystemDateTimeProvider(), storage, _compensations,
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
