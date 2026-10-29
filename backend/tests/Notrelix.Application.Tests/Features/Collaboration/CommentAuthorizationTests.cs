using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Collaboration.Comments.Commands.DeleteComment;
using Notrelix.Application.Features.Collaboration.Comments.Commands.ReopenComment;
using Notrelix.Application.Features.Collaboration.Comments.Commands.ResolveComment;
using Notrelix.Application.Features.Collaboration.Comments.Commands.UpdateComment;
using Notrelix.Application.Features.Collaboration.Comments.Services;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Application.Tests.Features.Collaboration;

/// <summary>
/// Edit and delete belong to the comment's author while they can still see its target; resolve and reopen to the
/// thread starter or an editor of the target. No view access is the same NotFound as a missing comment.
/// </summary>
public class CommentAuthorizationTests
{
    private static readonly DateTimeOffset Now = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly Guid _author = Guid.CreateVersion7();
    private readonly Guid _other = Guid.CreateVersion7();
    private readonly Guid _account = Guid.CreateVersion7();
    private readonly Guid _workspace = Guid.CreateVersion7();
    private readonly List<Comment> _rows = [];
    private readonly Mock<ICollaborationDbContext> _context = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<ICommentTargetAccess> _access = new();
    private bool _canView = true;
    private bool _canEdit;

    public CommentAuthorizationTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(Now);
        _context.Setup(c => c.Comments).Returns(TestDbSet.Create(_rows).Object);
        _access.Setup(a => a.HasAsync(It.IsAny<ResourceRef>(), It.IsAny<CommentTargetRight>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResourceRef _, CommentTargetRight right, CancellationToken _) => right == CommentTargetRight.View ? _canView : _canEdit);
    }

    private Comment Thread(string kind = "work-management.board-item")
    {
        var target = ResourceRef.Create(ResourceKind.Create(kind), Guid.CreateVersion7(), _workspace);
        var comment = Comment.Create(_account, _workspace, target, "Hello", _author, Now);
        _rows.Add(comment);
        return comment;
    }

    private Comment Reply(Comment parent)
    {
        var context = ParentCommentContext.Create(_account, _workspace, parent.Id, parent.Target, false);
        var reply = Comment.CreateReply(_account, _workspace, parent.Target, "Reply", _other, Now, context);
        _rows.Add(reply);
        return reply;
    }

    private Task<Result> Run(string action, Guid actor, Guid commentId)
    {
        _user.Setup(u => u.UserId).Returns(actor);
        return action switch
        {
            "edit" => new UpdateCommentCommandHandler(_context.Object, _user.Object, _clock.Object, _access.Object).Handle(new UpdateCommentCommand(commentId, "Changed"), default),
            "delete" => new DeleteCommentCommandHandler(_context.Object, _user.Object, _clock.Object, _access.Object).Handle(new DeleteCommentCommand(commentId), default),
            "resolve" => new ResolveCommentCommandHandler(_context.Object, _user.Object, _clock.Object, _access.Object).Handle(new ResolveCommentCommand(commentId), default),
            _ => new ReopenCommentCommandHandler(_context.Object, _user.Object, _clock.Object, _access.Object).Handle(new ReopenCommentCommand(commentId), default),
        };
    }

    [Theory]
    [InlineData("edit")]
    [InlineData("delete")]
    public async Task Author_WhoCanViewTheTarget_MayChangeTheComment(string action)
    {
        var comment = Thread();

        (await Run(action, _author, comment.Id)).Succeeded.Should().BeTrue();

        comment.Version.Should().BeGreaterThan(1);
    }

    [Theory]
    [InlineData("edit")]
    [InlineData("delete")]
    public async Task AnotherPersonWhoCanView_EvenAnEditor_IsForbiddenWithAStableCode(string action)
    {
        _canEdit = true;
        var comment = Thread();
        var version = comment.Version;

        var act = () => Run(action, _other, comment.Id);

        (await act.Should().ThrowAsync<ForbiddenException>()).Which.Code.Should().Be(CommentAccessRules.AuthorOnlyCode);
        comment.Version.Should().Be(version);
    }

    [Theory]
    [InlineData("edit")]
    [InlineData("delete")]
    [InlineData("resolve")]
    [InlineData("reopen")]
    public async Task NoViewAccessToTheTarget_IsNotFound_EvenForTheAuthor(string action)
    {
        _canView = false;
        var comment = Thread("documents.page");

        var act = () => Run(action, _author, comment.Id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Theory]
    [InlineData("resolve")]
    [InlineData("reopen")]
    public async Task ThreadStarter_MayResolveAndReopen_WithoutEditRights(string action)
    {
        var comment = Thread();
        if (action == "reopen") comment.Resolve(_author, Now);

        (await Run(action, _author, comment.Id)).Succeeded.Should().BeTrue();

        comment.CommentStatus.Should().Be(action == "resolve" ? CommentStatus.Resolved : CommentStatus.Active);
    }

    [Theory]
    [InlineData("resolve")]
    [InlineData("reopen")]
    public async Task EditorOfTheTarget_MayResolveAndReopenAnotherPersonsThread(string action)
    {
        _canEdit = true;
        var comment = Thread();
        if (action == "reopen") comment.Resolve(_author, Now);

        (await Run(action, _other, comment.Id)).Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData("resolve")]
    [InlineData("reopen")]
    public async Task PlainCommenter_IsForbiddenFromResolvingOrReopening(string action)
    {
        var comment = Thread();
        if (action == "reopen") comment.Resolve(_author, Now);
        var status = comment.CommentStatus;

        var act = () => Run(action, _other, comment.Id);

        (await act.Should().ThrowAsync<ForbiddenException>()).Which.Code.Should().Be(CommentAccessRules.ThreadStarterOrEditorCode);
        comment.CommentStatus.Should().Be(status);
    }

    [Fact]
    public async Task TheStarterOfAThread_MayResolveWhenTheIdNamesAReply()
    {
        var parent = Thread();
        var reply = Reply(parent);

        (await Run("resolve", _author, reply.Id)).Succeeded.Should().BeTrue();
    }

    [Fact]
    public void TheWorkspaceGate_IsNoLongerOwnerOnly()
    {
        new UpdateCommentCommand(Guid.CreateVersion7(), "x").Action.Should().NotBe(PermissionAction.UpdateItem);
        new DeleteCommentCommand(Guid.CreateVersion7()).Action.Should().NotBe(PermissionAction.UpdateItem);
    }
}
