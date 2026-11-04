using Notrelix.Application.Common.Tenancy;
using Notrelix.Application.Features.WorkManagement.Ordering;

namespace Notrelix.Application.Tests.Features.WorkManagement.Ordering;

/// <summary>
/// A stale move of a BoardGroup or BoardField answers 409 and names the last writer in <c>changedByName</c>: the
/// display name only, none for a Workspace Guest, and the plain conflict when the lookup fails.
/// </summary>
public sealed class StalePlacementNoticeTests
{
    private readonly Guid _workspace = Guid.CreateVersion7();
    private readonly Guid _viewer = Guid.CreateVersion7();
    private readonly Guid _writer = Guid.CreateVersion7();
    private readonly Mock<IActorLookupService> _actors = new();
    private readonly Mock<IWorkspaceAccessResolver> _access = new();
    private readonly Mock<ICurrentRequestContext> _request = new();

    public StalePlacementNoticeTests()
    {
        _request.SetupGet(r => r.UserId).Returns(_viewer);
        _request.Setup(r => r.RequireWorkspaceId()).Returns(_workspace);
        _actors.Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ActorSnapshot(_writer, "Ada Lovelace", null)]);
    }

    private void Access(bool guest) =>
        _access.Setup(a => a.ResolveAsync(_workspace, _viewer, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceAccessSnapshot(Guid.NewGuid(), _workspace, _viewer, true, true, guest));

    private static ConflictException Stale() => new("The neighbours moved.", "WorkManagement_Ordering_StalePlacement");

    private Task<ConflictException> Name(Guid? writer) =>
        StalePlacementNotice.NameAsync(Stale(), writer, _request.Object, _actors.Object, _access.Object, default);

    [Fact]
    public async Task Member_GetsTheLastWritersName()
    {
        Access(guest: false);

        var conflict = await Name(_writer);

        conflict.Extensions["changedByName"].Should().Be("Ada Lovelace");
        conflict.ErrorCode.Should().Be("WorkManagement_Ordering_StalePlacement");
    }

    [Fact]
    public async Task Guest_GetsNoName()
    {
        Access(guest: true);

        (await Name(_writer)).Extensions["changedByName"].Should().BeNull();
    }

    [Fact]
    public async Task FailedLookup_KeepsThePlainConflict()
    {
        Access(guest: false);
        _actors.Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException());

        var conflict = await Name(_writer);

        conflict.Extensions.Should().NotContainKey("changedByName");
    }
}
