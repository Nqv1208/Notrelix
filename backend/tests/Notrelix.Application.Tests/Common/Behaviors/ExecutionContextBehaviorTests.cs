namespace Notrelix.Application.Tests.Common.Behaviors;

public sealed class ExecutionContextBehaviorTests
{
    private sealed record GlobalRequest : IRequest<string>, IAnonymousRequest, IGlobalRequest, INoDataRequest;

    private sealed record WorkspaceRequest(Guid WorkspaceId)
        : IRequest<string>, IAuthenticatedRequest, IWorkspaceRequest, IReadRequest;

    private sealed record PersonRequest : IRequest<string>, IAuthenticatedRequest, IGlobalRequest, IReadRequest;

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
        var nextCalled = false;

        fixture.Tenant.SetupGet(context => context.AccountId).Returns(selectedAccountId);
        fixture.Locator
            .Setup(locator => locator.LocateAsync(
                It.IsAny<ResourceRef>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceLocation(
                ResourceKind.Create("documents.page"), Guid.NewGuid(), foreignAccountId, foreignWorkspaceId));

        var act = () => fixture.Behavior.Handle(
            new ResourceRequest(ResourceRef.Create(ResourceKind.Create("documents.page"), Guid.NewGuid())),
            _ =>
            {
                nextCalled = true;
                return Task.FromResult("ok");
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        nextCalled.Should().BeFalse(
            "the cross-account hide must reject before the rest of the pipeline ever runs");

        fixture.ExecutionContext.Snapshot.Should().BeNull(
            "the foreign resource must never produce an execution snapshot");
        fixture.Tenant.Verify(
            tenant => tenant.SetWorkspace(
                foreignAccountId, It.IsAny<Guid>(), It.IsAny<Guid?>()),
            Times.Never,
            "the foreign tenant must never be adopted");
    }

    [Fact]
    public async Task Api_token_cannot_reach_a_resource_outside_its_bound_workspace()
    {
        var boundAccount = Guid.NewGuid();
        var fixture = CreateFixture<ResourceRequest>(CredentialKind.ApiToken, boundAccount, Guid.NewGuid());
        var resource = ResourceRef.Create(ResourceKind.Create("work-management.board"), Guid.NewGuid());
        // The issuing user also belongs to a second workspace of the same account: still not the token's.
        fixture.Locator
            .Setup(locator => locator.LocateAsync(resource, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceLocation(resource.Kind, resource.ResourceId, boundAccount, Guid.NewGuid()));
        var nextCalled = false;

        var act = () => fixture.Behavior.Handle(
            new ResourceRequest(resource), _ => { nextCalled = true; return Task.FromResult("ok"); }, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>("a resource of another workspace is as missing as a made-up id");
        nextCalled.Should().BeFalse();
        fixture.ExecutionContext.Snapshot.Should().BeNull();
    }

    [Fact]
    public async Task Api_token_cannot_reach_a_resource_of_another_account()
    {
        var fixture = CreateFixture<ResourceRequest>(CredentialKind.ApiToken, Guid.NewGuid(), Guid.NewGuid());
        var resource = ResourceRef.Create(ResourceKind.Create("work-management.board"), Guid.NewGuid());
        fixture.Locator
            .Setup(locator => locator.LocateAsync(resource, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceLocation(resource.Kind, resource.ResourceId, Guid.NewGuid(), Guid.NewGuid()));

        var act = () => fixture.Behavior.Handle(
            new ResourceRequest(resource), _ => Task.FromResult("ok"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Api_token_reaches_a_resource_of_its_bound_workspace()
    {
        var boundAccount = Guid.NewGuid();
        var boundWorkspace = Guid.NewGuid();
        var fixture = CreateFixture<ResourceRequest>(CredentialKind.ApiToken, boundAccount, boundWorkspace);
        var resource = ResourceRef.Create(ResourceKind.Create("work-management.board"), Guid.NewGuid());
        fixture.Locator
            .Setup(locator => locator.LocateAsync(resource, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceLocation(resource.Kind, resource.ResourceId, boundAccount, boundWorkspace));

        var result = await fixture.Behavior.Handle(
            new ResourceRequest(resource), _ => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
    }

    [Fact]
    public async Task Api_token_cannot_reach_a_person_level_request_or_another_workspace()
    {
        var boundAccount = Guid.NewGuid();
        var boundWorkspace = Guid.NewGuid();

        var person = CreateFixture<PersonRequest>(CredentialKind.ApiToken, boundAccount, boundWorkspace);
        var asPerson = () => person.Behavior.Handle(new PersonRequest(), _ => Task.FromResult("ok"), CancellationToken.None);
        await asPerson.Should().ThrowAsync<ForbiddenException>("an authenticated global request is a person, not a workspace");

        var elsewhere = CreateFixture<WorkspaceRequest>(CredentialKind.ApiToken, boundAccount, boundWorkspace);
        var otherWorkspace = () => elsewhere.Behavior.Handle(
            new WorkspaceRequest(Guid.NewGuid()), _ => Task.FromResult("ok"), CancellationToken.None);
        await otherWorkspace.Should().ThrowAsync<ForbiddenException>();
    }

    private static Fixture<TRequest> CreateFixture<TRequest>(
        CredentialKind kind = CredentialKind.UserSession,
        Guid? boundAccountId = null,
        Guid? boundWorkspaceId = null) where TRequest : IRequest<string>
    {
        var userId = Guid.NewGuid();
        var executionContext = new Notrelix.Application.Common.Context.ExecutionContext();
        executionContext.SetUser(userId, "user@example.com", "User");
        var tenant = new Mock<ICurrentTenantContext>();
        tenant.SetupGet(context => context.UserId).Returns(userId);
        var credential = new Mock<ICurrentCredentialContext>();
        credential.SetupGet(context => context.Kind).Returns(kind);
        credential.SetupGet(context => context.BoundAccountId).Returns(boundAccountId);
        credential.SetupGet(context => context.BoundWorkspaceId).Returns(boundWorkspaceId);
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
