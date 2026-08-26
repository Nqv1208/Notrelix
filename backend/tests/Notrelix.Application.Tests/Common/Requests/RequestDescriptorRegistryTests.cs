namespace Notrelix.Application.Tests.Common.Requests;

public sealed class RequestDescriptorRegistryTests
{
    private sealed record ValidRequest : IRequest<string>, IAuthenticatedRequest, IGlobalRequest, IReadRequest;

    private sealed record MissingPrincipalRequest : IRequest<string>, IGlobalRequest, IReadRequest;

    private sealed record MultiplePrincipalsRequest
        : IRequest<string>, IAnonymousRequest, IAuthenticatedRequest, IGlobalRequest, IReadRequest;

    private sealed record MissingDataRequest : IRequest<string>, IAuthenticatedRequest, IGlobalRequest;

    private sealed record MultipleDataRequest
        : IRequest<string>, IAuthenticatedRequest, IGlobalRequest, IReadRequest, IWriteRequest;

    private sealed record MissingScopeRequest : IRequest<string>, IAuthenticatedRequest, IReadRequest;

    private sealed record MultipleScopesRequest
        : IRequest<string>, IAuthenticatedRequest, IGlobalRequest, IAccountRequest, IReadRequest;

    private sealed record IdempotentQuery
        : IQuery<string>, IAuthenticatedRequest, IGlobalRequest, IReadRequest, IIdempotentRequest;

    private sealed record InvalidTokenRequest
        : IRequest<string>, IAnonymousRequest, ITokenScopedRequest, INoDataRequest
    {
        public TokenPurpose TokenPurpose => TokenPurpose.ShareLink;
    }

    [Fact]
    public void Registry_resolves_the_same_immutable_descriptor_for_repeated_lookups()
    {
        var descriptor = RequestDescriptorValidator.Create(typeof(ValidRequest));
        var registry = RequestDescriptorRegistry.Create(typeof(ICommand<>).Assembly);

        descriptor.Principal.Should().Be(ApplicationPrincipalKind.Authenticated);
        descriptor.Scope.Should().Be(ApplicationScopeKind.Global);
        descriptor.DataAccess.Should().Be(ApplicationDataAccessKind.Read);

        var productionType = registry.Descriptors.First().RequestType;
        registry.GetRequired(productionType).Should().BeSameAs(registry.GetRequired(productionType));
    }

    [Theory]
    [InlineData(typeof(MissingPrincipalRequest), "*Exactly one principal marker is required; found 0*")]
    [InlineData(typeof(MultiplePrincipalsRequest), "*Exactly one principal marker is required; found 2*")]
    [InlineData(typeof(MissingDataRequest), "*Exactly one data access marker is required; found 0*")]
    [InlineData(typeof(MultipleDataRequest), "*Exactly one data access marker is required; found 2*")]
    [InlineData(typeof(MissingScopeRequest), "*Exactly one scope marker is required; found 0*")]
    [InlineData(typeof(MultipleScopesRequest), "*Exactly one scope marker is required; found 2*")]
    [InlineData(typeof(IdempotentQuery), "*Idempotency is valid only for write commands*")]
    [InlineData(typeof(InvalidTokenRequest), "*public string Token property*")]
    public void Invalid_request_contract_fails_closed(Type requestType, string expectedMessage)
    {
        var act = () => RequestDescriptorValidator.Create(requestType);

        act.Should().Throw<SecurityMisconfigurationException>().WithMessage(expectedMessage);
    }
}
