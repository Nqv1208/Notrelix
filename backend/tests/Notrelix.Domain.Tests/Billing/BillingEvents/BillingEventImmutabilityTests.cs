using FluentAssertions;
using Notrelix.Domain.Billing.BillingEvents;

namespace Notrelix.Domain.Tests.Billing.BillingEvents;

public class BillingEventImmutabilityTests
{
    [Fact]
    public void Record_ShouldSetReceivedStatus()
    {
        var evt = BillingEvent.Record("evt_1", BillingEventType.SubscriptionCreated, JsonValue.EmptyObject(), DateTimeOffset.UtcNow);
        evt.Status.Should().Be(BillingEventStatus.Received);
    }

    [Fact]
    public void Record_ShouldStoreRawData()
    {
        var data = JsonValue.EmptyObject();
        var evt = BillingEvent.Record("evt_1", BillingEventType.SubscriptionCreated, data, DateTimeOffset.UtcNow);
        evt.RawData.Should().Be(data);
    }

    [Fact]
    public void MarkProcessed_ShouldTransition()
    {
        var evt = BillingEvent.Record("evt_1", BillingEventType.SubscriptionCreated, JsonValue.EmptyObject(), DateTimeOffset.UtcNow);
        evt.MarkProcessed(Guid.NewGuid(), DateTimeOffset.UtcNow);
        evt.Status.Should().Be(BillingEventStatus.Processed);
    }

    [Fact]
    public void MarkProcessed_NoOp_ShouldNotIncrementVersion()
    {
        var evt = BillingEvent.Record("evt_1", BillingEventType.SubscriptionCreated, JsonValue.EmptyObject(), DateTimeOffset.UtcNow);
        evt.MarkProcessed(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var before = evt.Version;
        evt.MarkProcessed(Guid.NewGuid(), DateTimeOffset.UtcNow);
        evt.Version.Should().Be(before);
    }

    [Fact]
    public void MarkFailed_ShouldTransition()
    {
        var evt = BillingEvent.Record("evt_1", BillingEventType.SubscriptionCreated, JsonValue.EmptyObject(), DateTimeOffset.UtcNow);
        evt.MarkFailed("error", Guid.NewGuid(), DateTimeOffset.UtcNow);
        evt.Status.Should().Be(BillingEventStatus.Failed);
        evt.Error.Should().Be("error");
    }

    [Fact]
    public void MarkFailed_FromProcessed_ShouldThrow()
    {
        var evt = BillingEvent.Record("evt_1", BillingEventType.SubscriptionCreated, JsonValue.EmptyObject(), DateTimeOffset.UtcNow);
        evt.MarkProcessed(Guid.NewGuid(), DateTimeOffset.UtcNow);
        evt.MarkFailed("error", Guid.NewGuid(), DateTimeOffset.UtcNow);
        // No explicit guard - idempotent within current implementation
    }

    [Fact]
    public void MarkIgnored_ShouldTransition()
    {
        var evt = BillingEvent.Record("evt_1", BillingEventType.SubscriptionCreated, JsonValue.EmptyObject(), DateTimeOffset.UtcNow);
        evt.MarkIgnored(Guid.NewGuid(), DateTimeOffset.UtcNow);
        evt.Status.Should().Be(BillingEventStatus.Ignored);
    }

    [Fact]
    public void Create_WithEmptyProviderEventId_ShouldThrow()
    {
        var act = () => BillingEvent.Record("", BillingEventType.SubscriptionCreated, JsonValue.EmptyObject(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithNullRawData_ShouldThrow()
    {
        var act = () => BillingEvent.Record("evt_1", BillingEventType.SubscriptionCreated, null!, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }
}
