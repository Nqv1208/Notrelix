using Notrelix.Application.Common.Models;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Collaboration.Comments.Commands.ReopenComment;
using Notrelix.Application.Features.Collaboration.Comments.Commands.ResolveComment;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.Collaboration.Comments.Events;

namespace Notrelix.Application.Tests.Features.Collaboration;

/// <summary>Reopening a resolved thread mirrors resolving it: same permission, same no-op and failure rules.</summary>
public class ReopenCommentTests
{
    private static readonly DateTimeOffset Now = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly Guid _me = Guid.CreateVersion7();
    private readonly Guid _account = Guid.CreateVersion7();
    private readonly Guid _workspace = Guid.CreateVersion7();
    private readonly List<Comment> _rows = [];
    private readonly Mock<ICollaborationDbContext> _context = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Mock<IDateTimeProvider> _clock = new();

    public ReopenCommentTests()
    {
        _user.Setup(u => u.UserId).Returns(_me);
        _clock.Setup(c => c.UtcNow).Returns(Now);
        _context.Setup(c => c.Comments).Returns(TestDbSet.Create(_rows).Object);
    }

    private Comment PageComment()
    {
        var target = ResourceRef.Create(ResourceKind.Create("documents.page"), Guid.CreateVersion7(), _workspace);
        var comment = Comment.Create(_account, _workspace, target, "Hello", _me, Now);
        _rows.Add(comment);
        return comment;
    }

    private Task<Result> Reopen(Guid id) =>
        new ReopenCommentCommandHandler(_context.Object, _user.Object, _clock.Object)
            .Handle(new ReopenCommentCommand(id), CancellationToken.None);

    [Fact]
    public async Task Reopening_AResolvedThread_MakesItActiveAndRaisesTheEvent()
    {
        var comment = PageComment();
        comment.Resolve(_me, Now);
        var version = comment.Version;

        var result = await Reopen(comment.Id);

        result.Succeeded.Should().BeTrue();
        comment.CommentStatus.Should().Be(CommentStatus.Active);
        comment.Version.Should().Be(version + 1);
        comment.DomainEvents.OfType<CommentReopenedDomainEvent>().Should().ContainSingle();
    }

    [Fact]
    public async Task Reopening_AnActiveThread_IsASemanticNoOp()
    {
        var comment = PageComment();
        var version = comment.Version;

        var result = await Reopen(comment.Id);

        result.Succeeded.Should().BeTrue();
        comment.CommentStatus.Should().Be(CommentStatus.Active);
        comment.Version.Should().Be(version);
        comment.DomainEvents.OfType<CommentReopenedDomainEvent>().Should().BeEmpty();
    }

    [Fact]
    public async Task Reopening_AMissingThread_IsNotFound()
    {
        var act = () => Reopen(Guid.CreateVersion7());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Reopening_ADeletedThread_IsRejectedAndLeavesItUnchanged()
    {
        var comment = PageComment();
        comment.Resolve(_me, Now);
        comment.Delete(_me, Now);
        var version = comment.Version;

        var act = () => Reopen(comment.Id);

        await act.Should().ThrowAsync<Exception>();
        comment.CommentStatus.Should().Be(CommentStatus.Resolved);
        comment.Version.Should().Be(version);
    }

    [Fact]
    public void Reopen_RequiresTheSamePermissionOnTheSameResourceAsResolve()
    {
        var id = Guid.CreateVersion7();
        var reopen = new ReopenCommentCommand(id);
        var resolve = new ResolveCommentCommand(id);

        reopen.Should().BeAssignableTo<IRequirePermission>();
        reopen.Should().BeAssignableTo<IResourceScopedRequest>();
        reopen.Action.Should().Be(resolve.Action);
        reopen.Resource.Should().Be(resolve.Resource);
    }
}
