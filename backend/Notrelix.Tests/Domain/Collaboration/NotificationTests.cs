using FluentAssertions;
using Notrelix.Domain.Collaboration.Notifications;
using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.SharedKernel;
using Xunit;

namespace Notrelix.Domain.Tests.Collaboration;

public class NotificationTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var userId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();

        var notification = Notification.Create(userId, workspaceId, NotificationType.Mention, "New mention", "You were mentioned", DateTimeOffset.UtcNow);

        notification.UserId.Should().Be(userId);
        notification.WorkspaceId.Should().Be(workspaceId);
        notification.Type.Should().Be(NotificationType.Mention);
        notification.Title.Should().Be("New mention");
        notification.Content.Should().Be("You were mentioned");
        notification.IsRead.Should().BeFalse();
        notification.IsArchived.Should().BeFalse();
    }

    [Fact]
    public void Create_WithTarget_ShouldSetTarget()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid());

        var notification = Notification.Create(Guid.NewGuid(), Guid.NewGuid(), NotificationType.Comment, "Title", "Body", DateTimeOffset.UtcNow, target);

        notification.Target.Should().Be(target);
    }

    [Fact]
    public void Create_WithTargetWorkspaceMismatch_ShouldThrow()
    {
        var workspaceId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), Guid.NewGuid());

        var act = () => Notification.Create(Guid.NewGuid(), workspaceId, NotificationType.Comment, "Title", "Body", DateTimeOffset.UtcNow, target);
        act.Should().Throw<WorkspaceMismatchException>();
    }

    [Fact]
    public void MarkAsRead_ShouldSetRead_AndRaiseEvent()
    {
        var notification = Notification.Create(Guid.NewGuid(), Guid.NewGuid(), NotificationType.Assignment, "Task", "Assigned to you", DateTimeOffset.UtcNow);

        notification.MarkAsRead(DateTimeOffset.UtcNow);

        notification.IsRead.Should().BeTrue();
        notification.ReadAt.Should().NotBeNull();
        notification.DomainEvents.Should().ContainSingle(e => e is NotificationReadEvent);
    }

    [Fact]
    public void MarkAsRead_WhenAlreadyRead_ShouldBeNoOp()
    {
        var notification = Notification.Create(Guid.NewGuid(), Guid.NewGuid(), NotificationType.Assignment, "Task", "Assigned", DateTimeOffset.UtcNow);
        notification.MarkAsRead(DateTimeOffset.UtcNow);
        notification.ClearDomainEvents();

        notification.MarkAsRead(DateTimeOffset.UtcNow);

        notification.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void MarkAsRead_WhenArchived_ShouldThrow()
    {
        var notification = Notification.Create(Guid.NewGuid(), Guid.NewGuid(), NotificationType.System, "Alert", "System alert", DateTimeOffset.UtcNow);
        notification.Archive(DateTimeOffset.UtcNow);

        var act = () => notification.MarkAsRead(DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*archived*");
    }

    [Fact]
    public void Archive_ShouldSetArchived_AndRaiseEvent()
    {
        var notification = Notification.Create(Guid.NewGuid(), Guid.NewGuid(), NotificationType.WorkspaceInvite, "Invite", "You are invited", DateTimeOffset.UtcNow);

        notification.Archive(DateTimeOffset.UtcNow);

        notification.IsArchived.Should().BeTrue();
        notification.ArchivedAt.Should().NotBeNull();
        notification.DomainEvents.Should().ContainSingle(e => e is NotificationArchivedEvent);
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_ShouldBeNoOp()
    {
        var notification = Notification.Create(Guid.NewGuid(), Guid.NewGuid(), NotificationType.System, "Alert", "Body", DateTimeOffset.UtcNow);
        notification.Archive(DateTimeOffset.UtcNow);
        notification.ClearDomainEvents();

        notification.Archive(DateTimeOffset.UtcNow);

        notification.DomainEvents.Should().BeEmpty();
    }
}
