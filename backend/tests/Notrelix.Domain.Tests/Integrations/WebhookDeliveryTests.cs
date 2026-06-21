using FluentAssertions;
using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Integrations.Webhooks;
using Notrelix.Domain.Integrations.Webhooks.Events;
using Xunit;

namespace Notrelix.Domain.Tests.Integrations;

public class WebhookDeliveryTests
{
    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var delivery = WebhookDelivery.Create(Guid.NewGuid(), Guid.NewGuid(), WebhookEventType.BoardCreated, JsonValue.Create("{}"), DateTimeOffset.UtcNow);

        delivery.Status.Should().Be(WebhookDeliveryStatus.Pending);
        delivery.RetryCount.Should().Be(0);
        delivery.MaxRetries.Should().Be(3);
        delivery.DomainEvents.Should().ContainSingle(e => e is WebhookDeliveryRecordedDomainEvent);
    }

    [Fact]
    public void MarkDelivered_ShouldTransition_AndRaiseEvent()
    {
        var delivery = CreateDelivery();
        delivery.ClearDomainEvents();

        delivery.MarkDelivered(200, "{\"ok\":true}", DateTimeOffset.UtcNow);

        delivery.Status.Should().Be(WebhookDeliveryStatus.Sent);
        delivery.ResponseStatusCode.Should().Be(200);
        delivery.DeliveredAt.Should().NotBeNull();
        delivery.DomainEvents.Should().ContainSingle(e => e is WebhookDeliveryRecordedDomainEvent);
    }

    [Fact]
    public void MarkDelivered_WhenAlreadySent_ShouldThrow()
    {
        var delivery = CreateDelivery();
        delivery.MarkDelivered(200, null, DateTimeOffset.UtcNow);

        var act = () => delivery.MarkDelivered(200, null, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*status*");
    }

    [Fact]
    public void MarkFailed_ShouldTransition_AndRaiseEvent()
    {
        var delivery = CreateDelivery();
        delivery.ClearDomainEvents();

        delivery.MarkFailed(500, "Internal Error", DateTimeOffset.UtcNow, "Server error");

        delivery.Status.Should().Be(WebhookDeliveryStatus.Failed);
        delivery.FailureReason.Should().Be("Server error");
        delivery.DomainEvents.Should().ContainSingle(e => e is WebhookDeliveryRecordedDomainEvent);
    }

    [Fact]
    public void MarkFailed_WhenAlreadySent_ShouldThrow()
    {
        var delivery = CreateDelivery();
        delivery.MarkDelivered(200, null, DateTimeOffset.UtcNow);

        var act = () => delivery.MarkFailed(500, null, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*status*");
    }

    [Fact]
    public void ScheduleRetry_ShouldTransition_AndIncrementCount()
    {
        var delivery = CreateDelivery();
        delivery.MarkFailed(500, null, DateTimeOffset.UtcNow);

        delivery.ScheduleRetry(DateTimeOffset.UtcNow.AddMinutes(5));

        delivery.Status.Should().Be(WebhookDeliveryStatus.Retrying);
        delivery.RetryCount.Should().Be(1);
        delivery.NextRetryAt.Should().NotBeNull();
    }

    [Fact]
    public void ScheduleRetry_WhenNotFailed_ShouldThrow()
    {
        var delivery = CreateDelivery();

        var act = () => delivery.ScheduleRetry(DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*failed*");
    }

    [Fact]
    public void ScheduleRetry_WhenMaxRetriesReached_ShouldThrow()
    {
        var delivery = WebhookDelivery.Create(Guid.NewGuid(), Guid.NewGuid(), WebhookEventType.ItemUpdated, JsonValue.EmptyObject(), DateTimeOffset.UtcNow, maxRetries: 1);
        delivery.MarkFailed(500, null, DateTimeOffset.UtcNow);
        delivery.ScheduleRetry(DateTimeOffset.UtcNow.AddMinutes(5));

        delivery.MarkFailed(500, null, DateTimeOffset.UtcNow);

        var act = () => delivery.ScheduleRetry(DateTimeOffset.UtcNow.AddMinutes(10));
        act.Should().Throw<BusinessRuleException>().WithMessage("*retry count*");
    }

    [Fact]
    public void FullLifecycle_MarkDeliveredAfterRetry_ShouldSucceed()
    {
        var delivery = CreateDelivery();
        delivery.MarkFailed(500, null, DateTimeOffset.UtcNow);
        delivery.ScheduleRetry(DateTimeOffset.UtcNow.AddMinutes(1));

        delivery.MarkDelivered(200, "{}", DateTimeOffset.UtcNow);

        delivery.Status.Should().Be(WebhookDeliveryStatus.Sent);
    }

    private static WebhookDelivery CreateDelivery()
    {
        return WebhookDelivery.Create(Guid.NewGuid(), Guid.NewGuid(), WebhookEventType.BoardCreated, JsonValue.Create("{}"), DateTimeOffset.UtcNow);
    }
}
