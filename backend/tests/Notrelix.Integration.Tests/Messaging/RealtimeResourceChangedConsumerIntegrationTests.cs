using System.Text.Json;
using MassTransit;
using Notrelix.Application.Common.Realtime;
using Notrelix.Infrastructure.Messaging.Consumers.Realtime;

namespace Notrelix.Integration.Tests.Messaging;

/// <summary>
/// Realtime delivery contract for the stage pipeline (IA-TST-OBS chain):
/// business mutation + realtime intent commit atomically in the outbox
/// (proven by OutboxAtomicityTests), the dispatcher delivers committed
/// rows (OutboxDispatchContractTests) — this suite proves the final hop:
/// given a committed RealtimeResourceChangedV1, the consumer forwards the
/// event to the realtime publisher with every contract field intact.
///
/// Commit-before-publish ordering is intentionally NOT asserted here; it
/// belongs to the outbox boundary suites listed above. The publisher is
/// faked because this is a consumer delivery-mapping contract, not a
/// Redis transport test (ProductionGraphTests pins the real publisher).
/// </summary>
public sealed class RealtimeResourceChangedConsumerIntegrationTests
{
    private static readonly Guid EventId = Guid.NewGuid();
    private static readonly Guid CorrelationId = Guid.CreateVersion7();
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid ActorUserId = Guid.NewGuid();
    private static readonly Guid ResourceId = Guid.NewGuid();

    private static RealtimeResourceChangedV1 CreateChange(JsonElement payload) =>
        new(
            eventId: EventId,
            accountId: AccountId,
            workspaceId: WorkspaceId,
            actorUserId: ActorUserId,
            correlationId: CorrelationId,
            causationId: null,
            occurredAt: DateTimeOffset.UtcNow,
            topicNamespace: "notrelix.board-items",
            resourceKind: "work-management.board-item",
            resourceId: ResourceId,
            streamKey: "board-item:1",
            streamVersion: 42L,
            changeKind: "Updated",
            payloadContract: "work-management.board-item.updated.v1",
            payload: payload);

    [Fact]
    public async Task Consumer_ForwardsEveryContractField_ToPublisher_ExactlyOnce()
    {
        var payload = JsonDocument.Parse("""{"title":"Updated item","priority":"High"}""").RootElement;
        var change = CreateChange(payload);

        var publisher = new Mock<IRealtimePublisher>();
        var consumer = new RealtimeResourceChangedConsumer(publisher.Object);

        var consumeContext = new Mock<ConsumeContext<RealtimeResourceChangedV1>>();
        consumeContext.Setup(x => x.Message).Returns(change);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(consumeContext.Object);

        publisher.Verify(
            p => p.PublishAsync(
                It.Is<RealtimeResourceChangedV1>(actual =>
                    ReferenceEquals(actual, change)
                    && actual.TopicNamespace == "notrelix.board-items"
                    && actual.ResourceKind == "work-management.board-item"
                    && actual.ResourceId == ResourceId
                    && actual.StreamKey == "board-item:1"
                    && actual.StreamVersion == 42L
                    && actual.ChangeKind == "Updated"
                    && actual.PayloadContract == "work-management.board-item.updated.v1"
                    && JsonElement.DeepEquals(actual.Payload, payload)
                    && actual.EventId == EventId
                    && actual.AccountId == AccountId
                    && actual.WorkspaceId == WorkspaceId
                    && actual.ActorUserId == ActorUserId
                    && actual.CorrelationId == CorrelationId),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "the consumer must forward the event to the realtime publisher exactly once");
    }

    [Fact]
    public async Task Consumer_Propagates_CancellationToken_ToPublisher()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        CancellationToken? observedToken = null;
        var publisher = new Mock<IRealtimePublisher>();
        publisher
            .Setup(p => p.PublishAsync(It.IsAny<RealtimeResourceChangedV1>(), It.IsAny<CancellationToken>()))
            .Callback<RealtimeResourceChangedV1, CancellationToken>((_, token) => observedToken = token)
            .Returns(Task.FromCanceled(cts.Token));

        var consumer = new RealtimeResourceChangedConsumer(publisher.Object);

        var consumeContext = new Mock<ConsumeContext<RealtimeResourceChangedV1>>();
        consumeContext.Setup(x => x.Message).Returns(CreateChange(default));
        consumeContext.Setup(x => x.CancellationToken).Returns(cts.Token);

        var act = () => consumer.Consume(consumeContext.Object);

        await act.Should().ThrowAsync<OperationCanceledException>();
        observedToken.Should().Be(cts.Token,
            "the consumer must pass the transport cancellation through to the publisher");
    }

    [Fact]
    public async Task Publisher_Failure_Propagates_Out_Of_Consumer()
    {
        var publisherFailure = new InvalidOperationException("redis unavailable");
        var publisher = new Mock<IRealtimePublisher>();
        publisher
            .Setup(p => p.PublishAsync(It.IsAny<RealtimeResourceChangedV1>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(publisherFailure);

        var consumer = new RealtimeResourceChangedConsumer(publisher.Object);

        var consumeContext = new Mock<ConsumeContext<RealtimeResourceChangedV1>>();
        consumeContext.Setup(x => x.Message).Returns(CreateChange(default));
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        var act = () => consumer.Consume(consumeContext.Object);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("redis unavailable",
                "publisher failures must surface so the transport retry/poison policy applies");
    }
}
