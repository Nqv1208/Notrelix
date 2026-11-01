using Notrelix.Domain.Governance.Permissions;

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

    // A tenant-scoped write with no permission would skip the access facts, and with them the suspended-account check.
    private sealed record WorkspaceWriteWithoutFacts
        : IRequest<string>, IAuthenticatedRequest, IWorkspaceRequest, IWriteRequest
    {
        public Guid WorkspaceId => Guid.Empty;
    }

    private sealed record AccountWriteWithoutFacts
        : IRequest<string>, IAuthenticatedRequest, IAccountRequest, IWriteRequest;

    private sealed record ResourceWriteWithoutFacts
        : IRequest<string>, IAuthenticatedRequest, IResourceScopedRequest, IWriteRequest
    {
        public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("test.resource"), Guid.Empty);
    }

    private sealed record WorkspaceWriteWithPermission
        : IRequest<string>, IAuthenticatedRequest, IWorkspaceRequest, IWriteRequest, IRequirePermission
    {
        public Guid WorkspaceId => Guid.Empty;
        public PermissionAction Action => PermissionAction.ManageBoard;
        public ResourceRef? Resource => null;
    }

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

    [Theory]
    [InlineData(typeof(WorkspaceWriteWithoutFacts))]
    [InlineData(typeof(AccountWriteWithoutFacts))]
    [InlineData(typeof(ResourceWriteWithoutFacts))]
    public void A_tenant_scoped_write_without_access_facts_is_refused_at_registration(Type requestType)
    {
        var act = () => RequestDescriptorValidator.Create(requestType);

        act.Should().Throw<SecurityMisconfigurationException>()
            .WithMessage("*tenant-scoped write must require a permission*");
    }

    [Fact]
    public void A_tenant_scoped_write_that_requires_a_permission_is_accepted()
    {
        var descriptor = RequestDescriptorValidator.Create(typeof(WorkspaceWriteWithPermission));

        descriptor.Access.RequiresDatastoreFacts.Should().BeTrue();
    }

    [Theory]
    [InlineData(ApplicationScopeKind.Account)]
    [InlineData(ApplicationScopeKind.Workspace)]
    [InlineData(ApplicationScopeKind.Resource)]
    public void The_policy_engine_refuses_a_tenant_scoped_write_that_loaded_no_facts(ApplicationScopeKind scope)
    {
        // A hand-built descriptor (the validator would have refused it) must still never be waved through.
        var descriptor = new RequestDescriptor(
            typeof(WorkspaceWriteWithoutFacts),
            ApplicationRequestKind.Command,
            ApplicationPrincipalKind.Authenticated,
            scope,
            ApplicationDataAccessKind.Write,
            new AccessRequirements(false, false, false, false),
            false,
            false);
        var context = new ExecutionContextSnapshot(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
            ApplicationPrincipalKind.Authenticated, scope, "corr");

        var decision = new AccessPolicyEngine().Evaluate(
            descriptor, context, new AccessFacts(false, false, false, null, false, null, false, null, null, false, [], false, false, null, null),
            new WorkspaceWriteWithoutFacts());

        decision.Kind.Should().Be(AccessDecisionKind.SecurityMisconfiguration);
    }
}
