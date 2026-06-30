using FluentAssertions;
using Notrelix.Domain.Notifications.NotificationRecipients;

namespace Notrelix.Domain.Tests.Notifications.NotificationRecipients;

public class NotificationRecipientTests
{
    private static readonly Guid NotificationId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid RecipientUserId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_ShouldSetDefaultStatus()
    {
        var recipient = NotificationRecipient.Create(
            NotificationId, WorkspaceId, RecipientUserId, Now);

        recipient.Status.Should().Be(RecipientStatus.Unread);
        recipient.NotificationId.Should().Be(NotificationId);
        recipient.WorkspaceId.Should().Be(WorkspaceId);
        recipient.RecipientUserId.Should().Be(RecipientUserId);
    }

    [Fact]
    public void Create_ShouldGenerateId()
    {
        var recipient = NotificationRecipient.Create(
            NotificationId, WorkspaceId, RecipientUserId, Now);

        recipient.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_WithEmptyNotificationId_ShouldThrow()
    {
        var act = () => NotificationRecipient.Create(
            Guid.Empty, WorkspaceId, RecipientUserId, Now);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void MarkAsRead_ShouldTransitionFromUnread()
    {
        var recipient = NotificationRecipient.Create(
            NotificationId, WorkspaceId, RecipientUserId, Now);

        recipient.MarkAsRead(Now.AddMinutes(1));

        recipient.Status.Should().Be(RecipientStatus.Read);
        recipient.ReadAt.Should().Be(Now.AddMinutes(1));
        recipient.SeenAt.Should().Be(Now.AddMinutes(1));
    }

    [Fact]
    public void MarkAsRead_FromSeen_ShouldWork()
    {
        var recipient = NotificationRecipient.Create(
            NotificationId, WorkspaceId, RecipientUserId, Now);

        recipient.MarkAsSeen(Now.AddMinutes(1));
        recipient.MarkAsRead(Now.AddMinutes(2));

        recipient.Status.Should().Be(RecipientStatus.Read);
        recipient.SeenAt.Should().Be(Now.AddMinutes(1));
        recipient.ReadAt.Should().Be(Now.AddMinutes(2));
    }

    [Fact]
    public void MarkAsRead_AlreadyRead_ShouldBeIdempotent()
    {
        var recipient = NotificationRecipient.Create(
            NotificationId, WorkspaceId, RecipientUserId, Now);

        recipient.MarkAsRead(Now.AddMinutes(1));
        recipient.MarkAsRead(Now.AddMinutes(2));

        recipient.ReadAt.Should().Be(Now.AddMinutes(1));
    }

    [Fact]
    public void MarkAsRead_Archived_ShouldNotChange()
    {
        var recipient = NotificationRecipient.Create(
            NotificationId, WorkspaceId, RecipientUserId, Now);

        recipient.Archive(Now.AddMinutes(1));
        recipient.MarkAsRead(Now.AddMinutes(2));

        recipient.Status.Should().Be(RecipientStatus.Archived);
    }

    [Fact]
    public void Archive_ShouldTransition()
    {
        var recipient = NotificationRecipient.Create(
            NotificationId, WorkspaceId, RecipientUserId, Now);

        recipient.Archive(Now.AddMinutes(1));

        recipient.Status.Should().Be(RecipientStatus.Archived);
        recipient.ArchivedAt.Should().Be(Now.AddMinutes(1));
    }

    [Fact]
    public void Dismiss_ShouldTransition()
    {
        var recipient = NotificationRecipient.Create(
            NotificationId, WorkspaceId, RecipientUserId, Now);

        recipient.Dismiss(Now.AddMinutes(1));

        recipient.Status.Should().Be(RecipientStatus.Dismissed);
        recipient.DismissedAt.Should().Be(Now.AddMinutes(1));
    }

    [Fact]
    public void MarkAsSeen_ShouldTransitionFromUnread()
    {
        var recipient = NotificationRecipient.Create(
            NotificationId, WorkspaceId, RecipientUserId, Now);

        recipient.MarkAsSeen(Now.AddMinutes(1));

        recipient.Status.Should().Be(RecipientStatus.Seen);
        recipient.SeenAt.Should().Be(Now.AddMinutes(1));
    }
}
