using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Idempotency;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Collaboration.Attachments;
using Notrelix.Application.Features.Collaboration.Attachments.Commands.CreateBoardItemAttachment;
using Notrelix.Application.Features.Collaboration.Attachments.Commands.CreatePageAttachment;
using Notrelix.Application.Features.Collaboration.Attachments.DTOs;
using Notrelix.Application.Features.Collaboration.Attachments.Queries.GetBoardItemAttachments;
using Notrelix.Application.Features.Collaboration.Attachments.Services;
using Notrelix.Domain.Collaboration;
using Notrelix.Domain.Collaboration.Attachments;
using BusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Application.Tests.Features.Collaboration;

/// <summary>
/// UC-COL-B07 and UC-COL-B09: a link attachment on a board item or a page. The size, content type and source of a link are
/// owned by the server, the display name is trimmed and cleaned like a file name, links share the per-target cap with files,
/// and the create command is replayable with an Idempotency-Key.
/// </summary>
public sealed class AttachmentLinkTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid ItemId = Guid.NewGuid();
    private static readonly Guid OtherItemId = Guid.NewGuid();
    private static readonly Guid PageId = Guid.NewGuid();

    private readonly List<Attachment> _rows = [];
    private readonly FakeCurrentRequestContext _request = new() { UserId = Guid.NewGuid() };

    public AttachmentLinkTests() => _request.Tenant.SetWorkspace(AccountId, WorkspaceId, _request.UserId);

    private ICollaborationDbContext Db()
    {
        var db = new Mock<ICollaborationDbContext>();
        db.Setup(c => c.Attachments).Returns(TestDbSet.Create(_rows).Object);
        return db.Object;
    }

    private sealed class Clock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    private sealed class NoActors : IActorLookupService
    {
        public Task<IReadOnlyList<ActorSnapshot>> FindManyAsync(IReadOnlyList<Guid> userIds, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ActorSnapshot>>([]);

        public Task<ActorSnapshot?> FindAsync(Guid userId, CancellationToken ct) => Task.FromResult<ActorSnapshot?>(null);
    }

    private CreateBoardItemAttachmentCommandHandler ItemLinks(AttachmentUploadOptions? options = null) =>
        new(Db(), _request, new Clock(), new InMemoryAttachmentTargetLock(), Options.Create(options ?? new AttachmentUploadOptions()));

    private CreatePageAttachmentCommandHandler PageLinks(AttachmentUploadOptions? options = null) =>
        new(Db(), _request, new Clock(), new InMemoryAttachmentTargetLock(), Options.Create(options ?? new AttachmentUploadOptions()));

    [Fact]
    public async Task ItemLink_Succeeds_WithTheServerOwnedSizeTypeAndSource()
    {
        var result = await ItemLinks().Handle(new CreateBoardItemAttachmentCommand(ItemId, "Spec", "https://example.com/spec"), default);

        result.Succeeded.Should().BeTrue();
        var dto = result.Data!;
        dto.Filename.Should().Be("Spec");
        dto.Url.Should().Be("https://example.com/spec");
        dto.Kind.Should().Be(AttachmentDto.LinkKind);
        dto.SizeBytes.Should().Be(0);
        dto.ContentType.Should().Be("text/uri-list");
        dto.Source.Should().Be(nameof(AttachmentType.Link));
        dto.ResourceId.Should().Be(ItemId);
        dto.UploadedBy.Should().Be(_request.UserId);
        _rows.Should().ContainSingle().Which.Type.Should().Be(AttachmentType.Link);
    }

    [Fact]
    public async Task PageLink_Succeeds_WithTheServerOwnedSizeTypeAndSource()
    {
        var result = await PageLinks().Handle(new CreatePageAttachmentCommand(PageId, "Docs", "https://example.com/docs"), default);

        result.Data!.SizeBytes.Should().Be(0);
        result.Data.ContentType.Should().Be("text/uri-list");
        result.Data.Source.Should().Be(nameof(AttachmentType.Link));
        _rows.Single().Target.Kind.Should().Be(AttachmentTargets.Page);
    }

    [Fact]
    public async Task LinkName_IsTrimmed_AndControlAndBidiCharactersAreStripped()
    {
        var result = await ItemLinks().Handle(
            new CreateBoardItemAttachmentCommand(ItemId, "  Spec\u0000 ‮v2\r\n ", "https://example.com/spec"), default);

        result.Data!.Filename.Should().Be("Spec v2");
    }

    [Fact]
    public async Task LinkName_KeepsPathSeparatorsAndPunctuation_BecauseItIsATitle()
    {
        var result = await PageLinks().Handle(new CreatePageAttachmentCommand(PageId, "Q3: plan/draft?", "https://example.com/x"), default);

        result.Data!.Filename.Should().Be("Q3: plan/draft?");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\u0000\u0007‮")]
    public void LinkName_WithNothingVisible_IsRefused(string name)
    {
        new CreateBoardItemAttachmentCommandValidator().Validate(new CreateBoardItemAttachmentCommand(ItemId, name, "https://example.com"))
            .Errors.Should().Contain(e => e.PropertyName == nameof(CreateBoardItemAttachmentCommand.Filename));
        new CreatePageAttachmentCommandValidator().Validate(new CreatePageAttachmentCommand(PageId, name, "https://example.com"))
            .Errors.Should().Contain(e => e.PropertyName == nameof(CreatePageAttachmentCommand.Filename));
    }

    [Fact]
    public void LinkName_IsLimitedTo256_AfterTrimming()
    {
        var itemValidator = new CreateBoardItemAttachmentCommandValidator();
        var pageValidator = new CreatePageAttachmentCommandValidator();
        var atLimit = " " + new string('a', 256) + " ";
        var over = new string('a', 257);

        itemValidator.Validate(new CreateBoardItemAttachmentCommand(ItemId, atLimit, "https://example.com")).IsValid.Should().BeTrue();
        pageValidator.Validate(new CreatePageAttachmentCommand(PageId, atLimit, "https://example.com")).IsValid.Should().BeTrue();
        itemValidator.Validate(new CreateBoardItemAttachmentCommand(ItemId, over, "https://example.com")).IsValid.Should().BeFalse();
        pageValidator.Validate(new CreatePageAttachmentCommand(PageId, over, "https://example.com")).IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ItemLink_BeyondTheItemLimit_IsRefused_AndStoresNoRow()
    {
        var options = new AttachmentUploadOptions { MaxAttachmentsPerItem = 1 };
        var handler = ItemLinks(options);
        await handler.Handle(new CreateBoardItemAttachmentCommand(ItemId, "a", "https://example.com/a"), default);

        var act = () => handler.Handle(new CreateBoardItemAttachmentCommand(ItemId, "b", "https://example.com/b"), default);

        (await act.Should().ThrowAsync<BusinessRuleException>())
            .Which.RuleCode.Should().Be(CollaborationRuleCodes.Collaboration_Attachment_MaxAttachmentsExceeded);
        _rows.Should().HaveCount(1);
    }

    [Fact]
    public async Task PageLink_BeyondThePageLimit_IsRefused_AndStoresNoRow()
    {
        var options = new AttachmentUploadOptions { MaxAttachmentsPerItem = 99, MaxAttachmentsPerPage = 1 };
        var handler = PageLinks(options);
        await handler.Handle(new CreatePageAttachmentCommand(PageId, "a", "https://example.com/a"), default);

        var act = () => handler.Handle(new CreatePageAttachmentCommand(PageId, "b", "https://example.com/b"), default);

        (await act.Should().ThrowAsync<BusinessRuleException>())
            .Which.RuleCode.Should().Be(CollaborationRuleCodes.Collaboration_Attachment_MaxAttachmentsExceeded);
        _rows.Should().HaveCount(1);
    }

    [Fact]
    public void BothLinkCommands_AreIdempotent_WithAStableOperation_AndNoClientOwnedFields()
    {
        typeof(CreateBoardItemAttachmentCommand).Should().BeAssignableTo<IIdempotentRequest>();
        typeof(CreatePageAttachmentCommand).Should().BeAssignableTo<IIdempotentRequest>();
        typeof(CreateBoardItemAttachmentCommand).GetCustomAttributes(typeof(IdempotencyOperationAttribute), false)
            .Cast<IdempotencyOperationAttribute>().Single().Operation.Should().Be("collaboration.attachments.create-board-item-attachment.v1");
        typeof(CreatePageAttachmentCommand).GetCustomAttributes(typeof(IdempotencyOperationAttribute), false)
            .Cast<IdempotencyOperationAttribute>().Single().Operation.Should().Be("collaboration.attachments.create-page-attachment.v1");
        typeof(CreateBoardItemAttachmentCommand).GetProperties().Select(p => p.Name)
            .Should().NotContain(["SizeBytes", "ContentType", "Source"]);
    }

    [Fact]
    public async Task ItemList_ReturnsOnlyThisItemsRows_NewestFirst()
    {
        var handler = ItemLinks();
        await handler.Handle(new CreateBoardItemAttachmentCommand(ItemId, "first", "https://example.com/1"), default);
        await handler.Handle(new CreateBoardItemAttachmentCommand(OtherItemId, "other", "https://example.com/2"), default);
        await PageLinks().Handle(new CreatePageAttachmentCommand(ItemId, "same id on a page", "https://example.com/3"), default);
        await handler.Handle(new CreateBoardItemAttachmentCommand(ItemId, "second", "https://example.com/4"), default);

        var result = await new GetBoardItemAttachmentsQueryHandler(Db(), new NoActors())
            .Handle(new GetBoardItemAttachmentsQuery(ItemId), default);

        result.Data!.Select(a => a.Filename).Should().BeEquivalentTo(["first", "second"]);
        result.Data.Should().OnlyContain(a => a.ResourceId == ItemId);
    }
}
