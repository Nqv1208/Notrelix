using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Notrelix.Platform.Messaging.Consumers;
using Notrelix.Platform.Messaging.Observability;
using Notrelix.Platform.Messaging.Runtime;
using Xunit;

namespace Notrelix.Platform.Tests.Messaging.Consumers;

/// <summary>
/// FZ-PLT-01 delivery contract: typed, observable outcomes for backpressure,
/// event-version mismatch, ordering, and poison detection. Delivery is never
/// silently dropped and success metrics count only real handler success.
/// </summary>
public sealed class ConsumerHostDeliveryContractTests
{
    private readonly MessagingMetrics _metrics = new("test");
    private readonly Mock<IDiagnosticEventPublisher> _diagMock = new();
    private readonly ConsumerHost _sut;

    public ConsumerHostDeliveryContractTests()
    {
        _sut = new ConsumerHost(_metrics, _diagMock.Object, NullLogger<ConsumerHost>.Instance);
    }

    private static EventEnvelope CreateEnvelope(string eventName, int eventVersion = 1, long? sequence = null) => new()
    {
        EventName = eventName,
        EventVersion = eventVersion,
        CorrelationId = Guid.NewGuid(),
        Sequence = sequence,
        OccurredAt = DateTimeOffset.UtcNow,
        Data = new byte[0],
        ContentType = "application/json",
    };

    [Fact]
    public async Task BackpressureTimeout_Throws_TypedException_AndDoesNotDrop()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _sut.Register("test.event", (_, _) => release.Task, o =>
        {
            o.ConcurrencyLimit = 1;
            o.QueueWaitTimeout = TimeSpan.FromMilliseconds(50);
        });

        var first = _sut.DispatchAsync(CreateEnvelope("test.event"));
        await Task.Delay(20); // let the first dispatch take the only slot

        var act = () => _sut.DispatchAsync(CreateEnvelope("test.event"));
        await act.Should().ThrowAsync<ConsumerBackpressureException>();

        release.TrySetResult();
        await first;
    }

    [Fact]
    public async Task EventVersionMismatch_Throws_MessageContractException()
    {
        var handled = false;
        _sut.Register("test.event", (_, _) =>
        {
            handled = true;
            return Task.CompletedTask;
        }, o => o.ExpectedEventVersion = 2);

        var act = () => _sut.DispatchAsync(CreateEnvelope("test.event", eventVersion: 1));

        await act.Should().ThrowAsync<MessageContractException>();
        handled.Should().BeFalse("a version mismatch must not be delivered");
    }

    [Fact]
    public async Task EventVersionMatch_IsDelivered()
    {
        var handled = false;
        _sut.Register("test.event", (_, _) =>
        {
            handled = true;
            return Task.CompletedTask;
        }, o => o.ExpectedEventVersion = 2);

        await _sut.DispatchAsync(CreateEnvelope("test.event", eventVersion: 2));

        handled.Should().BeTrue();
    }

    [Fact]
    public async Task OrderedConsumer_MissingSequence_Throws_MessageOrderingException()
    {
        var handled = false;
        _sut.Register("test.event", (_, _) =>
        {
            handled = true;
            return Task.CompletedTask;
        }, o => o.OrderingRequired = true);

        var act = () => _sut.DispatchAsync(CreateEnvelope("test.event"));

        await act.Should().ThrowAsync<MessageOrderingException>();
        handled.Should().BeFalse("an envelope without a sequence must not be delivered");
        _diagMock.Verify(d => d.Publish(It.IsAny<DeliveryFailedEvent>()), Times.Once);
    }

    [Fact]
    public async Task OrderedConsumer_DuplicateSequence_Throws_MessageOrderingException()
    {
        var handled = 0;
        _sut.Register("test.event", (_, _) =>
        {
            Interlocked.Increment(ref handled);
            return Task.CompletedTask;
        }, o => o.OrderingRequired = true);

        var aggregateId = Guid.NewGuid();
        await _sut.DispatchAsync(CreateOrderedEnvelope(aggregateId, sequence: 1));

        var act = () => _sut.DispatchAsync(CreateOrderedEnvelope(aggregateId, sequence: 1));
        await act.Should().ThrowAsync<MessageOrderingException>();
        handled.Should().Be(1, "the duplicate sequence must not be delivered twice");
    }

    [Fact]
    public async Task OrderedConsumer_SequenceGap_Throws_MessageOrderingException()
    {
        _sut.Register("test.event", (_, _) => Task.CompletedTask, o => o.OrderingRequired = true);

        var aggregateId = Guid.NewGuid();
        await _sut.DispatchAsync(CreateOrderedEnvelope(aggregateId, sequence: 1));

        var act = () => _sut.DispatchAsync(CreateOrderedEnvelope(aggregateId, sequence: 3));
        await act.Should().ThrowAsync<MessageOrderingException>();
    }

    [Fact]
    public async Task OrderedConsumer_InOrderSequences_AreDelivered()
    {
        var handled = 0;
        _sut.Register("test.event", (_, _) =>
        {
            Interlocked.Increment(ref handled);
            return Task.CompletedTask;
        }, o => o.OrderingRequired = true);

        var aggregateId = Guid.NewGuid();
        await _sut.DispatchAsync(CreateOrderedEnvelope(aggregateId, sequence: 1));
        await _sut.DispatchAsync(CreateOrderedEnvelope(aggregateId, sequence: 2));
        await _sut.DispatchAsync(CreateOrderedEnvelope(aggregateId, sequence: 3));

        handled.Should().Be(3);
        _diagMock.Verify(d => d.Publish(It.IsAny<DeliveryFailedEvent>()), Times.Never);
    }

    [Fact]
    public async Task PoisonThreshold_Throws_PoisonMessageException_WrappingOriginal()
    {
        _sut.Register("test.event", (_, _) =>
            throw new InvalidOperationException("boom"), o => o.PoisonThreshold = 2);

        var envelope = CreateEnvelope("test.event");

        // Below threshold: original exception is rethrown.
        var first = () => _sut.DispatchAsync(envelope);
        await first.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");

        // At threshold: typed dead-letter recommendation wraps the original.
        var second = () => _sut.DispatchAsync(envelope);
        var expected = await second.Should().ThrowAsync<PoisonMessageException>();
        expected.And.InnerException.Should().BeOfType<InvalidOperationException>()
            .Which.Message.Should().Be("boom");
    }

    private static EventEnvelope CreateOrderedEnvelope(Guid aggregateId, long sequence) => new()
    {
        EventName = "test.event",
        EventVersion = 1,
        CorrelationId = Guid.NewGuid(),
        AggregateId = aggregateId,
        Sequence = sequence,
        OccurredAt = DateTimeOffset.UtcNow,
        Data = new byte[0],
        ContentType = "application/json",
    };
}
