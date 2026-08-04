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

        // Handler failures below the poison threshold are rethrown after diagnostics.
        Func<Task> dispatch = () => _sut.DispatchAsync(envelope);
        await dispatch.Should().ThrowAsync<InvalidOperationException>();   // fail 1
        await dispatch.Should().ThrowAsync<InvalidOperationException>();   // fail 2
        // Third call succeeds — poison resets
        await dispatch.Should().NotThrowAsync();
        // Fourth call succeeds — clean
        await dispatch.Should().NotThrowAsync();

        _diagMock.Verify(d => d.Publish(It.IsAny<DeliveryFailedEvent>()), Times.AtLeast(2));
    }

    [Fact]
    public async Task DispatchAsync_HandlerFailure_PublishesDiagnosticsThenRethrows()
    {
        // FZ-PLT-01 (final decision: Platform failure): publish diagnostics then
        // rethrow for transport retry. Currently the host swallows the exception.
        _sut.Register("test.event", (_, _) =>
            throw new InvalidOperationException("handler error"), o => o.PoisonThreshold = 5);

        var envelope = CreateEnvelope("test.event");

        Func<Task> act = () => _sut.DispatchAsync(envelope);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("handler error");
        _diagMock.Verify(d => d.Publish(It.Is<DeliveryFailedEvent>(e => e.Error.Contains("handler error"))), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_ConcurrencyLimitReached_NeverDrops()
    {
        // FZ-PLT-01 (final decision: Platform concurrency): wait or throw backpressure;
        // never drop. Currently the host returns success without running the handler.
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handlerRuns = 0L;

        _sut.Register("test.event", (_, _) =>
        {
            Interlocked.Increment(ref handlerRuns);
            entered.TrySetResult();
            return release.Task;
        }, o => o.ConcurrencyLimit = 1);

        var first = _sut.DispatchAsync(CreateEnvelope("test.event"));
        await entered.Task;

        var second = _sut.DispatchAsync(CreateEnvelope("test.event"));
        var settled = await Task.WhenAny(second, Task.Delay(1000));

        if (ReferenceEquals(settled, second))
        {
            if (second.IsCompletedSuccessfully)
            {
                Interlocked.Read(ref handlerRuns).Should().Be(2,
                    "a dispatch that succeeds while the slot is full proves a silent drop — never drop under backpressure");
            }
            // else: backpressure throw is acceptable — the transport retries.
        }
        else
        {
            release.TrySetResult();
            await second;
            Interlocked.Read(ref handlerRuns).Should().Be(2,
                "a waiting dispatch must execute the handler once the slot frees");
        }

        release.TrySetResult();
        await first;
    }

    [Fact]
    public async Task DispatchAsync_OrderingEnabled_ProcessesFirstMessage()
    {
        // FZ-PLT-02 (final decision: Platform ordering): require a real sequence when
        // ordering is enabled. Currently the host hardcodes sequence 0, which the
        // OrderingEnforcer always rejects — the first message is dropped.
        var handled = false;
        _sut.Register("test.event", (_, _) =>
        {
            handled = true;
            return Task.CompletedTask;
        }, o => o.OrderingRequired = true);

        var envelope = CreateEnvelope("test.event");
        await _sut.DispatchAsync(envelope);

        handled.Should().BeTrue(
            "an ordering-enabled consumer must process its first message — the host must supply a real sequence, not a hardcoded 0");
        _diagMock.Verify(d => d.Publish(It.IsAny<DeliveryFailedEvent>()), Times.Never);
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
