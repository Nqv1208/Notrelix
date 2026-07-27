using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Notrelix.Platform.Messaging.Consumers;
using Notrelix.Platform.Messaging.Observability;
using Notrelix.Platform.Messaging.Runtime;
using Xunit;

namespace Notrelix.Platform.Tests.Messaging.Consumers;

public sealed class ConsumerHostTests
{
    private readonly MessagingMetrics _metrics = new("test");
    private readonly Mock<IDiagnosticEventPublisher> _diagMock = new();
    private readonly ConsumerHost _sut;

    public ConsumerHostTests()
    {
        _sut = new ConsumerHost(_metrics, _diagMock.Object, NullLogger<ConsumerHost>.Instance);
    }

    [Fact]
    public void Register_ShouldAddConsumer()
    {
        _sut.Register("test.event", (_, _) => Task.CompletedTask);

        var registrations = _sut.GetRegistrations();
        registrations.Should().HaveCount(1);
        registrations[0].EventName.Should().Be("test.event");
    }

    [Fact]
    public void Register_ShouldApplyOptions()
    {
        _sut.Register("test.event", (_, _) => Task.CompletedTask, o =>
        {
            o.ConcurrencyLimit = 4;
            o.OrderingRequired = true;
            o.PoisonThreshold = 20;
        });

        var reg = _sut.GetRegistrations()[0];
        reg.Options.ConcurrencyLimit.Should().Be(4);
        reg.Options.OrderingRequired.Should().BeTrue();
        reg.Options.PoisonThreshold.Should().Be(20);
    }

    [Fact]
    public async Task DispatchAsync_ShouldExecuteHandler_WhenRegistered()
    {
        var handled = false;
        _sut.Register("test.event", (_, _) =>
        {
            handled = true;
            return Task.CompletedTask;
        });

        var envelope = CreateEnvelope("test.event");
        await _sut.DispatchAsync(envelope);

        handled.Should().BeTrue();
    }

    [Fact]
    public async Task DispatchAsync_ShouldNotThrow_WhenNoHandlerRegistered()
    {
        var envelope = CreateEnvelope("unregistered.event");
        await _sut.DispatchAsync(envelope);

        _diagMock.Verify(d => d.Publish(It.IsAny<DeliveryFailedEvent>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_ShouldNotExecute_WhenDisabled()
    {
        var handled = false;
        _sut.Register("test.event", (_, _) =>
        {
            handled = true;
            return Task.CompletedTask;
        }, o => o.Enabled = false);

        var envelope = CreateEnvelope("test.event");
        await _sut.DispatchAsync(envelope);

        handled.Should().BeFalse();
    }

    [Fact]
    public async Task DispatchAsync_ShouldRespectConcurrencyLimit()
    {
        var concurrent = 0;
        var maxConcurrent = 0;

        _sut.Register("test.event", async (_, _) =>
        {
            concurrent++;
            maxConcurrent = Math.Max(maxConcurrent, concurrent);
            await Task.Delay(50);
            concurrent--;
        }, o => o.ConcurrencyLimit = 1);

        var envelope = CreateEnvelope("test.event");
        var tasks = new[]
        {
            _sut.DispatchAsync(envelope),
            _sut.DispatchAsync(envelope),
            _sut.DispatchAsync(envelope),
        };

        await Task.WhenAll(tasks);
        maxConcurrent.Should().Be(1);
    }

    [Fact]
    public async Task DispatchAsync_ShouldResetPoison_OnSuccess()
    {
        var failCount = 0;
        _sut.Register("test.event", (_, _) =>
        {
            failCount++;
            if (failCount <= 2)
                throw new InvalidOperationException("fail");
            return Task.CompletedTask;
        }, o => o.PoisonThreshold = 5);

        var envelope = CreateEnvelope("test.event");

        // First call fails
        await _sut.DispatchAsync(envelope);
        // Second call fails
        await _sut.DispatchAsync(envelope);
        // Third call succeeds — should reset
        await _sut.DispatchAsync(envelope);
        // Fourth call succeeds — clean
        await _sut.DispatchAsync(envelope);

        // After success, poison should be 0
        var poisonBefore = failCount;
        await _sut.DispatchAsync(envelope);

        _diagMock.Verify(d => d.Publish(It.IsAny<DeliveryFailedEvent>()), Times.AtLeast(2));
    }

    [Fact]
    public async Task DispatchAsync_ShouldHandleHandlerThrowing_WithoutCrashing()
    {
        _sut.Register("test.event", (_, _) =>
            throw new InvalidOperationException("handler error"));

        var envelope = CreateEnvelope("test.event");
        await _sut.DispatchAsync(envelope);

        _diagMock.Verify(d => d.Publish(It.Is<DeliveryFailedEvent>(e => e.Error.Contains("handler error"))), Times.Once);
    }

    private static EventEnvelope CreateEnvelope(string eventName) => new()
    {
        EventName = eventName,
        EventVersion = 1,
        CorrelationId = Guid.NewGuid(),
        OccurredAt = DateTimeOffset.UtcNow,
        Data = new byte[0],
        ContentType = "application/json",
    };
}
