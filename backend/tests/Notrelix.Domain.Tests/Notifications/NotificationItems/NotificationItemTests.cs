using FluentAssertions;
using Notrelix.Domain.Notifications.NotificationItems;
using Notrelix.Domain.Notifications.NotificationItems.Events;

namespace Notrelix.Domain.Tests.Notifications.NotificationItems;

public class NotificationItemTests
{
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid ActorUserId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_ShouldSetAllRequiredFields()
    {
        var item = NotificationItem.Create(
            workspaceId: WorkspaceId,
            sourceContext: "collaboration",
            notificationType: "mention.created",
            severity: NotificationSeverity.Info,
            title: "You were mentioned",
            createdAt: Now,
            actorUserId: ActorUserId);

        item.WorkspaceId.Should().Be(WorkspaceId);
        item.SourceContext.Should().Be("collaboration");
        item.NotificationType.Should().Be("mention.created");
        item.Severity.Should().Be(NotificationSeverity.Info);
        item.Title.Should().Be("You were mentioned");
        item.ActorUserId.Should().Be(ActorUserId);
        item.Status.Should().Be(NotificationItemStatus.Active);
        item.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public void Create_ShouldGenerateId()
    {
        var item = NotificationItem.Create(
            WorkspaceId, "collaboration", "test", NotificationSeverity.Info, "Test", Now);

        item.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_ShouldRaiseNotificationItemCreatedEvent()
    {
        var item = NotificationItem.Create(
            WorkspaceId, "collaboration", "test", NotificationSeverity.Info, "Test", Now,
            actorUserId: ActorUserId);

        item.DomainEvents.Should().ContainSingle(e => e is NotificationItemCreatedDomainEvent);
    }

    [Fact]
    public void Create_WithEmptyWorkspaceId_ShouldThrow()
    {
        var act = () => NotificationItem.Create(
            Guid.Empty, "collaboration", "test", NotificationSeverity.Info, "Test", Now);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptySourceContext_ShouldThrow()
    {
        var act = () => NotificationItem.Create(
            WorkspaceId, "", "test", NotificationSeverity.Info, "Test", Now);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyTitle_ShouldThrow()
    {
        var act = () => NotificationItem.Create(
            WorkspaceId, "collaboration", "test", NotificationSeverity.Info, "", Now);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithSubjectTypeButNoSubjectId_ShouldThrow()
    {
        var act = () => NotificationItem.Create(
            WorkspaceId, "collaboration", "test", NotificationSeverity.Info, "Test", Now,
            subjectType: "Board");

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithSubjectIdButNoSubjectType_ShouldThrow()
    {
        var act = () => NotificationItem.Create(
            WorkspaceId, "collaboration", "test", NotificationSeverity.Info, "Test", Now,
            subjectId: Guid.NewGuid());

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithOptionalFields_ShouldSetThem()
    {
        var subjectId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();

        var item = NotificationItem.Create(
            WorkspaceId, "collaboration", "test", NotificationSeverity.Warning, "Test", Now,
            subjectType: "Board",
            subjectId: subjectId,
            resourceType: "BoardItem",
            resourceId: resourceId,
            body: "Test body",
            actionUrl: "https://example.com");

        item.SubjectType.Should().Be("Board");
        item.SubjectId.Should().Be(subjectId);
        item.ResourceType.Should().Be("BoardItem");
        item.ResourceId.Should().Be(resourceId);
        item.Body.Should().Be("Test body");
        item.ActionUrl.Should().Be("https://example.com");
    }

    [Fact]
    public void Cancel_ShouldChangeStatus()
    {
        var item = NotificationItem.Create(
            WorkspaceId, "collaboration", "test", NotificationSeverity.Info, "Test", Now);

        item.Cancel(Now.AddMinutes(1));

        item.Status.Should().Be(NotificationItemStatus.Cancelled);
    }

    [Fact]
    public void Cancel_AlreadyCancelled_ShouldBeIdempotent()
    {
        var item = NotificationItem.Create(
            WorkspaceId, "collaboration", "test", NotificationSeverity.Info, "Test", Now);

        item.Cancel(Now.AddMinutes(1));
        item.Cancel(Now.AddMinutes(2));

        item.Status.Should().Be(NotificationItemStatus.Cancelled);
    }

    [Fact]
    public void Cancel_ShouldIncrementVersion()
    {
        var item = NotificationItem.Create(
            WorkspaceId, "collaboration", "test", NotificationSeverity.Info, "Test", Now);

        var versionBefore = item.Version;
        item.Cancel(Now.AddMinutes(1));

        item.Version.Should().Be(versionBefore + 1);
    }
}
