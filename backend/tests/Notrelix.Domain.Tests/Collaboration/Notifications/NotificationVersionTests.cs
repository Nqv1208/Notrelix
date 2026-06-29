using FluentAssertions;
using Notrelix.Domain.Collaboration.Notifications;

namespace Notrelix.Domain.Tests.Collaboration;

public class NotificationVersionTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void MarkAsRead_ShouldIncrementVersion()
    {
        var notification = Notification.Create(_userId, _workspaceId, NotificationType.Mention, "Title", "Content", _now);
        notification.ClearDomainEvents();
        var version = notification.Version;

        notification.MarkAsRead(_now);

        notification.Version.Should().Be(version + 1);
        notification.DomainEvents.Should().Contain(e => e is NotificationReadDomainEvent);
    }

    [Fact]
    public void Archive_ShouldIncrementVersion()
    {
        var notification = Notification.Create(_userId, _workspaceId, NotificationType.Mention, "Title", "Content", _now);
        notification.ClearDomainEvents();
        var version = notification.Version;

        notification.Archive(_now);

        notification.Version.Should().Be(version + 1);
        notification.DomainEvents.Should().Contain(e => e is NotificationArchivedDomainEvent);
    }
}
