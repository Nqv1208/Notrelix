using Notrelix.Application.Common.CQRS.Execution;
using Notrelix.Application.Common.CQRS.Scoping;
using Notrelix.Application.Features.Identity.Registration.Commands.Register;

namespace Notrelix.Application.Tests.Common.CQRS;

public class RequestExecutionClassifierTests
{
    private sealed record AnonymousGlobalTransactionalRequest : IRequest<string>, IAnonymousRequest, IGlobalRequest, ITransactionalRequest;

    private sealed record WorkspaceTransactionalRequest : IRequest<string>, IWorkspaceRequest, ITransactionalRequest
    {
        public Guid WorkspaceId => Guid.NewGuid();
    }

    private sealed record ResourcePermissionReadRequest : IRequest<string>, IResourceScopedRequest, IRequirePermission, IRlsReadRequest
    {
        public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, Guid.NewGuid());
        public PermissionAction Action => PermissionAction.ViewBoard;
    }

    private sealed record PublicCacheGlobalRequest : IRequest<string>, IPublicCacheableQuery<string>, IGlobalRequest
    {
        public string CacheKey => "test";
        public TimeSpan? Ttl => null;
    }

    private sealed record GlobalPermissionRequest : IRequest<string>, IGlobalRequest, IRequirePermission
    {
        public PermissionAction Action => PermissionAction.ViewBoard;
        public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, Guid.NewGuid());
    }

    private sealed record SystemInternalRequest : IRequest<string>, ISystemInternalRequest
    {
        public UseCaseSecurityKind SecurityKind => UseCaseSecurityKind.SystemInternal;
    }

    private sealed record AnonymousSystemInternalRequest : IRequest<string>, IAnonymousRequest, ISystemInternalRequest
    {
        public UseCaseSecurityKind SecurityKind => UseCaseSecurityKind.SystemInternal;
    }

    private sealed record SubscriptionRequest : IRequest<string>, IWorkspaceRequest, IRequireSubscription
    {
        public Guid WorkspaceId => Guid.NewGuid();
        public string? MinimumTier => null;
    }

    private sealed record FeatureRequest : IRequest<string>, IAccountRequest, IRequireFeature
    {
        public Guid AccountId => Guid.NewGuid();
        public string FeatureCode => "test_feature";
        public int Amount => 1;
    }

    private sealed record AuthorizedCacheRequest : IRequest<string>, IAuthorizedCacheableRequest, IWorkspaceRequest
    {
        public Guid WorkspaceId => Guid.NewGuid();
        public string AuthorizedCacheKey => "test";
        public TimeSpan AuthorizedCacheTtl => TimeSpan.FromMinutes(5);
    }

    // --- Tests ---

    [Fact]
    public void RegisterCommand_Profile_Is_Anonymous_Global_Transactional_NoRls()
    {
        var request = new RegisterCommand
        {
            Email = "test@example.com",
            Password = "Password123!",
            Name = "Test User"
        };

        var profile = RequestExecutionClassifier.Classify(request);

        profile.IsAnonymous.Should().BeTrue();
        profile.IsGlobal.Should().BeTrue();
        profile.IsTransactional.Should().BeTrue();
        profile.RequiresRls.Should().BeFalse();
        profile.NeedsDbScope.Should().BeTrue();
        profile.IsReadOnlyDbScope.Should().BeFalse();
        profile.IsSystemInternal.Should().BeFalse();
        profile.IsTenantScoped.Should().BeFalse();
        profile.IsPublicCacheable.Should().BeFalse();
        profile.IsAuthorizedCacheable.Should().BeFalse();
    }

    [Fact]
    public void AnonymousGlobalTransactional_Profile_Matches_Expected()
    {
        var profile = Classify(new AnonymousGlobalTransactionalRequest());

        profile.IsAnonymous.Should().BeTrue();
        profile.IsGlobal.Should().BeTrue();
        profile.IsTransactional.Should().BeTrue();
        profile.RequiresRls.Should().BeFalse();
        profile.IsReadOnlyDbScope.Should().BeFalse();
        profile.IsSystemInternal.Should().BeFalse();
        profile.IsTenantScoped.Should().BeFalse();
    }

    [Fact]
    public void WorkspaceTransactional_Profile_RequiresRls()
    {
        var profile = Classify(new WorkspaceTransactionalRequest());

        profile.IsWorkspaceScoped.Should().BeTrue();
        profile.IsTransactional.Should().BeTrue();
        profile.RequiresRls.Should().BeTrue();
        profile.IsTenantScoped.Should().BeTrue();
        profile.NeedsDbScope.Should().BeTrue();
        profile.IsReadOnlyDbScope.Should().BeFalse();
        profile.IsGlobal.Should().BeFalse();
        profile.IsAnonymous.Should().BeFalse();
    }

    [Fact]
    public void ResourcePermissionRead_Profile_RequiresRls()
    {
        var profile = Classify(new ResourcePermissionReadRequest());

        profile.IsResourceScoped.Should().BeTrue();
        profile.RequiresPermission.Should().BeTrue();
        profile.IsRlsRead.Should().BeTrue();
        profile.RequiresRls.Should().BeTrue();
        profile.NeedsDbScope.Should().BeTrue();
        profile.IsReadOnlyDbScope.Should().BeTrue();
        profile.IsTransactional.Should().BeFalse();
        profile.IsGlobal.Should().BeFalse();
    }

    [Fact]
    public void PublicCacheGlobal_Profile_NoTenant_NoDbScope()
    {
        var profile = Classify(new PublicCacheGlobalRequest());

        profile.IsPublicCacheable.Should().BeTrue();
        profile.IsGlobal.Should().BeTrue();
        profile.IsTenantScoped.Should().BeFalse();
        profile.RequiresRls.Should().BeFalse();
        profile.NeedsDbScope.Should().BeFalse();
    }

    [Fact]
    public void GlobalPermission_Profile_RequiresRls()
    {
        var profile = Classify(new GlobalPermissionRequest());

        profile.IsGlobal.Should().BeTrue();
        profile.RequiresPermission.Should().BeTrue();
        profile.RequiresRls.Should().BeTrue();
        profile.IsTenantScoped.Should().BeFalse();
        profile.NeedsDbScope.Should().BeTrue();

        // Guard should reject this combination: Global + RequiresPermission
        profile.IsGlobal.Should().BeTrue();
        profile.RequiresPermission.Should().BeTrue();
    }

    [Fact]
    public void SystemInternal_Profile_IsSystemInternal()
    {
        var profile = Classify(new SystemInternalRequest());

        profile.IsSystemInternal.Should().BeTrue();
        profile.IsAnonymous.Should().BeFalse();
        profile.IsGlobal.Should().BeFalse();
        profile.RequiresRls.Should().BeFalse();
        profile.NeedsDbScope.Should().BeFalse();
    }

    [Fact]
    public void AnonymousSystemInternal_Profile_HasBothMarkers()
    {
        var profile = Classify(new AnonymousSystemInternalRequest());

        profile.IsAnonymous.Should().BeTrue();
        profile.IsSystemInternal.Should().BeTrue();

        // Guard should reject this combination
    }

    [Fact]
    public void SubscriptionRequest_Profile_RequiresRls()
    {
        var profile = Classify(new SubscriptionRequest());

        profile.RequiresSubscription.Should().BeTrue();
        profile.IsWorkspaceScoped.Should().BeTrue();
        profile.RequiresRls.Should().BeTrue();
    }

    [Fact]
    public void FeatureRequest_Profile_RequiresRls()
    {
        var profile = Classify(new FeatureRequest());

        profile.RequiresFeature.Should().BeTrue();
        profile.IsAccountScoped.Should().BeTrue();
        profile.RequiresRls.Should().BeTrue();
    }

    [Fact]
    public void AuthorizedCacheable_Profile_IsAuthorizedCacheable()
    {
        var profile = Classify(new AuthorizedCacheRequest());

        profile.IsAuthorizedCacheable.Should().BeTrue();
        profile.IsWorkspaceScoped.Should().BeTrue();
        profile.RequiresRls.Should().BeTrue();
    }

    [Fact]
    public void DefaultProfile_Properties_HaveCorrectDefaults()
    {
        var profile = Classify(new AnonymousGlobalTransactionalRequest());

        profile.RequiresSubscription.Should().BeFalse();
        profile.RequiresFeature.Should().BeFalse();
        profile.IsAccountScoped.Should().BeFalse();
        profile.IsWorkspaceScoped.Should().BeFalse();
        profile.IsResourceScoped.Should().BeFalse();
        profile.IsRlsRead.Should().BeFalse();
    }

    // --- Helpers ---

    private static RequestExecutionProfile Classify<TRequest>(TRequest request)
        where TRequest : notnull
    {
        return RequestExecutionClassifier.Classify(request);
    }
}
