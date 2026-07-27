using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Notrelix.Platform.Messaging.Reliability;
using Notrelix.Platform.Messaging.Runtime;
using Xunit;

namespace Notrelix.Platform.Tests.Messaging.Reliability;

public sealed class DeliveryEngineTests
{
    private readonly Mock<IDeadLetterQueue> _dlqMock = new();
    private readonly PoisonDetector _poisonDetector = new(threshold: 3);
    private readonly OrderingEnforcer _orderingEnforcer = new();
    private readonly CircuitBreaker _circuitBreaker = new(5, TimeSpan.FromSeconds(30));
    private readonly DeliveryEngine _sut;

        private static readonly RetryPolicy FastRetry = new()
    {
        MaxRetries = 5,
        Strategy = BackoffStrategy.Fixed,
        BaseDelay = TimeSpan.Zero,
    };

    public DeliveryEngineTests()
    {
        _sut = new DeliveryEngine(
            _poisonDetector,
            _orderingEnforcer,
            _circuitBreaker,
            _dlqMock.Object,
            defaultRetryPolicy: FastRetry,
            logger: NullLogger<DeliveryEngine>.Instance);
    }

    [Fact]
    public async Task DeliverAsync_ShouldSucceed_WhenSendSucceeds()
    {
        var envelope = CreateEnvelope();

        var result = await _sut.DeliverAsync(envelope, () => Task.CompletedTask);

        result.Success.Should().BeTrue();
        result.RetryCount.Should().Be(0);
    }

    [Fact]
    public async Task DeliverAsync_ShouldRetry_WhenSendFails()
    {
        var envelope = CreateEnvelope();
        var attempts = 0;

        var result = await _sut.DeliverAsync(envelope, () =>
        {
            attempts++;
            if (attempts < 2)
                throw new TimeoutException("transient");
            return Task.CompletedTask;
        });

        result.Success.Should().BeTrue();
        result.RetryCount.Should().Be(1);
    }

    [Fact]
    public async Task DeliverAsync_ShouldDeadLetter_WhenRetriesExhausted()
    {
        var envelope = CreateEnvelope();
        var options = new DeliveryOptions
        {
            RetryPolicy = new RetryPolicy { MaxRetries = 2, Strategy = BackoffStrategy.Fixed, BaseDelay = TimeSpan.Zero },
        };

        var result = await _sut.DeliverAsync(
            envelope,
            () => throw new InvalidOperationException("always fails"),
            options);

        result.Success.Should().BeFalse();
        result.DeadLettered.Should().BeTrue();
        _dlqMock.Verify(d => d.DeadLetterAsync(It.IsAny<DeadLetterEntry>(), default), Times.Once);
    }

    [Fact]
    public async Task DeliverAsync_ShouldDeadLetter_WhenPoisonDetected()
    {
        var envelope = CreateEnvelope();
        var options = new DeliveryOptions
        {
            RetryPolicy = new RetryPolicy
            {
                MaxRetries = 10,
                Strategy = BackoffStrategy.Fixed,
                BaseDelay = TimeSpan.Zero,
            },
        };

        DeliveryResult? result = null;
        for (var i = 0; i < 3; i++)
        {
            result = await _sut.DeliverAsync(
                envelope,
                () => throw new InvalidOperationException("poison"),
                options);
        }

        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.DeadLettered.Should().BeTrue();
    }

    [Fact]
    public async Task DeliverAsync_ShouldEnforceOrdering()
    {
        var envelope = CreateEnvelope();
        var options = new DeliveryOptions
        {
            PartitionKey = "p1",
            SequenceNumber = 1,
        };

        var first = await _sut.DeliverAsync(envelope, () => Task.CompletedTask, options);
        first.Success.Should().BeTrue();

        var duplicate = await _sut.DeliverAsync(envelope, () => Task.CompletedTask,
            options with { SequenceNumber = 1 });
        duplicate.Success.Should().BeFalse();
        duplicate.ErrorMessage.Should().Contain("Duplicate");
    }

    [Fact]
    public async Task DeliverAsync_ShouldHonorCircuitBreaker()
    {
        var envelope = CreateEnvelope();
        var breaker = new CircuitBreaker(failureThreshold: 1, openDuration: TimeSpan.FromHours(1));

        var engine = new DeliveryEngine(
            new PoisonDetector(100),
            new OrderingEnforcer(),
            breaker,
            defaultRetryPolicy: FastRetry,
            logger: NullLogger<DeliveryEngine>.Instance);

        await engine.DeliverAsync(envelope, () => throw new InvalidOperationException("fail"));

        var result = await engine.DeliverAsync(envelope, () => Task.CompletedTask);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Circuit breaker open");
    }

    private static EventEnvelope CreateEnvelope() => new()
    {
        EventName = "test.delivery",
        EventVersion = 1,
        CorrelationId = Guid.NewGuid(),
        OccurredAt = DateTimeOffset.UtcNow,
        Data = Array.Empty<byte>(),
        ContentType = "application/json",
    };
}
