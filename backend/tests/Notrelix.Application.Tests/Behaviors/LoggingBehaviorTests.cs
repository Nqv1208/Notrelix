using Microsoft.Extensions.Logging;
using Notrelix.Application.Common.Behaviors;

namespace Notrelix.Application.Tests.Behaviors;

public class LoggingBehaviorTests
{
    public sealed record TestRequest;
    public sealed record TestResponse(string Value);

    [Fact]
    public async Task Handle_WhenExecuted_LogsAtEntryAndExit()
    {
        var logger = new Mock<ILogger<LoggingBehavior<TestRequest, TestResponse>>>();
        var behavior = new LoggingBehavior<TestRequest, TestResponse>(logger.Object);

        var response = await behavior.Handle(
            new TestRequest(), ct => Task.FromResult(new TestResponse("ok")), default);

        response.Value.Should().Be("ok");
        logger.Verify(
            x => x.Log(LogLevel.Information, It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v!.ToString()!.Contains("Handling")),
                It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        logger.Verify(
            x => x.Log(LogLevel.Information, It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v!.ToString()!.Contains("Handled")),
                It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenHandlerThrows_LogsException()
    {
        var logger = new Mock<ILogger<LoggingBehavior<TestRequest, TestResponse>>>();
        var behavior = new LoggingBehavior<TestRequest, TestResponse>(logger.Object);

        Func<Task> act = () => behavior.Handle(
            new TestRequest(),
            ct => throw new InvalidOperationException("boom"),
            default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        logger.Verify(
            x => x.Log(LogLevel.Error, It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.Is<InvalidOperationException>(e => e.Message == "boom"),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
