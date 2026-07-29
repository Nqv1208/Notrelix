using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.Billing.BillingEvents;

namespace Notrelix.Domain.Tests.Billing;

[CoversAggregate(typeof(BillingEvent))]
public class BillingEventTests
{
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;
    private readonly Guid _actor = Guid.NewGuid();

    [Fact]
    public void Record_WithValidData_ShouldSucceed()
    {
        var rawData = JsonValue.Create("{\"type\":\"invoice.paid\"}");

        var billingEvent = BillingEvent.Record("evt_123", BillingEventType.InvoicePaid, rawData, _now);

        billingEvent.ProviderEventId.Should().Be("evt_123");
        billingEvent.Type.Should().Be(BillingEventType.InvoicePaid);
        billingEvent.Status.Should().Be(BillingEventStatus.Received);
        billingEvent.RawData.Should().Be(rawData);
        billingEvent.ReceivedAt.Should().Be(_now);
        billingEvent.Error.Should().BeNull();
    }

    [Fact]
    public void Record_WithEmptyProviderEventId_ShouldThrow()
    {
        var act = () => BillingEvent.Record("  ", BillingEventType.InvoicePaid, JsonValue.EmptyObject(), _now);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Record_WithNullRawData_ShouldThrow()
    {
        var act = () => BillingEvent.Record("evt_123", BillingEventType.InvoicePaid, null!, _now);

        act.Should().Throw<BusinessRuleException>();
    }

    [CoversMutation(typeof(BillingEvent), "MarkIgnored(System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [CoversMutation(typeof(BillingEvent), "MarkFailed(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [CoversMutation(typeof(BillingEvent), "MarkProcessed(System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void MarkProcessed_WhenReceived_ShouldSucceed()
    {
        var billingEvent = BillingEvent.Record("evt_123", BillingEventType.SubscriptionCreated, JsonValue.EmptyObject(), _now);

        billingEvent.MarkProcessed(_actor, _now.AddMinutes(1));

        billingEvent.Status.Should().Be(BillingEventStatus.Processed);
        billingEvent.UpdatedBy.Should().Be(_actor);
    }

    [CoversMutation(typeof(BillingEvent), "MarkIgnored(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [CoversMutation(typeof(BillingEvent), "MarkFailed(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [CoversMutation(typeof(BillingEvent), "MarkProcessed(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void MarkProcessed_WhenAlreadyProcessed_ShouldBeIdempotent()
    {
        var billingEvent = BillingEvent.Record("evt_123", BillingEventType.SubscriptionCreated, JsonValue.EmptyObject(), _now);
        billingEvent.MarkProcessed(_actor, _now.AddMinutes(1));
        var versionAfterFirst = billingEvent.Version;

        billingEvent.MarkProcessed(_actor, _now.AddMinutes(2));

        billingEvent.Status.Should().Be(BillingEventStatus.Processed);
        billingEvent.Version.Should().Be(versionAfterFirst);
    }

    [CoversMutation(typeof(BillingEvent), "MarkIgnored(System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [CoversMutation(typeof(BillingEvent), "MarkFailed(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [CoversMutation(typeof(BillingEvent), "MarkProcessed(System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void MarkFailed_ShouldSetStatusAndError()
    {
        var billingEvent = BillingEvent.Record("evt_123", BillingEventType.InvoicePaymentFailed, JsonValue.EmptyObject(), _now);

        billingEvent.MarkFailed("Processing error", _actor, _now.AddMinutes(1));

        billingEvent.Status.Should().Be(BillingEventStatus.Failed);
        billingEvent.Error.Should().Be("Processing error");
    }

    [CoversMutation(typeof(BillingEvent), "MarkIgnored(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [CoversMutation(typeof(BillingEvent), "MarkFailed(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [CoversMutation(typeof(BillingEvent), "MarkProcessed(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void MarkFailed_WhenAlreadyFailed_ShouldBeIdempotent()
    {
        var billingEvent = BillingEvent.Record("evt_123", BillingEventType.InvoicePaymentFailed, JsonValue.EmptyObject(), _now);
        billingEvent.MarkFailed("First error", _actor, _now.AddMinutes(1));
        var versionAfterFirst = billingEvent.Version;

        billingEvent.MarkFailed("Second error", _actor, _now.AddMinutes(2));

        billingEvent.Status.Should().Be(BillingEventStatus.Failed);
        billingEvent.Version.Should().Be(versionAfterFirst);
    }

    [CoversMutation(typeof(BillingEvent), "MarkIgnored(System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [CoversMutation(typeof(BillingEvent), "MarkFailed(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [CoversMutation(typeof(BillingEvent), "MarkProcessed(System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void MarkIgnored_WhenReceived_ShouldSucceed()
    {
        var billingEvent = BillingEvent.Record("evt_123", BillingEventType.SubscriptionDeleted, JsonValue.EmptyObject(), _now);

        billingEvent.MarkIgnored(_actor, _now.AddMinutes(1));

        billingEvent.Status.Should().Be(BillingEventStatus.Ignored);
    }

    [CoversMutation(typeof(BillingEvent), "MarkIgnored(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [CoversMutation(typeof(BillingEvent), "MarkFailed(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [CoversMutation(typeof(BillingEvent), "MarkProcessed(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void MarkIgnored_WhenNotReceived_ShouldBeIdempotent()
    {
        var billingEvent = BillingEvent.Record("evt_123", BillingEventType.SubscriptionDeleted, JsonValue.EmptyObject(), _now);
        billingEvent.MarkProcessed(_actor, _now.AddMinutes(1));
        var versionAfterProcess = billingEvent.Version;

        billingEvent.MarkIgnored(_actor, _now.AddMinutes(2));

        billingEvent.Status.Should().Be(BillingEventStatus.Processed);
        billingEvent.Version.Should().Be(versionAfterProcess);
    }
}
