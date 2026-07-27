using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Notrelix.Platform.Messaging.Operations;
using Notrelix.Platform.Messaging.Runtime;
using Xunit;

namespace Notrelix.Platform.Tests.Messaging.Operations;

public sealed class ReplayEngineTests
{
    private readonly Mock<IMessagingRuntime> _runtimeMock = new();
    private readonly Mock<IReplayCheckpointStore> _checkpointStoreMock = new();
    private readonly Mock<IReplayAuditLog> _auditLogMock = new();
    private readonly ReplayEngine _sut;

    public ReplayEngineTests()
    {
        _runtimeMock.Setup(r => r.PublishAsync(It.IsAny<EventPublication>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MessagingResult.Ok(Guid.NewGuid()));

        _checkpointStoreMock.Setup(s => s.SaveAsync(
                It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, Guid ws, long pos, CancellationToken _) =>
                new ReplayCheckpoint { Id = pos, EventName = name, WorkspaceId = ws, EventPosition = pos, CreatedAt = DateTimeOffset.UtcNow });

        _auditLogMock.Setup(a => a.StartAsync(It.IsAny<ReplayRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        _sut = new ReplayEngine(
            _runtimeMock.Object,
            _checkpointStoreMock.Object,
            _auditLogMock.Object,
            NullLogger<ReplayEngine>.Instance);
    }

    private static EventPublication CreatePublication(Guid workspaceId) => new()
    {
        Event = new { Id = 1 },
        Context = new PublishContext
        {
            CorrelationId = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            OccurredAt = DateTimeOffset.UtcNow,
        },
    };

    [Fact]
    public async Task ExecuteAsync_ShouldPublishAllEvents_FromStrategy()
    {
        var request = new ReplayRequest
        {
            EventName = "test.replay",
            WorkspaceId = Guid.NewGuid(),
            MaxEventsPerSecond = 0,
        };

        var strategy = new TestStrategy(3);

        var result = await _sut.ExecuteAsync(request, strategy);

        result.Success.Should().BeTrue();
        result.TotalPublished.Should().Be(3);
        result.TotalRequested.Should().Be(3);
        _runtimeMock.Verify(r => r.PublishAsync(It.IsAny<EventPublication>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSaveCheckpoint_AfterEachEvent()
    {
        var request = new ReplayRequest
        {
            EventName = "test.replay",
            WorkspaceId = Guid.NewGuid(),
            MaxEventsPerSecond = 0,
        };

        var strategy = new TestStrategy(2);

        await _sut.ExecuteAsync(request, strategy);

        _checkpointStoreMock.Verify(
            s => s.SaveAsync("test.replay", request.WorkspaceId, It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldWriteAuditLog()
    {
        var request = new ReplayRequest
        {
            EventName = "test.replay",
            WorkspaceId = Guid.NewGuid(),
            MaxEventsPerSecond = 0,
        };

        var strategy = new TestStrategy(1);

        await _sut.ExecuteAsync(request, strategy);

        _auditLogMock.Verify(a => a.StartAsync(It.IsAny<ReplayRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _auditLogMock.Verify(a => a.UpdateAsync(It.IsAny<long>(), It.IsAny<ReplayResult>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldTrackFailedPublishes()
    {
        _runtimeMock.Setup(r => r.PublishAsync(It.IsAny<EventPublication>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MessagingResult { Success = false, Errors = ["fail"] });

        var request = new ReplayRequest
        {
            EventName = "test.replay",
            WorkspaceId = Guid.NewGuid(),
            MaxEventsPerSecond = 0,
        };

        var strategy = new TestStrategy(3);

        var result = await _sut.ExecuteAsync(request, strategy);

        result.TotalPublished.Should().Be(0);
        result.TotalFailed.Should().Be(3);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnCancelled_WhenCancelledViaToken()
    {
        var request = new ReplayRequest
        {
            EventName = "test.replay",
            WorkspaceId = Guid.NewGuid(),
            MaxEventsPerSecond = 0,
        };

        var strategy = new TestStrategy(10);
        using var cts = new CancellationTokenSource();

        // Cancel after a short delay so the engine starts processing
        cts.CancelAfter(TimeSpan.FromMilliseconds(1));

        var result = await _sut.ExecuteAsync(request, strategy, cts.Token);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("cancelled");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHandleException_FromPublish()
    {
        _runtimeMock.Setup(r => r.PublishAsync(It.IsAny<EventPublication>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("runtime error"));

        var request = new ReplayRequest
        {
            EventName = "test.replay",
            WorkspaceId = Guid.NewGuid(),
            MaxEventsPerSecond = 0,
        };

        var strategy = new TestStrategy(2);

        var result = await _sut.ExecuteAsync(request, strategy);

        result.TotalFailed.Should().Be(2);
        result.TotalPublished.Should().Be(0);
    }
}

file sealed class TestStrategy : IReplayStrategy
{
    private readonly int _count;

    public TestStrategy(int count)
    {
        _count = count;
    }

    public ReplayStrategyType StrategyType => ReplayStrategyType.Latest;

    public async IAsyncEnumerable<EventPublication> GetEventsAsync(
        ReplayRequest request,
        IReplayCheckpointStore checkpointStore,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (var i = 0; i < _count; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            yield return new EventPublication
            {
                Event = new { Id = i },
                Context = new PublishContext
                {
                    CorrelationId = Guid.NewGuid(),
                    WorkspaceId = request.WorkspaceId,
                    OccurredAt = DateTimeOffset.UtcNow,
                },
            };

            await Task.CompletedTask;
        }
    }
}
