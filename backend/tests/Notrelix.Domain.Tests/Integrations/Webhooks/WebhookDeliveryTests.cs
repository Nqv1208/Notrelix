using FluentAssertions;
using Notrelix.Domain.Integrations.Webhooks;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Integrations;

public class WebhookDeliveryTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid SubscriptionId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var delivery = WebhookDelivery.Create(AccountId, WorkspaceId, SubscriptionId, WebhookEventType.BoardCreated, JsonValue.Create("{}"), Now);

        delivery.Status.Should().Be(WebhookDeliveryStatus.Pending);
        delivery.RetryCount.Should().Be(0);
        delivery.MaxRetries.Should().Be(3);
        delivery.DomainEvents.Should().ContainSingle(e => e is WebhookDeliveryRecordedDomainEvent);
    }

    [CoversMutation(typeof(WebhookDelivery), nameof(WebhookDelivery.MarkFailed), MutationScenario.Event, typeof(int?), typeof(string), typeof(DateTimeOffset), typeof(string))]
    [CoversMutation(typeof(WebhookDelivery), nameof(WebhookDelivery.MarkDelivered), MutationScenario.Event, typeof(int), typeof(string), typeof(DateTimeOffset))]
    [Fact]
    public void MarkDelivered_ShouldTransition_AndRaiseEvent()
    {
        var delivery = CreateDelivery();
        ((IHasDomainEvents)delivery).ClearDomainEvents();

        delivery.MarkDelivered(200, "{\"ok\":true}", Now);

        delivery.Status.Should().Be(WebhookDeliveryStatus.Sent);
        delivery.ResponseStatusCode.Should().Be(200);
        delivery.DeliveredAt.Should().NotBeNull();
        delivery.DomainEvents.Should().ContainSingle(e => e is WebhookDeliveryRecordedDomainEvent);
    }

    [CoversMutation(typeof(WebhookDelivery), nameof(WebhookDelivery.MarkFailed), MutationScenario.NoOp, typeof(int?), typeof(string), typeof(DateTimeOffset), typeof(string))]
    [CoversMutation(typeof(WebhookDelivery), nameof(WebhookDelivery.MarkDelivered), MutationScenario.NoOp, typeof(int), typeof(string), typeof(DateTimeOffset))]
    [Fact]
    public void MarkDelivered_WhenAlreadySent_ShouldThrow()
    {
        var delivery = CreateDelivery();
        delivery.MarkDelivered(200, null, Now);

        var act = () => delivery.MarkDelivered(200, null, Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*status*");
    }

    [CoversMutation(typeof(WebhookDelivery), nameof(WebhookDelivery.MarkFailed), MutationScenario.Event, typeof(int?), typeof(string), typeof(DateTimeOffset), typeof(string))]
    [CoversMutation(typeof(WebhookDelivery), nameof(WebhookDelivery.MarkDelivered), MutationScenario.Event, typeof(int), typeof(string), typeof(DateTimeOffset))]
    [Fact]
    public void MarkFailed_ShouldTransition_AndRaiseEvent()
    {
        var delivery = CreateDelivery();
        ((IHasDomainEvents)delivery).ClearDomainEvents();

        delivery.MarkFailed(500, "Internal Error", Now, "Server error");

        delivery.Status.Should().Be(WebhookDeliveryStatus.Failed);
        delivery.FailureReason.Should().Be("Server error");
        delivery.DomainEvents.Should().ContainSingle(e => e is WebhookDeliveryRecordedDomainEvent);
    }

    [CoversMutation(typeof(WebhookDelivery), nameof(WebhookDelivery.MarkFailed), MutationScenario.NoOp, typeof(int?), typeof(string), typeof(DateTimeOffset), typeof(string))]
    [CoversMutation(typeof(WebhookDelivery), nameof(WebhookDelivery.MarkDelivered), MutationScenario.NoOp, typeof(int), typeof(string), typeof(DateTimeOffset))]
    [Fact]
    public void MarkFailed_WhenAlreadySent_ShouldThrow()
    {
        var delivery = CreateDelivery();
        delivery.MarkDelivered(200, null, Now);

        var act = () => delivery.MarkFailed(500, null, Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*status*");
    }

    [CoversMutation(typeof(WebhookDelivery), nameof(WebhookDelivery.ScheduleRetry), MutationScenario.Version, typeof(DateTimeOffset))]
    [Fact]
    public void ScheduleRetry_ShouldTransition_AndIncrementCount()
    {
        var delivery = CreateDelivery();
        delivery.MarkFailed(500, null, Now);

        delivery.ScheduleRetry(Now.AddMinutes(5));

        delivery.Status.Should().Be(WebhookDeliveryStatus.Retrying);
        delivery.RetryCount.Should().Be(1);
        delivery.NextRetryAt.Should().NotBeNull();
    }

    [CoversMutation(typeof(WebhookDelivery), nameof(WebhookDelivery.ScheduleRetry), MutationScenario.Invalid, typeof(DateTimeOffset))]
    [CoversMutation(typeof(WebhookDelivery), nameof(WebhookDelivery.MarkFailed), MutationScenario.Invalid, typeof(int?), typeof(string), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void ScheduleRetry_WhenNotFailed_ShouldThrow()
    {
        var delivery = CreateDelivery();

        var act = () => delivery.ScheduleRetry(Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*failed*");
    }

    [CoversMutation(typeof(WebhookDelivery), nameof(WebhookDelivery.ScheduleRetry), MutationScenario.Invalid, typeof(DateTimeOffset))]
    [Fact]
    public void ScheduleRetry_WhenMaxRetriesReached_ShouldThrow()
    {
        var delivery = WebhookDelivery.Create(AccountId, WorkspaceId, SubscriptionId, WebhookEventType.ItemUpdated, JsonValue.EmptyObject(), Now, maxRetries: 1);
        delivery.MarkFailed(500, null, Now);
        delivery.ScheduleRetry(Now.AddMinutes(5));

        delivery.MarkFailed(500, null, Now);

        var act = () => delivery.ScheduleRetry(Now.AddMinutes(10));
        act.Should().Throw<BusinessRuleException>().WithMessage("*retry count*");
    }

    [CoversMutation(typeof(WebhookDelivery), nameof(WebhookDelivery.ScheduleRetry), MutationScenario.Valid, typeof(DateTimeOffset))]
    [CoversMutation(typeof(WebhookDelivery), nameof(WebhookDelivery.MarkFailed), MutationScenario.Valid, typeof(int?), typeof(string), typeof(DateTimeOffset), typeof(string))]
    [CoversMutation(typeof(WebhookDelivery), nameof(WebhookDelivery.MarkDelivered), MutationScenario.Valid, typeof(int), typeof(string), typeof(DateTimeOffset))]
    [Fact]
    public void FullLifecycle_MarkDeliveredAfterRetry_ShouldSucceed()
    {
        var delivery = CreateDelivery();
        delivery.MarkFailed(500, null, Now);
        delivery.ScheduleRetry(Now.AddMinutes(1));

        delivery.MarkDelivered(200, "{}", Now);

        delivery.Status.Should().Be(WebhookDeliveryStatus.Sent);
    }

    private static WebhookDelivery CreateDelivery()
    {
        return WebhookDelivery.Create(AccountId, WorkspaceId, SubscriptionId, WebhookEventType.BoardCreated, JsonValue.Create("{}"), Now);
    }
}
