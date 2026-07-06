namespace Notrelix.Application.Tests.Common.Behaviors;

public class SubscriptionGateBehaviorTests
{
    private const string FreeTier = "Free";
    private const string ProTier = "Pro";

    public sealed record FreeTierRequest : IRequest<string>, IRequireSubscription
    {
        public string? MinimumTier => null;
    }

    public sealed record ProTierRequest : IRequest<string>, IRequireSubscription
    {
        public string? MinimumTier => ProTier;
    }

    public sealed record NoSubscriptionRequest : IRequest<string>;

    private static Mock<IExecutionContextReader> CreateContextWithAccount(Guid? accountId)
    {
        var ctx = new Mock<IExecutionContextReader>();
        ctx.Setup(x => x.AccountId).Returns(accountId);
        return ctx;
    }

    private static SubscriptionGateBehavior<TRequest, string> CreateBehavior<TRequest>(
        Mock<IExecutionContextReader>? context = null,
        Mock<ISubscriptionChecker>? checker = null)
        where TRequest : notnull
    {
        return new SubscriptionGateBehavior<TRequest, string>(
            checker?.Object ?? Mock.Of<ISubscriptionChecker>(),
            context?.Object ?? Mock.Of<IExecutionContextReader>(),
            Mock.Of<ILogger<SubscriptionGateBehavior<TRequest, string>>>());
    }

    [Fact]
    public async Task NonSubscriptionRequest_PassesThrough()
    {
        var behavior = CreateBehavior<NoSubscriptionRequest>();
        var handlerCalled = false;

        RequestHandlerDelegate<string> next = ct =>
        {
            handlerCalled = true;
            return Task.FromResult("ok");
        };

        var result = await behavior.Handle(new NoSubscriptionRequest(), next, CancellationToken.None);

        result.Should().Be("ok");
        handlerCalled.Should().BeTrue();
    }

    [Fact]
    public async Task MissingAccountId_ThrowsSecurityMisconfiguration()
    {
        var behavior = CreateBehavior<FreeTierRequest>(
            context: CreateContextWithAccount(null));

        RequestHandlerDelegate<string> next = _ => Task.FromResult("should not reach");

        Func<Task> act = () => behavior.Handle(new FreeTierRequest(), next, CancellationToken.None);

        await act.Should().ThrowAsync<SecurityMisconfigurationException>()
            .WithMessage("*no account context*");
    }

    [Fact]
    public async Task EmptyAccountId_ThrowsSecurityMisconfiguration()
    {
        var behavior = CreateBehavior<FreeTierRequest>(
            context: CreateContextWithAccount(Guid.Empty));

        RequestHandlerDelegate<string> next = _ => Task.FromResult("should not reach");

        Func<Task> act = () => behavior.Handle(new FreeTierRequest(), next, CancellationToken.None);

        await act.Should().ThrowAsync<SecurityMisconfigurationException>()
            .WithMessage("*no account context*");
    }

    [Fact]
    public async Task ActiveSubscription_AllowsPassThrough()
    {
        var checker = new Mock<ISubscriptionChecker>();
        checker.Setup(x => x.HasActiveSubscriptionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var behavior = CreateBehavior<FreeTierRequest>(
            context: CreateContextWithAccount(Guid.NewGuid()),
            checker: checker);

        RequestHandlerDelegate<string> next = _ => Task.FromResult("allowed");
        var result = await behavior.Handle(new FreeTierRequest(), next, CancellationToken.None);

        result.Should().Be("allowed");
        checker.Verify(x => x.HasActiveSubscriptionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NoActiveSubscription_ThrowsForbidden()
    {
        var checker = new Mock<ISubscriptionChecker>();
        checker.Setup(x => x.HasActiveSubscriptionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var behavior = CreateBehavior<FreeTierRequest>(
            context: CreateContextWithAccount(Guid.NewGuid()),
            checker: checker);

        RequestHandlerDelegate<string> next = _ => Task.FromResult("should not reach");

        Func<Task> act = () => behavior.Handle(new FreeTierRequest(), next, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*active subscription*");
    }

    [Fact]
    public async Task MinimumTierMet_AllowsPassThrough()
    {
        var checker = new Mock<ISubscriptionChecker>();
        checker.Setup(x => x.HasMinimumTierAsync(It.IsAny<Guid>(), ProTier, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var behavior = CreateBehavior<ProTierRequest>(
            context: CreateContextWithAccount(Guid.NewGuid()),
            checker: checker);

        RequestHandlerDelegate<string> next = _ => Task.FromResult("allowed");
        var result = await behavior.Handle(new ProTierRequest(), next, CancellationToken.None);

        result.Should().Be("allowed");
    }

    [Fact]
    public async Task MinimumTierNotMet_ThrowsForbidden()
    {
        var checker = new Mock<ISubscriptionChecker>();
        checker.Setup(x => x.HasMinimumTierAsync(It.IsAny<Guid>(), ProTier, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var behavior = CreateBehavior<ProTierRequest>(
            context: CreateContextWithAccount(Guid.NewGuid()),
            checker: checker);

        RequestHandlerDelegate<string> next = _ => Task.FromResult("should not reach");

        Func<Task> act = () => behavior.Handle(new ProTierRequest(), next, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*subscription tier*");
    }
}
