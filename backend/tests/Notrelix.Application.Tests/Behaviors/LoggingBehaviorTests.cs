using Notrelix.Application.Common.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace Notrelix.Application.Tests.Behaviors;

public class ApplicationTracingBehaviorTests
{
    public sealed record TestRequest;
    public sealed record TestResponse(string Value);

    private static IExecutionContextReader CreateMockExecutionContext()
    {
        var ctx = new Notrelix.Application.Common.Context.ExecutionContext();
        ctx.SetUser(Guid.NewGuid(), "test@test.com", "Test User");
        ctx.SetTenant(Guid.NewGuid(), Guid.NewGuid());
        return ctx;
    }

    private static Mock<IRequestDescriptorRegistry> CreateMockDescriptors()
    {
        var descriptors = new Mock<IRequestDescriptorRegistry>();
        descriptors
            .Setup(d => d.GetRequired(typeof(TestRequest)))
            .Returns(new RequestDescriptor(
                typeof(TestRequest),
                ApplicationRequestKind.Command,
                ApplicationPrincipalKind.Authenticated,
                ApplicationScopeKind.Workspace,
                ApplicationDataAccessKind.Read,
                new AccessRequirements(false, false, false, false),
                IsIdempotent: false,
                RequiresExpectedVersion: false));
        return descriptors;
    }

    [Fact]
    public async Task Handle_WhenExecuted_LogsAtEntryAndExit()
    {
        var logger = new Mock<ILogger<ApplicationTracingBehavior<TestRequest, TestResponse>>>();
        var executionContext = CreateMockExecutionContext();
        var behavior = new ApplicationTracingBehavior<TestRequest, TestResponse>(
            CreateMockDescriptors().Object, logger.Object, executionContext, new Mock<IHostEnvironment>().Object, new PipelineMetrics());

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
        var logger = new Mock<ILogger<ApplicationTracingBehavior<TestRequest, TestResponse>>>();
        var executionContext = CreateMockExecutionContext();
        var behavior = new ApplicationTracingBehavior<TestRequest, TestResponse>(
            CreateMockDescriptors().Object, logger.Object, executionContext, new Mock<IHostEnvironment>().Object, new PipelineMetrics());

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
