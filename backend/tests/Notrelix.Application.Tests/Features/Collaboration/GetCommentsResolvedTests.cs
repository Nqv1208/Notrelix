using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Collaboration.Comments.DTOs;
using Notrelix.Application.Features.Collaboration.Comments.Queries.GetComments;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.Collaboration.Reactions;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Application.Tests.Features.Collaboration;

/// <summary>The thread list says which threads are resolved, so a client can fold them away.</summary>
public class GetCommentsResolvedTests
{
    private static readonly DateTimeOffset Now = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly Guid _me = Guid.CreateVersion7();
    private readonly Guid _account = Guid.CreateVersion7();
    private readonly Guid _workspace = Guid.CreateVersion7();
    private readonly Guid _page = Guid.CreateVersion7();
    private readonly List<Comment> _rows = [];
    private readonly Mock<ICollaborationDbContext> _context = new();
    private readonly Mock<IActorLookupService> _actors = new();
    private readonly Mock<ICurrentUser> _user = new();

    public GetCommentsResolvedTests()
    {
        _context.Setup(c => c.Comments).Returns(TestDbSet.Create(_rows).Object);
        _context.Setup(c => c.Reactions).Returns(TestDbSet.Create<Reaction>().Object);
        _user.Setup(u => u.UserId).Returns(_me);
        _actors
            .Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ActorSnapshot(_me, "Me", null)]);
    }

    private Comment OnPage(string text)
    {
        var target = ResourceRef.Create(ResourceKind.Create("documents.page"), _page, _workspace);
        var comment = Comment.Create(_account, _workspace, target, text, _me, Now);
        _rows.Add(comment);
        return comment;
    }

    [Fact]
    public async Task Handle_ReportsAResolvedThreadWithTheTimeItWasResolved_AndAnOpenOneWithout()
    {
        var open = OnPage("still open");
        var resolved = OnPage("settled");
        resolved.Resolve(_me, Now.AddHours(2));

        var result = await new GetCommentsQueryHandler(_context.Object, _actors.Object, _user.Object, new InMemoryCommentTombstones(_rows))
            .Handle(GetCommentsQuery.ForPage(_page), CancellationToken.None);

        var byText = result.Data!.ToDictionary(c => c.ContentMd);
        byText["still open"].ResolvedAt.Should().BeNull();
        byText["settled"].ResolvedAt.Should().Be(Now.AddHours(2).DateTime);
        open.Id.Should().NotBe(resolved.Id);
    }

    [Fact]
    public async Task Handle_ReportsAReopenedThreadAsOpenAgain()
    {
        var comment = OnPage("back again");
        comment.Resolve(_me, Now.AddHours(1));
        comment.Reopen(_me, Now.AddHours(2));

        var result = await new GetCommentsQueryHandler(_context.Object, _actors.Object, _user.Object, new InMemoryCommentTombstones(_rows))
            .Handle(GetCommentsQuery.ForPage(_page), CancellationToken.None);

        result.Data!.Single().ResolvedAt.Should().BeNull();
    }

    private Comment ReplyOn(Comment parent, string text)
    {
        var parentContext = ParentCommentContext.Create(_account, _workspace, parent.Id, parent.Target);
        var reply = Comment.CreateReply(_account, _workspace, parent.Target, text, _me, Now.AddMinutes(5), parentContext);
        _rows.Add(reply);
        return reply;
    }

    private Task<Result<List<CommentDto>>> ListPage(int? limit = null, TimeCursorPosition? after = null) =>
        new GetCommentsQueryHandler(_context.Object, _actors.Object, _user.Object, new InMemoryCommentTombstones(_rows))
            .Handle(GetCommentsQuery.ForPage(_page) with { Limit = limit, After = after }, CancellationToken.None);

    [Fact]
    public async Task Handle_KeepsADeletedParentAsATombstoneWhileItHasLiveReplies()
    {
        var parent = OnPage("the first remark");
        var reply = ReplyOn(parent, "a reply that outlives it");
        parent.Delete(_me, Now.AddMinutes(1));

        var result = await ListPage();

        var rows = result.Data!;
        rows.Select(c => c.Id).Should().Equal(parent.Id, reply.Id);
        var tombstone = rows[0];
        tombstone.IsDeleted.Should().BeTrue();
        tombstone.ContentMd.Should().BeEmpty("a deleted comment keeps no body");
        tombstone.UserName.Should().BeEmpty("a deleted comment keeps no author name");
        tombstone.UserId.Should().Be(Guid.Empty);
        tombstone.Reactions.Should().BeEmpty();
        tombstone.Anchor.Should().BeNull();
        rows[1].ParentCommentId.Should().Be(parent.Id, "the reply stays attached to its parent");
        rows[1].IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_OmitsADeletedCommentThatHasNoLiveReplies()
    {
        var alone = OnPage("deleted with no replies");
        var kept = OnPage("still here");
        alone.Delete(_me, Now.AddMinutes(1));

        var result = await ListPage();

        result.Data!.Select(c => c.Id).Should().Equal(kept.Id);
    }

    [Fact]
    public async Task Handle_OmitsADeletedCommentWhoseOnlyReplyIsAlsoDeleted()
    {
        var parent = OnPage("parent");
        var reply = ReplyOn(parent, "reply");
        reply.Delete(_me, Now.AddMinutes(6));
        parent.Delete(_me, Now.AddMinutes(7));

        var result = await ListPage();

        result.Data!.Should().BeEmpty("a deleted reply is no live reply, so the parent has nothing to keep attached");
    }

    [Fact]
    public async Task Handle_PagesTombstonesInTheSameOrderAsTheLiveComments()
    {
        var target = ResourceRef.Create(ResourceKind.Create("documents.page"), _page, _workspace);
        var first = Comment.Create(_account, _workspace, target, "first", _me, Now);
        var second = Comment.Create(_account, _workspace, target, "second", _me, Now.AddMinutes(1));
        var third = Comment.Create(_account, _workspace, target, "third", _me, Now.AddMinutes(10));
        _rows.AddRange([first, second, third]);
        var firstReply = ReplyOn(first, "answer to first"); // at Now + 5 minutes
        first.Delete(_me, Now.AddMinutes(2));

        // Order: first (tombstone), second, firstReply, third.
        var firstPage = (await ListPage(limit: 2)).Data!;
        firstPage.Select(c => c.Id).Should().Equal(first.Id, second.Id);
        firstPage[0].IsDeleted.Should().BeTrue();

        var cursor = new TimeCursorPosition(second.CreatedAt, second.Id);
        var secondPage = (await ListPage(limit: 2, after: cursor)).Data!;
        secondPage.Select(c => c.Id).Should().Equal(firstReply.Id, third.Id);
        secondPage[0].ParentCommentId.Should().Be(first.Id);
    }

    [Fact]
    public void Action_ReadsAPagesCommentsUnderTheViewPageAction()
    {
        GetCommentsQuery.ForPage(_page).Action.Should().Be(PermissionAction.ViewPage);
        GetCommentsQuery.ForBoardItem(_page).Action.Should().Be(PermissionAction.ViewBoard);
    }
}
