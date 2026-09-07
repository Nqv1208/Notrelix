namespace Notrelix.Application.Tests.Common.Behaviors;

public sealed class ExecutionContextBehaviorTests
{
    private sealed record GlobalRequest : IRequest<string>, IAnonymousRequest, IGlobalRequest, INoDataRequest;

    private sealed record WorkspaceRequest(Guid WorkspaceId)
        : IRequest<string>, IAuthenticatedRequest, IWorkspaceRequest, IReadRequest;

    private sealed record ResourceRequest(ResourceRef Resource)
        : IRequest<string>, IAuthenticatedRequest, IResourceScopedRequest, IReadRequest;

    [Fact]
    public async Task Global_request_publishes_snapshot_without_locator_io()
    {
        var fixture = CreateFixture<GlobalRequest>();

        await fixture.Behavior.Handle(
            new GlobalRequest(),
            _ => Task.FromResult("ok"),
            CancellationToken.None);

        fixture.Locator.Verify(
            locator => locator.LocateAsync(It.IsAny<ResourceRef>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        fixture.ExecutionContext.Snapshot.Should().NotBeNull();
        fixture.ExecutionContext.Snapshot!.Scope.Should().Be(ApplicationScopeKind.Global);
    }

    [Fact]
    public async Task Workspace_request_resolves_tenant_from_bootstrap_without_locator()
    {
        var fixture = CreateFixture<WorkspaceRequest>();
        var workspaceId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        fixture.TenantBootstrap.Setup(store => store.ResolveWorkspaceAccessAsync(
                workspaceId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceAccessSnapshot(accountId, workspaceId, Guid.NewGuid(), true, true));

        await fixture.Behavior.Handle(
            new WorkspaceRequest(workspaceId),
            _ => Task.FromResult("ok"),
            CancellationToken.None);

        fixture.Locator.Verify(
            locator => locator.LocateAsync(It.IsAny<ResourceRef>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        fixture.ExecutionContext.Snapshot.Should().Match<ExecutionContextSnapshot>(snapshot =>
            snapshot.AccountId == accountId && snapshot.WorkspaceId == workspaceId);
    }

    [Fact]
    public async Task Resource_request_calls_locator_once_and_publishes_ownership_snapshot()
    {
        var fixture = CreateFixture<ResourceRequest>();
        var resource = ResourceRef.Create(ResourceKind.Create("work-management.board"), Guid.NewGuid());
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        fixture.Locator
            .Setup(locator => locator.LocateAsync(resource, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceLocation(resource.Kind, resource.ResourceId, accountId, workspaceId));

        await fixture.Behavior.Handle(
            new ResourceRequest(resource),
            _ => Task.FromResult("ok"),
            CancellationToken.None);

        fixture.Locator.Verify(
            locator => locator.LocateAsync(resource, It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.ExecutionContext.Snapshot.Should().Match<ExecutionContextSnapshot>(snapshot =>
            snapshot.Resource == resource
            && snapshot.AccountId == accountId
            && snapshot.WorkspaceId == workspaceId);
    }

    [Fact]
    public async Task Missing_resource_preserves_not_found_contract()
    {
        var fixture = CreateFixture<ResourceRequest>();
        var resource = ResourceRef.Create(ResourceKind.Create("work-management.board"), Guid.NewGuid());
        fixture.Locator
            .Setup(locator => locator.LocateAsync(resource, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResourceLocation?)null);

        var act = () => fixture.Behavior.Handle(
            new ResourceRequest(resource),
            _ => Task.FromResult("ok"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        fixture.ExecutionContext.Snapshot.Should().BeNull();
    }

    /// <summary>
    /// M7 freeze characterization: a resource located in a DIFFERENT account
    /// than the selected request context is hidden, not denied — the behavior
    /// throws NotFound, never adopts the foreign tenant, and the rest of the
    /// pipeline never executes. Cross-tenant existence must not leak through
    /// a Forbidden-vs-NotFound distinction for every resource-scoped request.
    /// </summary>
    [Fact]
    public async Task Resource_request_located_in_another_account_is_hidden_and_never_adopts_foreign_tenant()
    {
        var fixture = CreateFixture<ResourceRequest>();
        var selectedAccountId = Guid.NewGuid();
        var foreignAccountId = Guid.NewGuid();
        var foreignWorkspaceId = Guid.NewGuid();

        fixture.Tenant.SetupGet(context => context.AccountId).Returns(selectedAccountId);
        fixture.Locator
            .Setup(locator => locator.LocateAsync(
                It.IsAny<ResourceRef>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceLocation(
                ResourceKind.Create("documents.page"), Guid.NewGuid(), foreignAccountId, foreignWorkspaceId));

        var act = () => fixture.Behavior.Handle(
            new ResourceRequest(ResourceRef.Create(ResourceKind.Create("documents.page"), Guid.NewGuid())),
            _ => Task.FromResult("ok"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();

        fixture.ExecutionContext.Snapshot.Should().BeNull(
            "the foreign resource must never produce an execution snapshot");
        fixture.Tenant.Verify(
            tenant => tenant.SetWorkspace(
                foreignAccountId, It.IsAny<Guid>(), It.IsAny<Guid?>()),
            Times.Never,
            "the foreign tenant must never be adopted");
    }

    private static Fixture<TRequest> CreateFixture<TRequest>() where TRequest : IRequest<string>
    {
        var userId = Guid.NewGuid();
        var executionContext = new Notrelix.Application.Common.Context.ExecutionContext();
        executionContext.SetUser(userId, "user@example.com", "User");
        var tenant = new Mock<ICurrentTenantContext>();
        tenant.SetupGet(context => context.UserId).Returns(userId);
        var credential = new Mock<ICurrentCredentialContext>();
        credential.SetupGet(context => context.Kind).Returns(CredentialKind.UserSession);
        var locator = new Mock<IResourceLocator>();
        var descriptor = RequestDescriptorValidator.Create(typeof(TRequest));
        var descriptors = new Mock<IRequestDescriptorRegistry>();
        descriptors.Setup(registry => registry.GetRequired(typeof(TRequest))).Returns(descriptor);

        var tenantBootstrap = new Mock<ITenantBootstrapStore>();
        tenantBootstrap
            .Setup(store => store.ResolveWorkspaceAccessAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceAccessSnapshot(
                Guid.NewGuid(), Guid.NewGuid(), userId, true, true));

        var behavior = new ExecutionContextBehavior<TRequest, string>(
            descriptors.Object,
            executionContext,
            tenant.Object,
            credential.Object,
            locator.Object,
            tenantBootstrap.Object);

        return new Fixture<TRequest>(behavior, executionContext, tenant, locator, tenantBootstrap);
    }

    private sealed record Fixture<TRequest>(
        ExecutionContextBehavior<TRequest, string> Behavior,
        Notrelix.Application.Common.Context.ExecutionContext ExecutionContext,
        Mock<ICurrentTenantContext> Tenant,
        Mock<IResourceLocator> Locator,
        Mock<ITenantBootstrapStore> TenantBootstrap)
        where TRequest : IRequest<string>;
}
