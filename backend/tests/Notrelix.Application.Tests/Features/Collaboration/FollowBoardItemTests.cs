using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Collaboration.Watchers.Commands.FollowBoardItem;
using Notrelix.Application.Features.Collaboration.Watchers.Commands.UnfollowBoardItem;
using Notrelix.Domain.Collaboration.Watchers;

namespace Notrelix.Application.Tests.Features.Collaboration;

/// <summary>Following an Item is idempotent, belongs to the follower, and stopping removes only their own row.</summary>
public class FollowBoardItemTests
{
    private static readonly DateTimeOffset Now = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly Guid _me = Guid.CreateVersion7();
    private readonly Guid _account = Guid.CreateVersion7();
    private readonly Guid _workspace = Guid.CreateVersion7();
    private readonly Mock<ICurrentRequestContext> _request = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly List<ResourceWatcher> _rows = [];
    private readonly Mock<DbSet<ResourceWatcher>> _watchers;
    private readonly Mock<ICollaborationDbContext> _context = new();

    public FollowBoardItemTests()
    {
        _request.Setup(c => c.UserId).Returns(_me);
        _request.Setup(c => c.RequireAccountId()).Returns(_account);
        _request.Setup(c => c.RequireWorkspaceId()).Returns(_workspace);
        _clock.Setup(c => c.UtcNow).Returns(Now);
        _watchers = TestDbSet.Create(_rows);
        _watchers.Setup(w => w.Remove(It.IsAny<ResourceWatcher>())).Callback<ResourceWatcher>(row => _rows.Remove(row));
        _context.Setup(c => c.ResourceWatchers).Returns(_watchers.Object);
    }

    private Task Follow(Guid item) =>
        new FollowBoardItemCommandHandler(_context.Object, _request.Object, _clock.Object)
            .Handle(new FollowBoardItemCommand(item), CancellationToken.None);

    private Task Unfollow(Guid item) =>
        new UnfollowBoardItemCommandHandler(_context.Object, _request.Object, _clock.Object)
            .Handle(new UnfollowBoardItemCommand(item), CancellationToken.None);

    private ResourceWatcher Row(Guid user, Guid item) => ResourceWatcher.Create(
        _account, _workspace,
        ResourceRef.Create(ResourceKind.Create(FollowBoardItemCommand.BoardItemKind), item, _workspace),
        user, user, Now);

    [Fact]
    public async Task Following_RecordsTheCallerAsAFollowerOfTheItem()
    {
        var item = Guid.CreateVersion7();

        await Follow(item);

        var row = _rows.Should().ContainSingle().Subject;
        row.UserId.Should().Be(_me);
        row.Target.ResourceId.Should().Be(item);
        row.WorkspaceId.Should().Be(_workspace);
    }

    [Fact]
    public async Task FollowingTwice_IsTheSameAsOnce()
    {
        var item = Guid.CreateVersion7();

        await Follow(item);
        await Follow(item);

        _rows.Should().HaveCount(1);
    }

    [Fact]
    public async Task Unfollowing_RemovesTheCallersRowOnly()
    {
        var item = Guid.CreateVersion7();
        var someoneElse = Guid.CreateVersion7();
        _rows.Add(Row(someoneElse, item));
        await Follow(item);

        await Unfollow(item);

        _rows.Should().ContainSingle().Which.UserId.Should().Be(someoneElse);
    }

    [Fact]
    public async Task UnfollowingAnItemThatIsNotFollowed_ChangesNothing()
    {
        await Unfollow(Guid.CreateVersion7());

        _rows.Should().BeEmpty();
    }
}
