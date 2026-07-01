using FluentAssertions;
using Notrelix.Domain.Integrations.Webhooks;

namespace Notrelix.Domain.Tests.Integrations.Webhooks;

public class WebhookDeliveryLifecycleTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _subscriptionId = Guid.NewGuid();
    private readonly JsonValue _payload = JsonValue.Create("{}");
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_ShouldSetPendingStatus_AndRaiseEvent()
    {
        var delivery = WebhookDelivery.Create(Guid.NewGuid(), _workspaceId, _subscriptionId, WebhookEventType.ItemUpdated, _payload, _now);

        delivery.Status.Should().Be(WebhookDeliveryStatus.Pending);
        delivery.WorkspaceId.Should().Be(_workspaceId);
        delivery.WebhookSubscriptionId.Should().Be(_subscriptionId);
        delivery.RetryCount.Should().Be(0);
        delivery.DomainEvents.Should().ContainSingle(e => e is WebhookDeliveryRecordedDomainEvent);
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
        delivery.DomainEvents.Should().Contain(e => e is WebhookDeliveryRecordedDomainEvent);
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
        delivery.DomainEvents.Should().Contain(e => e is WebhookDeliveryRecordedDomainEvent);
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
        return WebhookDelivery.Create(Guid.NewGuid(), _workspaceId, _subscriptionId, WebhookEventType.ItemUpdated, _payload, _now, maxRetries);
    }
}
