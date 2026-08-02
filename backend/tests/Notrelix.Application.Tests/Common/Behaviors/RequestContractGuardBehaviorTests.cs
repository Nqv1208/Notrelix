namespace Notrelix.Application.Tests.Common.Behaviors;

public class RequestContractGuardBehaviorTests
{
    // --- Test request types ---

    private sealed record GlobalWorkspaceRequest : IRequest<string>, IGlobalRequest, IWorkspaceRequest
    {
        public Guid WorkspaceId => Guid.NewGuid();
    }

    private sealed record GlobalResourceRequest : IRequest<string>, IGlobalRequest, IResourceScopedRequest
    {
        public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, Guid.NewGuid());
    }

    private sealed record GlobalPermissionRequest : IRequest<string>, IGlobalRequest, IRequirePermission
    {
        public PermissionAction Action => PermissionAction.ViewBoard;
        public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, Guid.NewGuid());
    }

    private sealed record AnonymousWorkspaceRequest : IRequest<string>, IAnonymousRequest, IWorkspaceRequest
    {
        public Guid WorkspaceId => Guid.NewGuid();
    }

    private sealed record AnonymousResourceRequest : IRequest<string>, IAnonymousRequest, IResourceScopedRequest
    {
        public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, Guid.NewGuid());
    }

    private sealed record PublicCacheWorkspaceRequest : IRequest<string>, IPublicCacheableQuery<string>, IWorkspaceRequest
    {
        public Guid WorkspaceId => Guid.NewGuid();
        public object CacheIdentity => "test";
        public TimeSpan? Ttl => null;
    }

    private sealed record PublicCacheAuthorizedCacheRequest : IRequest<string>, IPublicCacheableQuery<string>, IAuthorizedCacheableRequest, IGlobalRequest
    {
        public object CacheIdentity => "test-public";
        public TimeSpan? Ttl => null;
        public AuthorizedCacheScope CacheScope => AuthorizedCacheScope.Workspace;
        object IAuthorizedCacheableRequest.CacheIdentity => "test-authorized";
        TimeSpan? IAuthorizedCacheableRequest.CacheTtl => TimeSpan.FromMinutes(5);
    }

    private sealed record ValidAnonymousGlobalRequest : IRequest<string>, IAnonymousRequest, IGlobalRequest;

    private sealed record ValidPermissionRequest : IRequest<string>, IWorkspaceRequest, IRequirePermission
    {
        public Guid WorkspaceId => Guid.NewGuid();
        public PermissionAction Action => PermissionAction.ViewBoard;
        public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, Guid.NewGuid());
    }

    // --- Helpers ---

    private static RequestContractGuardBehavior<T, string> CreateBehavior<T>()
        where T : IRequest<string>
        => new();

    // --- Test: Global + Scope violations ---

    [Fact]
    public async Task GlobalWorkspaceRequest_ShouldThrow()
    {
        var behavior = CreateBehavior<GlobalWorkspaceRequest>();
        Func<Task> act = () => behavior.Handle(new GlobalWorkspaceRequest(), _ => Task.FromResult("ok"), CancellationToken.None);
        await act.Should().ThrowAsync<SecurityMisconfigurationException>()
            .WithMessage("*multiple scope markers*");
    }

    [Fact]
    public async Task GlobalResourceRequest_ShouldThrow()
    {
        var behavior = CreateBehavior<GlobalResourceRequest>();
        Func<Task> act = () => behavior.Handle(new GlobalResourceRequest(), _ => Task.FromResult("ok"), CancellationToken.None);
        await act.Should().ThrowAsync<SecurityMisconfigurationException>()
            .WithMessage("*multiple scope markers*");
    }

    [Fact]
    public async Task GlobalPermissionRequest_ShouldThrow()
    {
        var behavior = CreateBehavior<GlobalPermissionRequest>();
        Func<Task> act = () => behavior.Handle(new GlobalPermissionRequest(), _ => Task.FromResult("ok"), CancellationToken.None);
        await act.Should().ThrowAsync<SecurityMisconfigurationException>()
            .WithMessage("*Global request cannot require tenant/resource permission.*");
    }

    // --- Test: Anonymous + Scope violations ---

    [Fact]
    public async Task AnonymousWorkspaceRequest_ShouldThrow()
    {
        var behavior = CreateBehavior<AnonymousWorkspaceRequest>();
        Func<Task> act = () => behavior.Handle(new AnonymousWorkspaceRequest(), _ => Task.FromResult("ok"), CancellationToken.None);
        await act.Should().ThrowAsync<SecurityMisconfigurationException>()
            .WithMessage("*Anonymous request cannot be tenant/resource scoped.*");
    }

    [Fact]
    public async Task AnonymousResourceRequest_ShouldThrow()
    {
        var behavior = CreateBehavior<AnonymousResourceRequest>();
        Func<Task> act = () => behavior.Handle(new AnonymousResourceRequest(), _ => Task.FromResult("ok"), CancellationToken.None);
        await act.Should().ThrowAsync<SecurityMisconfigurationException>()
            .WithMessage("*Anonymous request cannot be tenant/resource scoped.*");
    }

    // --- Test: PublicCache violations ---

    [Fact]
    public async Task PublicCacheWorkspaceRequest_ShouldThrow()
    {
        var behavior = CreateBehavior<PublicCacheWorkspaceRequest>();
        Func<Task> act = () => behavior.Handle(new PublicCacheWorkspaceRequest(), _ => Task.FromResult("ok"), CancellationToken.None);
        await act.Should().ThrowAsync<SecurityMisconfigurationException>()
            .WithMessage("*Public cache cannot be used for tenant-scoped requests.*");
    }

    [Fact]
    public async Task PublicCacheAuthorizedCacheRequest_ShouldThrow()
    {
        var behavior = CreateBehavior<PublicCacheAuthorizedCacheRequest>();
        Func<Task> act = () => behavior.Handle(new PublicCacheAuthorizedCacheRequest(), _ => Task.FromResult("ok"), CancellationToken.None);
        await act.Should().ThrowAsync<SecurityMisconfigurationException>()
            .WithMessage("*both*cache*");
    }

    // --- Test: Valid requests pass ---

    [Fact]
    public async Task ValidAnonymousGlobalRequest_ShouldPass()
    {
        var behavior = CreateBehavior<ValidAnonymousGlobalRequest>();
        var result = await behavior.Handle(new ValidAnonymousGlobalRequest(), _ => Task.FromResult("ok"), CancellationToken.None);
        result.Should().Be("ok");
    }

    [Fact]
    public async Task ValidPermissionRequest_ShouldPass()
    {
        var behavior = CreateBehavior<ValidPermissionRequest>();
        var result = await behavior.Handle(new ValidPermissionRequest(), _ => Task.FromResult("ok"), CancellationToken.None);
        result.Should().Be("ok");
    }
}
