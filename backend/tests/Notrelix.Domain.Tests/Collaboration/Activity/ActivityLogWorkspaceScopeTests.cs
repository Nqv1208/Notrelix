using FluentAssertions;
using Notrelix.Domain.Collaboration.Activity;

namespace Notrelix.Domain.Tests.Collaboration;

public class ActivityLogWorkspaceScopeTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid WsB = Guid.NewGuid();

    [Fact]
    public void Record_WithMatchingWorkspace_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceType.Board, Guid.NewGuid(), WsA);
        var log = ActivityLog.Record(WsA, Guid.NewGuid(), ActivityType.Created, target, DateTimeOffset.UtcNow);
        log.WorkspaceId.Should().Be(WsA);
    }

    [Fact]
    public void Record_WithMismatchedWorkspace_ShouldThrow()
    {
        var target = ResourceRef.Create(ResourceType.Board, Guid.NewGuid(), WsB);
        var act = () => ActivityLog.Record(WsA, Guid.NewGuid(), ActivityType.Created, target, DateTimeOffset.UtcNow);
        act.Should().Throw<WorkspaceMismatchException>();
    }

    [Fact]
    public void Record_WithUnscopedResourceRef_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceType.Board, Guid.NewGuid());
        var log = ActivityLog.Record(WsA, Guid.NewGuid(), ActivityType.Created, target, DateTimeOffset.UtcNow);
        log.WorkspaceId.Should().Be(WsA);
    }
}
