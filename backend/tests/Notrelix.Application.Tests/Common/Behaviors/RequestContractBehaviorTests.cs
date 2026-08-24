using FluentValidation;

namespace Notrelix.Application.Tests.Common.Behaviors;

public sealed class RequestContractBehaviorTests
{
    private sealed record ValidRequest(string Value)
        : IRequest<string>, IAuthenticatedRequest, IGlobalRequest, IReadRequest;

    private sealed record TokenRequest(string Token)
        : IRequest<string>, IAnonymousRequest, ITokenScopedRequest, IReadRequest
    {
        public TokenPurpose TokenPurpose => TokenPurpose.WorkspaceInvitation;
    }

    private sealed record VersionedRequest(long ExpectedVersion)
        : IRequest<string>, IAuthenticatedRequest, IResourceScopedRequest, IWriteRequest, IExpectedVersionRequest
    {
        public ResourceRef Resource => ResourceRef.Create(
            ResourceKind.Create("work-management.board"), Guid.NewGuid());
    }

    private sealed record IdempotentRequest
        : IRequest<string>, IAuthenticatedRequest, IGlobalRequest, IWriteRequest, IIdempotentRequest;

    [Fact]
    public async Task Valid_request_reaches_next_once()
    {
        var nextCalls = 0;
        var behavior = CreateBehavior<ValidRequest>([]);

        var result = await behavior.Handle(
            new ValidRequest("ok"),
            _ =>
            {
                nextCalls++;
                return Task.FromResult("handled");
            },
            CancellationToken.None);

        result.Should().Be("handled");
        nextCalls.Should().Be(1);
    }

    [Fact]
    public async Task Fluent_validation_failure_does_not_reach_next()
    {
        var validator = new InlineValidator<ValidRequest>();
        validator.RuleFor(request => request.Value).NotEmpty();
        var behavior = CreateBehavior<ValidRequest>([validator]);
        var nextCalls = 0;

        var act = () => behavior.Handle(
            new ValidRequest(string.Empty),
            _ =>
            {
                nextCalls++;
                return Task.FromResult("handled");
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.ValidationException>();
        nextCalls.Should().Be(0);
    }

    [Fact]
    public async Task Blank_token_fails_before_next()
    {
        var behavior = CreateBehavior<TokenRequest>([]);

        var act = () => behavior.Handle(
            new TokenRequest(" "),
            _ => Task.FromResult("handled"),
            CancellationToken.None);

        await act.Should().ThrowAsync<SecurityMisconfigurationException>()
            .WithMessage("*non-empty token*");
    }

    [Fact]
    public async Task Invalid_expected_version_fails_before_next()
    {
        var behavior = CreateBehavior<VersionedRequest>([]);

        var act = () => behavior.Handle(
            new VersionedRequest(0),
            _ => Task.FromResult("handled"),
            CancellationToken.None);

        await act.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.ValidationException>()
            .WithMessage("*ExpectedVersion must be a positive value*");
    }

    [Fact]
    public async Task Missing_idempotency_key_fails_before_next()
    {
        var context = new IdempotencyExecutionContext();
        var behavior = CreateBehavior<IdempotentRequest>([], context);

        var act = () => behavior.Handle(
            new IdempotentRequest(),
            _ => Task.FromResult("handled"),
            CancellationToken.None);

        await act.Should().ThrowAsync<IdempotencyContextMissingException>();
    }

    private static RequestContractBehavior<TRequest, string> CreateBehavior<TRequest>(
        IEnumerable<IValidator<TRequest>> validators,
        IIdempotencyExecutionContext? idempotencyContext = null)
        where TRequest : IRequest<string>
    {
        var descriptor = RequestDescriptorValidator.Create(typeof(TRequest));
        var registry = new Mock<IRequestDescriptorRegistry>();
        registry.Setup(candidate => candidate.GetRequired(typeof(TRequest))).Returns(descriptor);

        return new RequestContractBehavior<TRequest, string>(
            registry.Object,
            validators,
            idempotencyContext ?? Mock.Of<IIdempotencyExecutionContext>());
    }
}
