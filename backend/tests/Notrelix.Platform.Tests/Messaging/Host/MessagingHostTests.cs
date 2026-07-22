using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Notrelix.Platform.Messaging.Host;
using Notrelix.Platform.Messaging.Observability;
using Notrelix.Platform.Messaging.Reliability;
using Notrelix.Platform.Messaging.Runtime;
using Notrelix.Platform.Messaging.Transport;
using Xunit;

namespace Notrelix.Platform.Tests.Messaging.Host;

public sealed class MessagingHostTests
{
    private readonly Mock<IMessagingRuntime> _runtimeMock = new();
    private readonly Mock<IDeliveryEngine> _deliveryEngineMock = new();
    private readonly Mock<IConnectionManager> _connectionManagerMock = new();
    private readonly MessagingMetrics _metrics = new("test");
    private readonly Mock<IDiagnosticEventPublisher> _diagMock = new();
    private readonly MessagingHealthCheck _healthCheck;
    private readonly MessagingHost _sut;

    public MessagingHostTests()
    {
        _connectionManagerMock.SetupGet(c => c.IsConnected).Returns(true);
        _connectionManagerMock.Setup(c => c.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _runtimeMock.Setup(r => r.PublishAsync(It.IsAny<EventPublication>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MessagingResult.Ok(Guid.NewGuid()));

        _deliveryEngineMock.Setup(d => d.DeliverAsync(
                It.IsAny<EventEnvelope>(),
                It.IsAny<Func<Task>>(),
                It.IsAny<DeliveryOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(DeliveryResult.Ok());

        _healthCheck = new MessagingHealthCheck(
            _connectionManagerMock.Object,
            NullLogger<MessagingHealthCheck>.Instance);

        _sut = new MessagingHost(
            _runtimeMock.Object,
            _deliveryEngineMock.Object,
            _connectionManagerMock.Object,
            Options.Create(new MessagingHostOptions()),
            _metrics,
            _diagMock.Object,
            _healthCheck,
            NullLogger<MessagingHost>.Instance);
    }

    [Fact]
    public async Task StartAsync_ShouldConnectTransport_WhenAutoConnectEnabled()
    {
        await _sut.StartAsync();

        _connectionManagerMock.Verify(c => c.ConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_ShouldNotConnect_WhenAutoConnectDisabled()
    {
        var host = new MessagingHost(
            _runtimeMock.Object,
            _deliveryEngineMock.Object,
            _connectionManagerMock.Object,
            Options.Create(new MessagingHostOptions { AutoConnect = false }),
            _metrics,
            _diagMock.Object,
            _healthCheck,
            NullLogger<MessagingHost>.Instance);

        await host.StartAsync();

        _connectionManagerMock.Verify(c => c.ConnectAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StopAsync_ShouldDisconnectTransport_WhenConnected()
    {
        await _sut.StopAsync();

        _connectionManagerMock.Verify(c => c.DisconnectAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ShouldReturnResult_FromRuntime()
    {
        var publication = TestPublication.Create();

        var result = await _sut.PublishAsync(publication);

        result.Success.Should().BeTrue();
        result.EnvelopeId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PublishAsync_ShouldPublishDiagnosticEvent()
    {
        var publication = TestPublication.Create();

        await _sut.PublishAsync(publication);

        _diagMock.Verify(d => d.Publish(It.IsAny<EventPublishedEvent>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ShouldPublishFailedDiagnostic_WhenRuntimeFails()
    {
        _runtimeMock.Setup(r => r.PublishAsync(It.IsAny<EventPublication>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MessagingResult { Success = false, Errors = ["failed"] });

        var publication = TestPublication.Create();

        await _sut.PublishAsync(publication);

        _diagMock.Verify(d => d.Publish(It.IsAny<EventPublishFailedEvent>()), Times.Once);
    }

    [Fact]
    public async Task DeliverAsync_ShouldReturnResult_FromDeliveryEngine()
    {
        var envelope = TestEnvelope.Create();

        var result = await _sut.DeliverAsync(envelope, () => Task.CompletedTask);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task DeliverAsync_ShouldPublishDiagnosticEvent()
    {
        var envelope = TestEnvelope.Create();

        await _sut.DeliverAsync(envelope, () => Task.CompletedTask);

        _diagMock.Verify(d => d.Publish(It.IsAny<DeliverySucceededEvent>()), Times.Once);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnHealthy_WhenConnected()
    {
        var result = await _sut.CheckHealthAsync();

        result.IsHealthy.Should().BeTrue();
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnDegraded_WhenNotConnected()
    {
        _connectionManagerMock.SetupGet(c => c.IsConnected).Returns(false);

        var result = await _sut.CheckHealthAsync();

        result.IsDegraded.Should().BeTrue();
    }

    [Fact]
    public async Task DisposeAsync_ShouldDisconnectTransport()
    {
        await _sut.DisposeAsync();

        _connectionManagerMock.Verify(c => c.DisconnectAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

file static class TestPublication
{
    public static EventPublication Create() => new()
    {
        Event = new TestEvent
        {
            EventId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            OccurredAt = DateTimeOffset.UtcNow,
        },
        Context = new PublishContext
        {
            CorrelationId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            OccurredAt = DateTimeOffset.UtcNow,
        },
    };
}

file static class TestEnvelope
{
    public static EventEnvelope Create() => new()
    {
        EventName = "test.host",
        EventVersion = 1,
        CorrelationId = Guid.NewGuid(),
        OccurredAt = DateTimeOffset.UtcNow,
        Data = new byte[0],
        ContentType = "application/json",
    };
}

file sealed record TestEvent
{
    public Guid EventId { get; init; }
    public Guid CorrelationId { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
}
