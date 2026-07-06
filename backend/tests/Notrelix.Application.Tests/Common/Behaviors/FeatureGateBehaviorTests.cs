namespace Notrelix.Application.Tests.Common.Behaviors;

public class FeatureGateBehaviorTests
{
    private const string TestFeature = "automation";

    public sealed record FeatureEnabledRequest : IRequest<string>, IRequireFeature
    {
        public string FeatureCode => TestFeature;
        public int Amount => 1;
    }

    public sealed record NoFeatureRequest : IRequest<string>;

    private static Mock<IExecutionContextReader> CreateContextWithAccount(Guid? accountId)
    {
        var ctx = new Mock<IExecutionContextReader>();
        ctx.Setup(x => x.AccountId).Returns(accountId);
        return ctx;
    }

    private static FeatureGateBehavior<TRequest, string> CreateBehavior<TRequest>(
        Mock<IExecutionContextReader>? context = null,
        Mock<IFeatureGateChecker>? checker = null)
        where TRequest : notnull
    {
        return new FeatureGateBehavior<TRequest, string>(
            checker?.Object ?? Mock.Of<IFeatureGateChecker>(),
            context?.Object ?? Mock.Of<IExecutionContextReader>(),
            Mock.Of<ILogger<FeatureGateBehavior<TRequest, string>>>());
    }

    [Fact]
    public async Task NonFeatureRequest_PassesThrough()
    {
        var behavior = CreateBehavior<NoFeatureRequest>();
        var handlerCalled = false;

        RequestHandlerDelegate<string> next = ct =>
        {
            handlerCalled = true;
            return Task.FromResult("ok");
        };

        var result = await behavior.Handle(new NoFeatureRequest(), next, CancellationToken.None);

        result.Should().Be("ok");
        handlerCalled.Should().BeTrue();
    }

    [Fact]
    public async Task MissingAccountId_ThrowsSecurityMisconfiguration()
    {
        var behavior = CreateBehavior<FeatureEnabledRequest>(
            context: CreateContextWithAccount(null));

        RequestHandlerDelegate<string> next = _ => Task.FromResult("should not reach");

        Func<Task> act = () => behavior.Handle(new FeatureEnabledRequest(), next, CancellationToken.None);

        await act.Should().ThrowAsync<SecurityMisconfigurationException>()
            .WithMessage("*no account context*");
    }

    [Fact]
    public async Task EmptyAccountId_ThrowsSecurityMisconfiguration()
    {
        var behavior = CreateBehavior<FeatureEnabledRequest>(
            context: CreateContextWithAccount(Guid.Empty));

        RequestHandlerDelegate<string> next = _ => Task.FromResult("should not reach");

        Func<Task> act = () => behavior.Handle(new FeatureEnabledRequest(), next, CancellationToken.None);

        await act.Should().ThrowAsync<SecurityMisconfigurationException>()
            .WithMessage("*no account context*");
    }

    [Fact]
    public async Task FeatureEnabled_AllowsPassThrough()
    {
        var checker = new Mock<IFeatureGateChecker>();
        checker.Setup(x => x.IsFeatureEnabledAsync(
                It.IsAny<Guid>(), TestFeature, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var behavior = CreateBehavior<FeatureEnabledRequest>(
            context: CreateContextWithAccount(Guid.NewGuid()),
            checker: checker);

        RequestHandlerDelegate<string> next = _ => Task.FromResult("allowed");
        var result = await behavior.Handle(new FeatureEnabledRequest(), next, CancellationToken.None);

        result.Should().Be("allowed");
    }

    [Fact]
    public async Task FeatureDisabled_ThrowsForbidden()
    {
        var checker = new Mock<IFeatureGateChecker>();
        checker.Setup(x => x.IsFeatureEnabledAsync(
                It.IsAny<Guid>(), TestFeature, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var behavior = CreateBehavior<FeatureEnabledRequest>(
            context: CreateContextWithAccount(Guid.NewGuid()),
            checker: checker);

        RequestHandlerDelegate<string> next = _ => Task.FromResult("should not reach");

        Func<Task> act = () => behavior.Handle(new FeatureEnabledRequest(), next, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*not enabled*");
    }
}
