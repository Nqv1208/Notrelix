using FluentAssertions;
using Notrelix.Domain.Collaboration.Notifications;

namespace Notrelix.Domain.Tests.Collaboration;

public class NotificationWorkspaceScopeTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid WsB = Guid.NewGuid();

    [Fact]
    public void Create_WithMatchingWorkspace_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WsA);
        var notification = Notification.Create(Guid.NewGuid(), WsA, NotificationType.System, "Title", "Content", DateTimeOffset.UtcNow, target);
        notification.WorkspaceId.Should().Be(WsA);
    }

    [Fact]
    public void Create_WithMismatchedWorkspace_ShouldThrow()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WsB);
        var act = () => Notification.Create(Guid.NewGuid(), WsA, NotificationType.System, "Title", "Content", DateTimeOffset.UtcNow, target);
        act.Should().Throw<WorkspaceMismatchException>();
    }

    [Fact]
    public void Create_WithUnscopedResourceRef_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid());
        var notification = Notification.Create(Guid.NewGuid(), WsA, NotificationType.System, "Title", "Content", DateTimeOffset.UtcNow, target);
        notification.WorkspaceId.Should().Be(WsA);
    }

    [Fact]
    public void Create_WithNullTarget_ShouldSucceed()
    {
        var notification = Notification.Create(Guid.NewGuid(), WsA, NotificationType.System, "Title", "Content", DateTimeOffset.UtcNow);
        notification.WorkspaceId.Should().Be(WsA);
    }
}
