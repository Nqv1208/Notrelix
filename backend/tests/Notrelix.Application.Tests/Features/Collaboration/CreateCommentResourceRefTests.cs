using Notrelix.Application.Features.Collaboration.Comments.DTOs;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Collaboration.Comments.Commands.CreateComment;
using Notrelix.Application.Features.Documents.Public.PageBlocks;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Public.ItemNotifications;
using Notrelix.Domain.Collaboration;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.Collaboration.Mentions;

namespace Notrelix.Application.Tests.Features.Collaboration;

/// <summary>
/// TAC-DC-002/003 — the pinned Collaboration mutation stays local: the comment
/// stores the foreign Work item as the canonical SharedKernel ResourceRef,
/// parent-comment validation is Collaboration-local, and no Work aggregate or
/// Work persistence participates anywhere in the handler.
/// </summary>
public class CreateCommentResourceRefTests
{
    private static readonly DateTimeOffset TestNow = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly Mock<ICurrentRequestContext> _requestContextMock = new();
    private readonly Mock<IDateTimeProvider> _clockMock = new();
    private readonly Mock<IPageBlockFacts> _pageBlocksMock = new();
    private readonly Mock<IWorkItemNotificationFacts> _itemFactsMock = new();
    private readonly Mock<IResourceVisibility> _visibilityMock = new();
    private readonly Guid _boardId = Guid.CreateVersion7();

