using FluentAssertions;
using Notrelix.Domain.Integrations.Webhooks;

namespace Notrelix.Domain.Tests.Integrations;

public class OutboundDeliveryLogEntryTests
{
    private static readonly Guid DeliveryId = Guid.NewGuid();
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid ConnectionId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 10, 0, 0, TimeSpan.Zero);

    private static OutboundDeliveryLogEntry Pending() =>
        OutboundDeliveryLogEntry.Create(DeliveryId, AccountId, WorkspaceId, ConnectionId, "item.created", Now);

    [Fact]
    public void Create_StartsPending_WithNoAttempts_AndTheDeliveryIdentity()
    {
        var delivery = Pending();

        delivery.Id.Should().Be(DeliveryId);
        delivery.Status.Should().Be(OutboundDeliveryStatus.Pending);
        delivery.AttemptCount.Should().Be(0);
        delivery.ConnectionId.Should().Be(ConnectionId);
        delivery.EventType.Should().Be("item.created");
        delivery.IsTerminal.Should().BeFalse();
    }

    [Fact]
    public void FailedAttempts_KeepTheCountAndTheLastStatusAndClass()
    {
        var delivery = Pending();

        delivery.StartAttempt(1, Now);
        delivery.RecordRetrying(503, OutboundDeliveryFailureClass.ReceiverError, Now);
        delivery.StartAttempt(2, Now);
        delivery.RecordRetrying(429, OutboundDeliveryFailureClass.RateLimited, Now);
        delivery.StartAttempt(3, Now);
        delivery.RecordFailed(500, OutboundDeliveryFailureClass.ReceiverError, Now.AddMinutes(1));

        delivery.Status.Should().Be(OutboundDeliveryStatus.Failed);
        delivery.AttemptCount.Should().Be(3);
        delivery.ResponseStatusCode.Should().Be(500);
        delivery.FailureClass.Should().Be(OutboundDeliveryFailureClass.ReceiverError);
        delivery.DeliveredAt.Should().BeNull();
    }

    [Fact]
    public void Delivered_ClearsTheFailureClass_AndSetsTheTime()
    {
        var delivery = Pending();
        delivery.StartAttempt(1, Now);
        delivery.RecordRetrying(502, OutboundDeliveryFailureClass.ReceiverError, Now);
        delivery.StartAttempt(2, Now);

        delivery.RecordDelivered(204, Now.AddSeconds(5));

        delivery.Status.Should().Be(OutboundDeliveryStatus.Sent);
        delivery.ResponseStatusCode.Should().Be(204);
        delivery.FailureClass.Should().BeNull();
        delivery.DeliveredAt.Should().Be(Now.AddSeconds(5));
        delivery.AttemptCount.Should().Be(2);
    }

    [Fact]
    public void Skipped_RecordsTheReason_AndIsTerminal()
    {
        var delivery = Pending();

        delivery.RecordSkipped(OutboundDeliverySkipReason.ConnectionPaused, Now);

        delivery.Status.Should().Be(OutboundDeliveryStatus.Skipped);
        delivery.SkipReason.Should().Be(OutboundDeliverySkipReason.ConnectionPaused);
        delivery.IsTerminal.Should().BeTrue();
    }

    [Fact]
    public void TerminalDelivery_IgnoresLaterAttempts()
    {
        var delivery = Pending();
        delivery.StartAttempt(1, Now);
        delivery.RecordDelivered(200, Now);

        delivery.StartAttempt(2, Now.AddSeconds(1));
        delivery.RecordFailed(500, OutboundDeliveryFailureClass.ReceiverError, Now.AddSeconds(1));
        delivery.RecordSkipped(OutboundDeliverySkipReason.ConnectionRevoked, Now.AddSeconds(2));

        delivery.Status.Should().Be(OutboundDeliveryStatus.Sent);
        delivery.AttemptCount.Should().Be(1);
        delivery.ResponseStatusCode.Should().Be(200);
        delivery.SkipReason.Should().BeNull();
    }
}
