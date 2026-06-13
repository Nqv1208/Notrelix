using FluentAssertions;
using Notrelix.Domain.Analytics.Dashboards;
using Notrelix.Domain.Analytics.Dashboards.Events;
using Notrelix.Domain.Analytics.Widgets;
using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Integrations.Webhooks;
using Notrelix.Domain.Integrations.Webhooks.Events;
using Notrelix.Domain.SharedKernel;
using Xunit;

namespace Notrelix.Domain.Tests.Analytics;

public class Phase6AuditTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _actor = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void Rename_ShouldIncrementVersion()
    {
        var dashboard = Dashboard.Create(_workspaceId, "Old", _actor, _now);
        var versionBefore = dashboard.Version;

        dashboard.Rename("New", _actor, _now);

        dashboard.Version.Should().Be(versionBefore + 1);
        dashboard.DomainEvents.Should().Contain(e => e is DashboardRenamedEvent);
    }

    [Fact]
    public void Rename_SameName_ShouldNotIncrementVersion()
    {
        var dashboard = Dashboard.Create(_workspaceId, "Same", _actor, _now);
        var versionBefore = dashboard.Version;
        dashboard.ClearDomainEvents();

        dashboard.Rename("Same", _actor, _now);

        dashboard.Version.Should().Be(versionBefore);
        dashboard.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ChangeVisibility_ShouldIncrementVersion()
    {
        var dashboard = Dashboard.Create(_workspaceId, "D", _actor, _now);
        var versionBefore = dashboard.Version;

        dashboard.ChangeVisibility(DashboardVisibility.Public, _actor, _now);

        dashboard.Version.Should().Be(versionBefore + 1);
        dashboard.DomainEvents.Should().Contain(e => e is DashboardVisibilityChangedEvent);
    }

    [Fact]
    public void ChangeVisibility_SameValue_ShouldNotIncrementVersion()
    {
        var dashboard = Dashboard.Create(_workspaceId, "D", _actor, _now);
        dashboard.ClearDomainEvents();
        var versionBefore = dashboard.Version;

        dashboard.ChangeVisibility(DashboardVisibility.Private, _actor, _now);

        dashboard.Version.Should().Be(versionBefore);
        dashboard.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void AddWidget_ShouldIncrementVersion()
    {
        var dashboard = Dashboard.Create(_workspaceId, "D", _actor, _now);
        var versionBefore = dashboard.Version;
        var pos = WidgetPosition.Create(0, 0, 2, 2);

        dashboard.AddWidget("Stats", "Chart", JsonValue.EmptyObject(), pos, _actor, _now);

        dashboard.Version.Should().Be(versionBefore + 1);
        dashboard.DomainEvents.Should().Contain(e => e is DashboardWidgetAddedEvent);
    }

    [Fact]
    public void RemoveWidget_ShouldIncrementVersion()
    {
        var dashboard = Dashboard.Create(_workspaceId, "D", _actor, _now);
        var pos = WidgetPosition.Create(0, 0, 2, 2);
        dashboard.AddWidget("Stats", "Chart", JsonValue.EmptyObject(), pos, _actor, _now);
        dashboard.ClearDomainEvents();
        var versionBefore = dashboard.Version;
        var widgetId = dashboard.Widgets.First().Id;

        dashboard.RemoveWidget(widgetId, _actor, _now);

        dashboard.Version.Should().Be(versionBefore + 1);
        dashboard.DomainEvents.Should().Contain(e => e is DashboardWidgetRemovedEvent);
    }

    [Fact]
    public void RemoveWidget_UnknownId_ShouldNotIncrementVersion()
    {
        var dashboard = Dashboard.Create(_workspaceId, "D", _actor, _now);
        dashboard.ClearDomainEvents();
        var versionBefore = dashboard.Version;

        dashboard.RemoveWidget(Guid.NewGuid(), _actor, _now);

        dashboard.Version.Should().Be(versionBefore);
        dashboard.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void MoveWidget_ShouldIncrementVersion()
    {
        var dashboard = Dashboard.Create(_workspaceId, "D", _actor, _now);
        var pos1 = WidgetPosition.Create(0, 0, 2, 2);
        dashboard.AddWidget("Stats", "Chart", JsonValue.EmptyObject(), pos1, _actor, _now);
        dashboard.ClearDomainEvents();
        var versionBefore = dashboard.Version;
        var widgetId = dashboard.Widgets.First().Id;
        var pos2 = WidgetPosition.Create(2, 2, 4, 4);

        dashboard.MoveWidget(widgetId, pos2, _actor, _now);

        dashboard.Version.Should().Be(versionBefore + 1);
        dashboard.DomainEvents.Should().Contain(e => e is DashboardWidgetMovedEvent);
    }

    [Fact]
    public void SoftDelete_ShouldIncrementVersion_AndRaiseDeletedEvent()
    {
        var dashboard = Dashboard.Create(_workspaceId, "D", _actor, _now);
        dashboard.ClearDomainEvents();
        var versionBefore = dashboard.Version;

        dashboard.SoftDelete(_actor, _now);

        dashboard.Version.Should().Be(versionBefore + 1);
        dashboard.IsDeleted.Should().BeTrue();
        dashboard.Status.Should().Be(DashboardStatus.Archived);
        dashboard.DomainEvents.Should().Contain(e => e is DashboardDeletedEvent);
    }

    [Fact]
    public void Restore_ShouldIncrementVersion_AndRaiseRestoredEvent()
    {
        var dashboard = Dashboard.Create(_workspaceId, "D", _actor, _now);
        dashboard.SoftDelete(_actor, _now);
        dashboard.ClearDomainEvents();
        var versionBefore = dashboard.Version;

        dashboard.Restore(_actor, _now);

        dashboard.Version.Should().Be(versionBefore + 1);
        dashboard.IsDeleted.Should().BeFalse();
        dashboard.Status.Should().Be(DashboardStatus.Active);
        dashboard.DomainEvents.Should().Contain(e => e is DashboardRestoredEvent);
    }

    [Fact]
    public void SoftDelete_Twice_ShouldNotIncrementVersion()
    {
        var dashboard = Dashboard.Create(_workspaceId, "D", _actor, _now);
        dashboard.SoftDelete(_actor, _now);
        dashboard.ClearDomainEvents();
        var versionBefore = dashboard.Version;

        dashboard.SoftDelete(_actor, _now);

        dashboard.Version.Should().Be(versionBefore);
        dashboard.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Restore_Twice_ShouldNotIncrementVersion()
    {
        var dashboard = Dashboard.Create(_workspaceId, "D", _actor, _now);
        dashboard.SoftDelete(_actor, _now);
        dashboard.Restore(_actor, _now);
        dashboard.ClearDomainEvents();
        var versionBefore = dashboard.Version;

        dashboard.Restore(_actor, _now);

        dashboard.Version.Should().Be(versionBefore);
        dashboard.DomainEvents.Should().BeEmpty();
    }
}

public class Phase6WebhookDeliveryTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _subscriptionId = Guid.NewGuid();
    private readonly JsonValue _payload = JsonValue.Create("{}");
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_ShouldSetPendingStatus_AndRaiseEvent()
    {
        var delivery = WebhookDelivery.Create(_workspaceId, _subscriptionId, WebhookEventType.ItemUpdated, _payload, _now);

        delivery.Status.Should().Be(WebhookDeliveryStatus.Pending);
        delivery.WorkspaceId.Should().Be(_workspaceId);
        delivery.WebhookSubscriptionId.Should().Be(_subscriptionId);
        delivery.RetryCount.Should().Be(0);
        delivery.DomainEvents.Should().ContainSingle(e => e is WebhookDeliveryRecordedEvent);
    }

    [Fact]
    public void MarkDelivered_ShouldTransitionToSent()
    {
        var delivery = CreateDelivery();

        delivery.MarkDelivered(200, "OK", _now);

        delivery.Status.Should().Be(WebhookDeliveryStatus.Sent);
        delivery.ResponseStatusCode.Should().Be(200);
        delivery.ResponseBody.Should().Be("OK");
        delivery.DeliveredAt.Should().Be(_now);
        delivery.Version.Should().Be(2);
        delivery.DomainEvents.Should().Contain(e => e is WebhookDeliveryRecordedEvent);
    }

    [Fact]
    public void MarkFailed_ShouldTransitionToFailed()
    {
        var delivery = CreateDelivery();

        delivery.MarkFailed(500, "Internal Server Error", _now, "Timeout");

        delivery.Status.Should().Be(WebhookDeliveryStatus.Failed);
        delivery.ResponseStatusCode.Should().Be(500);
        delivery.FailureReason.Should().Be("Timeout");
        delivery.FailedAt.Should().Be(_now);
        delivery.Version.Should().Be(2);
        delivery.DomainEvents.Should().Contain(e => e is WebhookDeliveryRecordedEvent);
    }

    [Fact]
    public void MarkDelivered_FromRetrying_ShouldSucceed()
    {
        var delivery = CreateDelivery();
        delivery.MarkFailed(500, "Error", _now);
        delivery.ScheduleRetry(_now.AddMinutes(5));

        delivery.MarkDelivered(200, "OK", _now);

        delivery.Status.Should().Be(WebhookDeliveryStatus.Sent);
    }

    [Fact]
    public void MarkDelivered_FromSent_ShouldThrow()
    {
        var delivery = CreateDelivery();
        delivery.MarkDelivered(200, "OK", _now);

        var act = () => delivery.MarkDelivered(200, "OK", _now);

        act.Should().Throw<BusinessRuleException>().WithMessage("Cannot mark delivery as sent from status Sent.");
    }

    [Fact]
    public void ScheduleRetry_ShouldTransitionToRetrying()
    {
        var delivery = CreateDelivery();
        delivery.MarkFailed(500, "Error", _now, "Timeout");
        var nextRetry = _now.AddMinutes(5);

        delivery.ScheduleRetry(nextRetry);

        delivery.Status.Should().Be(WebhookDeliveryStatus.Retrying);
        delivery.RetryCount.Should().Be(1);
        delivery.NextRetryAt.Should().Be(nextRetry);
        delivery.Version.Should().Be(3);
    }

    [Fact]
    public void ScheduleRetry_FromPending_ShouldThrow()
    {
        var delivery = CreateDelivery();

        var act = () => delivery.ScheduleRetry(_now.AddMinutes(5));

        act.Should().Throw<BusinessRuleException>().WithMessage("Can only schedule retry for a failed delivery.");
    }

    [Fact]
    public void ScheduleRetry_ExceedingMaxRetries_ShouldThrow()
    {
        var delivery = CreateDelivery(maxRetries: 1);
        delivery.MarkFailed(500, "Error", _now);
        delivery.ScheduleRetry(_now.AddMinutes(5));

        delivery.MarkFailed(500, "Error", _now);
        var act = () => delivery.ScheduleRetry(_now.AddMinutes(5));

        act.Should().Throw<BusinessRuleException>().WithMessage("Maximum retry count (1) reached.");
    }

    private WebhookDelivery CreateDelivery(int maxRetries = 3)
    {
        return WebhookDelivery.Create(_workspaceId, _subscriptionId, WebhookEventType.ItemUpdated, _payload, _now, maxRetries);
    }
}
