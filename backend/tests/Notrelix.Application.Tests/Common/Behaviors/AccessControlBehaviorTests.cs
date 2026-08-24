namespace Notrelix.Application.Tests.Common.Behaviors;

public sealed class AccessControlBehaviorTests
{
    private sealed record AnonymousRequest : IRequest<string>, IAnonymousRequest, IGlobalRequest, INoDataRequest;

    private sealed record VerifiedRequest
        : IRequest<string>, IAuthenticatedRequest, IGlobalRequest, INoDataRequest, IRequireVerifiedEmail;

    [Fact]
    public async Task Anonymous_unprotected_request_avoids_facts_io()
    {
        var fixture = CreateFixture<AnonymousRequest>();

        var result = await fixture.Behavior.Handle(
            new AnonymousRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
        fixture.Provider.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task Verified_email_policy_matches_fact(bool verified, bool allowed)
    {
        var fixture = CreateFixture<VerifiedRequest>();
        fixture.Provider.Setup(provider => provider.ResolveAsync(
                It.IsAny<RequestDescriptor>(), It.IsAny<ExecutionContextSnapshot>(),
                It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Facts(userExists: true, emailVerified: verified));

        var act = () => fixture.Behavior.Handle(
            new VerifiedRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

        if (allowed)
        {
            (await act()).Should().Be("ok");
        }
        else
        {
            await act.Should().ThrowAsync<ForbiddenException>()
                .WithMessage("*Email must be confirmed*");
        }

        fixture.Provider.Verify(provider => provider.ResolveAsync(
            It.IsAny<RequestDescriptor>(), It.IsAny<ExecutionContextSnapshot>(),
            It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Policy_evaluator_is_pure_by_constructor_contract()
    {
        typeof(AccessPolicyEngine).GetConstructors().Should().ContainSingle();
        typeof(AccessPolicyEngine).GetConstructors().Single().GetParameters().Should().BeEmpty();
    }

    private static Fixture<TRequest> CreateFixture<TRequest>() where TRequest : IRequest<string>
    {
        var descriptor = RequestDescriptorValidator.Create(typeof(TRequest));
        var descriptors = new Mock<IRequestDescriptorRegistry>();
        descriptors.Setup(registry => registry.GetRequired(typeof(TRequest))).Returns(descriptor);
        var context = new Mock<IExecutionContextReader>();
        context.SetupGet(reader => reader.Snapshot).Returns(new ExecutionContextSnapshot(
            descriptor.Principal == ApplicationPrincipalKind.Anonymous ? null : Guid.NewGuid(),
            null, null, null, descriptor.Principal, descriptor.Scope, Guid.NewGuid().ToString("D")));
        var provider = new Mock<IAccessFactsProvider>();

        return new Fixture<TRequest>(
            new AccessControlBehavior<TRequest, string>(
                descriptors.Object, context.Object, provider.Object, new AccessPolicyEngine()),
            provider);
    }

    private static AccessFacts Facts(bool userExists = false, bool emailVerified = false) => new(
        userExists, emailVerified, false, null, false, null, false, null, null, false, [], false, null, false);

    private sealed record Fixture<TRequest>(
        AccessControlBehavior<TRequest, string> Behavior,
        Mock<IAccessFactsProvider> Provider)
        where TRequest : IRequest<string>;
}