    public CreateCommentResourceRefTests()
    {
        _requestContextMock.Setup(c => c.UserId).Returns(Guid.CreateVersion7());
        _requestContextMock.Setup(c => c.RequireAccountId()).Returns(Guid.CreateVersion7());
        _requestContextMock.Setup(c => c.RequireWorkspaceId()).Returns(Guid.CreateVersion7());
        _requestContextMock.Setup(c => c.IsAuthenticated).Returns(true);
        _clockMock.Setup(c => c.UtcNow).Returns(TestNow);
        _itemFactsMock
            .Setup(f => f.FindAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkItemNotificationFacts(_boardId, "Item", false, null));
        _visibilityMock
            .Setup(v => v.VisibleBoardIdsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([_boardId]);
    }

    private (CreateCommentCommandHandler Handler, Mock<DbSet<Comment>> Comments, Mock<DbSet<Mention>> Mentions, Mock<ICollaborationDbContext> ContextMock) CreateSut()
    {
        var comments = TestDbSet.Create<Comment>();
        var mentions = TestDbSet.Create<Mention>();
        var contextMock = new Mock<ICollaborationDbContext>();
        contextMock.Setup(c => c.Comments).Returns(comments.Object);
        contextMock.Setup(c => c.PageMentions).Returns(mentions.Object);
        return (new CreateCommentCommandHandler(contextMock.Object, _requestContextMock.Object, _clockMock.Object, _pageBlocksMock.Object, _itemFactsMock.Object, _visibilityMock.Object), comments, mentions, contextMock);
    }

    [Fact]
    public async Task CommentWithExplicitMentionedUsers_CreatesMentionAggregates_InTheSameUnitOfWork()
    {
        var (sut, _, mentions, _) = CreateSut();
        var mentionedUser = Guid.CreateVersion7();
        var authorId = _requestContextMock.Object.UserId;
        var workspaceId = _requestContextMock.Object.RequireWorkspaceId();
        var boardItemId = Guid.CreateVersion7();

        var result = await sut.Handle(
            CreateCommentCommand.ForBoardItem(boardItemId, "hello", null, [mentionedUser]),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        mentions.Verify(m => m.Add(It.Is<Mention>(mention =>
            mention.MentionedId == mentionedUser &&
            mention.MentionedByUserId == authorId &&
            mention.WorkspaceId == workspaceId &&
            mention.Type == MentionType.User)), Times.Once);
    }

    [Fact]
    public async Task ItemCommentMentioningSomeoneWhoCannotViewTheBoard_KeepsTheCommentAndCreatesNoMention()
    {
        var (sut, comments, mentions, _) = CreateSut();
        var outsider = Guid.CreateVersion7();
        var insider = Guid.CreateVersion7();
        _visibilityMock
            .Setup(v => v.VisibleBoardIdsAsync(It.IsAny<Guid>(), outsider, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Guid.CreateVersion7()]);

        var result = await sut.Handle(
            CreateCommentCommand.ForBoardItem(Guid.CreateVersion7(), "hi @outsider @insider", null, [outsider, insider]),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        comments.Verify(c => c.Add(It.IsAny<Comment>()), Times.Once);
        mentions.Verify(m => m.Add(It.Is<Mention>(x => x.MentionedId == outsider)), Times.Never);
        mentions.Verify(m => m.Add(It.Is<Mention>(x => x.MentionedId == insider)), Times.Once);
    }

    [Fact]
    public async Task ItemCommentWhoseItemIsGone_CreatesNoMention()
    {
        var (sut, _, mentions, _) = CreateSut();
        _itemFactsMock
            .Setup(f => f.FindAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkItemNotificationFacts?)null);

        var result = await sut.Handle(
            CreateCommentCommand.ForBoardItem(Guid.CreateVersion7(), "hello", null, [Guid.CreateVersion7()]),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        mentions.Verify(m => m.Add(It.IsAny<Mention>()), Times.Never);
    }

    [Fact]
    public async Task CommentWithDuplicateMentions_CollapsesToDistinctAggregates()
    {
        // M7 normalization is identity-only: empty ids are rejected, distinct
        // ids collapse — the authority does not forbid a self-mention, so the
        // producer must not silently drop one.
        var (sut, _, mentions, _) = CreateSut();
        var mentionedUser = Guid.CreateVersion7();

        var result = await sut.Handle(
            CreateCommentCommand.ForBoardItem(Guid.CreateVersion7(), "hello", null, [mentionedUser, mentionedUser, Guid.Empty]),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        mentions.Verify(m => m.Add(It.IsAny<Mention>()), Times.Once,
            "duplicate ids collapse and empty ids are rejected before the aggregate");
    }

    [Fact]
    public async Task ForBoardItem_StoresCanonicalResourceRef_ThroughCollaborationContextOnly()
    {
        var (sut, comments, _, _) = CreateSut();
        var boardItemId = Guid.CreateVersion7();

        var result = await sut.Handle(CreateCommentCommand.ForBoardItem(boardItemId, "hello", null), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        comments.Verify(c => c.Add(It.Is<Comment>(comment =>
            comment.Target.Kind.Value == "work-management.board-item" &&
            comment.Target.ResourceId == boardItemId &&
            comment.Target.WorkspaceId == _requestContextMock.Object.RequireWorkspaceId())), Times.Once);
    }

    [Fact]
    public async Task Reply_WithUnknownParent_FailsWithoutForeignLookup()
    {
        var (sut, comments, _, _) = CreateSut();

        await sut.Invoking(s => s.Handle(
                CreateCommentCommand.ForBoardItem(Guid.CreateVersion7(), "orphan reply", Guid.CreateVersion7()),
                CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>(
                "parent-comment existence is a Collaboration-owned check and must not require the Work context");
        comments.Verify(c => c.Add(It.IsAny<Comment>()), Times.Never);
    }

    [Fact]
    public void Command_CarriesStableResourceRefIdentity()
    {
        var boardItemId = Guid.CreateVersion7();
        var command = CreateCommentCommand.ForBoardItem(boardItemId, "hello", null);

        command.Resource.Kind.Value.Should().Be("work-management.board-item");
        command.Resource.ResourceId.Should().Be(boardItemId);
        command.Resource.WorkspaceId.Should().BeNull(
            "the request-level ref is the stable cross-resource identity; workspace scope is bound by the pipeline");
    }

    [Fact]
    public async Task PageComment_WithAnchorOnABlockOfThePage_StoresTheTextRangeAnchor()
    {
        var (sut, comments, _, _) = CreateSut();
        var pageId = Guid.CreateVersion7();
        var blockId = Guid.CreateVersion7();
        var workspaceId = _requestContextMock.Object.RequireWorkspaceId();
        _pageBlocksMock
            .Setup(f => f.BlockBelongsToPageAsync(workspaceId, pageId, blockId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await sut.Handle(
            CreateCommentCommand.ForPage(pageId, "why?", null, null, new CommentAnchorInput(blockId, "world", 6, 11)),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        comments.Verify(c => c.Add(It.Is<Comment>(comment =>
            comment.Anchor.IsTextRange &&
            comment.Anchor.BlockId == blockId &&
            comment.Anchor.Quote == "world" &&
            comment.Anchor.StartOffset == 6 &&
            comment.Anchor.EndOffset == 11)), Times.Once);
    }

    [Fact]
    public async Task PageComment_WithAnchorOnABlockOfAnotherPage_IsRejectedAndNothingIsAdded()
    {
        var (sut, comments, mentions, _) = CreateSut();
        _pageBlocksMock
            .Setup(f => f.BlockBelongsToPageAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await sut.Invoking(s => s.Handle(
                CreateCommentCommand.ForPage(Guid.CreateVersion7(), "why?", null, [Guid.CreateVersion7()],
                    new CommentAnchorInput(Guid.CreateVersion7(), "world", 6, 11)),
                CancellationToken.None))
            .Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>()
            .Where(e => e.RuleCode == CollaborationRuleCodes.Collaboration_Comment_AnchorInvalid);

        comments.Verify(c => c.Add(It.IsAny<Comment>()), Times.Never);
        mentions.Verify(m => m.Add(It.IsAny<Mention>()), Times.Never);
    }

    [Fact]
    public async Task PageComment_WithAnchorWhoseRangeDisagreesWithItsQuote_IsRejectedBeforeAnyLookup()
    {
        var (sut, comments, _, _) = CreateSut();

        await sut.Invoking(s => s.Handle(
                CreateCommentCommand.ForPage(Guid.CreateVersion7(), "why?", null, null,
                    new CommentAnchorInput(Guid.CreateVersion7(), "world", 6, 20)),
                CancellationToken.None))
            .Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();

        _pageBlocksMock.Verify(f => f.BlockBelongsToPageAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        comments.Verify(c => c.Add(It.IsAny<Comment>()), Times.Never);
    }

    [Fact]
    public async Task BoardItemComment_WithAnchor_IsRejected()
    {
        var (sut, comments, _, _) = CreateSut();

        await sut.Invoking(s => s.Handle(
                CreateCommentCommand.ForBoardItem(Guid.CreateVersion7(), "why?", null, null,
                    new CommentAnchorInput(Guid.CreateVersion7(), "world", 6, 11)),
                CancellationToken.None))
            .Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();

        comments.Verify(c => c.Add(It.IsAny<Comment>()), Times.Never);
    }

    [Fact]
    public async Task Reply_WithAnchor_IsRejected()
    {
        var (sut, comments, _, _) = CreateSut();

        await sut.Invoking(s => s.Handle(
                CreateCommentCommand.ForPage(Guid.CreateVersion7(), "why?", Guid.CreateVersion7(), null,
                    new CommentAnchorInput(Guid.CreateVersion7(), "world", 6, 11)),
                CancellationToken.None))
            .Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();

        comments.Verify(c => c.Add(It.IsAny<Comment>()), Times.Never);
    }
}
