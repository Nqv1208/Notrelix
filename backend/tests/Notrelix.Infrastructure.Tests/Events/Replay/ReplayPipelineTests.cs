using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Infrastructure.Events.Replay;

namespace Notrelix.Infrastructure.Tests.Events.Replay;

public sealed class ReplayPipelineTests
{
    [Fact]
    public async Task ExecuteAsync_WhenAuthorized_FailsClosedWithoutRetainedEventSource()
    {
        var pipeline = new ReplayPipeline(NullLogger<ReplayPipeline>.Instance);

        var result = await pipeline.ExecuteAsync(new ReplayRequest
        {
            EventName = "board.item.created",
            EventVersion = 1,
            RequestedBy = "system-test",
            RequestedAt = DateTimeOffset.UtcNow,
            Authorized = true,
        });

        result.Success.Should().BeFalse();
        result.EventsReplayed.Should().Be(0);
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("no retained event source");
    }

    [Fact]
    public async Task ExecuteAsync_WhenUnauthorized_RejectsBeforeCapabilityCheck()
    {
        var pipeline = new ReplayPipeline(NullLogger<ReplayPipeline>.Instance);

        var result = await pipeline.ExecuteAsync(new ReplayRequest
        {
            EventName = "board.item.created",
            EventVersion = 1,
            RequestedBy = "system-test",
            RequestedAt = DateTimeOffset.UtcNow,
            Authorized = false,
        });

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle("Replay not authorized.");
    }
}
