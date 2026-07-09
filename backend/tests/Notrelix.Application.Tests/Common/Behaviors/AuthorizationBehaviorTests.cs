namespace Notrelix.Application.Tests.Common.Behaviors;

public class AuthorizationBehaviorTests
{
    // --- Test request types ---

    public sealed record AnonymousRequest : IRequest<string>, IAnonymousRequest;

    public sealed record AuthenticatedRequest : IRequest<string>, IAuthenticatedRequest;

    public sealed record WorkspaceNoPermissionRequest : IRequest<string>, IWorkspaceRequest
    {
        public Guid WorkspaceId => Guid.NewGuid();
    }

    public sealed record WorkspaceWithPermissionRequest : IRequest<string>, IWorkspaceRequest, IRequirePermission
    {
        public Guid WorkspaceId => Guid.NewGuid();
        public PermissionAction Action => PermissionAction.ViewBoard;
        public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, Guid.NewGuid(), Guid.NewGuid());
    }

    public sealed record WorkspaceSystemInternalRequest : IRequest<string>, IWorkspaceRequest, ISystemInternalRequest
    {
        public Guid WorkspaceId => Guid.NewGuid();
    }

    public sealed record AccountNoPermissionRequest : IRequest<string>, IAccountRequest;

    public sealed record AccountSystemInternalRequest : IRequest<string>, IAccountRequest, ISystemInternalRequest;

    public sealed record AccountWithPermissionRequest : IRequest<string>, IAccountRequest, IRequirePermission
    {
        public PermissionAction Action => PermissionAction.ViewBoard;
        public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, Guid.NewGuid());
    }

    public sealed record AccountNullResourceRequest : IRequest<string>, IAccountRequest, IRequirePermission
    {
        public PermissionAction Action => PermissionAction.ViewWorkspace;
        public ResourceRef? Resource => null;
    }

    public sealed record WorkspaceNullResourceRequest : IRequest<string>, IWorkspaceRequest, IRequirePermission
    {
        public Guid WorkspaceId => Guid.NewGuid();
        public PermissionAction Action => PermissionAction.ViewBoard;
        public ResourceRef? Resource => null;
    }

    public sealed record UnclassifiedRequest : IRequest<string>;

    public sealed record SystemInternalPlainRequest : IRequest<string>, ISystemInternalRequest
    {
        public UseCaseSecurityKind SecurityKind => UseCaseSecurityKind.SystemInternal;
    }

    // --- Helpers ---

    private static Mock<ICurrentUser> CreateAuthenticatedUser()
    {
        var mock = new Mock<ICurrentUser>();
        mock.Setup(x => x.UserId).Returns(Guid.NewGuid());
        mock.Setup(x => x.IsAuthenticated).Returns(true);
        return mock;
    }

    private static Mock<ICurrentUser> CreateUnauthenticatedUser()
    {
        var mock = new Mock<ICurrentUser>();
        mock.Setup(x => x.UserId).Returns(Guid.Empty);
        mock.Setup(x => x.IsAuthenticated).Returns(false);
        return mock;
    }

    private static Mock<ICurrentTenantContext> CreateTenantContext(Guid? accountId = null, Guid? workspaceId = null)
    {
        var mock = new Mock<ICurrentTenantContext>();
        mock.Setup(x => x.AccountId).Returns(accountId);
        mock.Setup(x => x.WorkspaceId).Returns(workspaceId);
        mock.Setup(x => x.RequireAccountId()).Returns(accountId ?? Guid.Empty);
        return mock;
    }

    private static AuthorizationBehavior<T, string> CreateBehavior<T>(
        Mock<ICurrentUser>? user = null,
        Mock<ICurrentTenantContext>? tenant = null,
        Mock<IAuthorizationDecisionStore>? permission = null)
        where T : IRequest<string>
    {
        return new AuthorizationBehavior<T, string>(
            user?.Object ?? CreateAuthenticatedUser().Object,
            tenant?.Object ?? CreateTenantContext(Guid.NewGuid(), Guid.NewGuid()).Object,
            permission?.Object ?? Mock.Of<IAuthorizationDecisionStore>(),
            Mock.Of<ILogger<AuthorizationBehavior<T, string>>>());
    }

    // --- Tests ---

    [Fact]
    public async Task AnonymousRequest_SkipsAuth_CallsHandler()
    {
        var handlerCalled = false;
        var behavior = CreateBehavior<AnonymousRequest>(user: CreateUnauthenticatedUser());

        RequestHandlerDelegate<string> next = _ =>
        {
            handlerCalled = true;
            return Task.FromResult("ok");
        };

        var result = await behavior.Handle(new AnonymousRequest(), next, CancellationToken.None);

        result.Should().Be("ok");
        handlerCalled.Should().BeTrue("anonymous request should call handler");
    }

    [Fact]
    public async Task SystemInternalRequest_WithoutUser_BypassesAuth_CallsHandler()
    {
        var handlerCalled = false;
        var behavior = CreateBehavior<SystemInternalPlainRequest>(user: CreateUnauthenticatedUser());

        RequestHandlerDelegate<string> next = _ =>
        {
            handlerCalled = true;
            return Task.FromResult("ok");
        };

        var result = await behavior.Handle(new SystemInternalPlainRequest(), next, CancellationToken.None);

        result.Should().Be("ok");
        handlerCalled.Should().BeTrue("system-internal request should bypass auth and call handler even without user context");
    }

    [Fact]
    public async Task SystemInternalRequest_WithUser_BypassesAuth_CallsHandler()
    {
        var handlerCalled = false;
        var behavior = CreateBehavior<SystemInternalPlainRequest>(user: CreateAuthenticatedUser());

        RequestHandlerDelegate<string> next = _ =>
        {
            handlerCalled = true;
            return Task.FromResult("ok");
        };

        var result = await behavior.Handle(new SystemInternalPlainRequest(), next, CancellationToken.None);

        result.Should().Be("ok");
        handlerCalled.Should().BeTrue("system-internal request should call handler even with authenticated user");
    }

    [Fact]
    public async Task AuthenticatedRequest_WithUser_CallsHandler()
    {
        var handlerCalled = false;
        var behavior = CreateBehavior<AuthenticatedRequest>();

        RequestHandlerDelegate<string> next = _ =>
        {
            handlerCalled = true;
            return Task.FromResult("ok");
        };

        var result = await behavior.Handle(new AuthenticatedRequest(), next, CancellationToken.None);

        result.Should().Be("ok");
        handlerCalled.Should().BeTrue("authenticated request with user should call handler");
    }

    [Fact]
    public async Task NonAnonymousRequest_WithoutUser_ThrowsUnauthorized()
    {
        var handlerCalled = false;
        var behavior = CreateBehavior<AuthenticatedRequest>(user: CreateUnauthenticatedUser());

        RequestHandlerDelegate<string> next = _ =>
        {
            handlerCalled = true;
            return Task.FromResult("ok");
        };

        Func<Task> act = () => behavior.Handle(new AuthenticatedRequest(), next, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
        handlerCalled.Should().BeFalse("handler should not execute when auth fails");
    }

    [Fact]
    public async Task WorkspaceRequest_WithoutPermissionMarker_ThrowsSecurityMisconfiguration()
    {
        var handlerCalled = false;
        var behavior = CreateBehavior<WorkspaceNoPermissionRequest>();

        RequestHandlerDelegate<string> next = _ =>
        {
            handlerCalled = true;
            return Task.FromResult("ok");
        };

        Func<Task> act = () => behavior.Handle(new WorkspaceNoPermissionRequest(), next, CancellationToken.None);

        await act.Should().ThrowAsync<SecurityMisconfigurationException>();
        handlerCalled.Should().BeFalse("handler should not execute on security misconfiguration");
    }

    [Fact]
    public async Task WorkspaceRequest_WithoutTenantContext_ThrowsSecurityMisconfiguration()
    {
        var handlerCalled = false;
        var tenant = CreateTenantContext(accountId: null, workspaceId: null);
        var behavior = CreateBehavior<WorkspaceWithPermissionRequest>(tenant: tenant);

        RequestHandlerDelegate<string> next = _ =>
        {
            handlerCalled = true;
            return Task.FromResult("ok");
        };

        Func<Task> act = () => behavior.Handle(new WorkspaceWithPermissionRequest(), next, CancellationToken.None);

        await act.Should().ThrowAsync<SecurityMisconfigurationException>();
        handlerCalled.Should().BeFalse("handler should not execute when tenant context is missing");
    }

    [Fact]
    public async Task WorkspaceRequest_WithPermissionMarker_CallsPermissionService()
    {
        var handlerCalled = false;
        var permissionService = new Mock<IAuthorizationDecisionStore>();
        permissionService.Setup(x => x.EvaluateAsync(It.IsAny<PermissionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PermissionDecision(true, null));

        var behavior = CreateBehavior<WorkspaceWithPermissionRequest>(permission: permissionService);

        RequestHandlerDelegate<string> next = _ =>
        {
            handlerCalled = true;
            return Task.FromResult("ok");
        };

        await behavior.Handle(new WorkspaceWithPermissionRequest(), next, CancellationToken.None);

        handlerCalled.Should().BeTrue("handler should execute when permission is granted");
        permissionService.Verify(
            x => x.EvaluateAsync(It.IsAny<PermissionContext>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PermissionDenied_ThrowsForbidden_DoesNotCallHandler()
    {
        var handlerCalled = false;
        var permissionService = new Mock<IAuthorizationDecisionStore>();
        permissionService.Setup(x => x.EvaluateAsync(It.IsAny<PermissionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PermissionDecision(false, "missing_permission"));

        var behavior = CreateBehavior<WorkspaceWithPermissionRequest>(permission: permissionService);

        RequestHandlerDelegate<string> next = _ =>
        {
            handlerCalled = true;
            return Task.FromResult("ok");
        };

        Func<Task> act = () => behavior.Handle(new WorkspaceWithPermissionRequest(), next, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        handlerCalled.Should().BeFalse("handler should not execute when permission denied");
    }

    [Fact]
    public async Task WorkspaceSystemInternalRequest_DoesNotRequirePermissionMarker()
    {
        var handlerCalled = false;
        var behavior = CreateBehavior<WorkspaceSystemInternalRequest>();

        RequestHandlerDelegate<string> next = _ =>
        {
            handlerCalled = true;
            return Task.FromResult("ok");
        };

        var result = await behavior.Handle(new WorkspaceSystemInternalRequest(), next, CancellationToken.None);

        result.Should().Be("ok");
        handlerCalled.Should().BeTrue("system internal workspace request should execute without permission marker");
    }

    [Fact]
    public async Task AccountRequest_WithoutPermissionMarker_ThrowsSecurityMisconfiguration()
    {
        var handlerCalled = false;
        var tenant = CreateTenantContext(accountId: Guid.NewGuid());
        var behavior = CreateBehavior<AccountNoPermissionRequest>(tenant: tenant);

        RequestHandlerDelegate<string> next = _ =>
        {
            handlerCalled = true;
            return Task.FromResult("ok");
        };

        Func<Task> act = () => behavior.Handle(new AccountNoPermissionRequest(), next, CancellationToken.None);

        await act.Should().ThrowAsync<SecurityMisconfigurationException>();
        handlerCalled.Should().BeFalse("handler should not execute on security misconfiguration");
    }

    [Fact]
    public async Task AccountSystemInternalRequest_DoesNotRequirePermissionMarker()
    {
        var handlerCalled = false;
        var tenant = CreateTenantContext(accountId: Guid.NewGuid());
        var behavior = CreateBehavior<AccountSystemInternalRequest>(tenant: tenant);

        RequestHandlerDelegate<string> next = _ =>
        {
            handlerCalled = true;
            return Task.FromResult("ok");
        };

        var result = await behavior.Handle(new AccountSystemInternalRequest(), next, CancellationToken.None);

        result.Should().Be("ok");
        handlerCalled.Should().BeTrue("system internal account request should execute without permission marker");
    }

    [Fact]
    public async Task AccountRequest_WithPermissionMarker_CallsPermissionService()
    {
        var handlerCalled = false;
        var tenant = CreateTenantContext(accountId: Guid.NewGuid());
        var permissionService = new Mock<IAuthorizationDecisionStore>();
        permissionService.Setup(x => x.EvaluateAsync(It.IsAny<PermissionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PermissionDecision(true, null));

        var behavior = CreateBehavior<AccountWithPermissionRequest>(tenant: tenant, permission: permissionService);

        RequestHandlerDelegate<string> next = _ =>
        {
            handlerCalled = true;
            return Task.FromResult("ok");
        };

        await behavior.Handle(new AccountWithPermissionRequest(), next, CancellationToken.None);

        handlerCalled.Should().BeTrue("handler should execute when permission is granted");
    }

    [Fact]
    public async Task AccountScopedNullResource_UsesTenantAccountId()
    {
        var handlerCalled = false;
        var accountId = Guid.NewGuid();
        var tenant = CreateTenantContext(accountId: accountId);
        var permissionService = new Mock<IAuthorizationDecisionStore>();
        permissionService.Setup(x => x.EvaluateAsync(It.IsAny<PermissionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PermissionDecision(true, null));

        var behavior = CreateBehavior<AccountNullResourceRequest>(tenant: tenant, permission: permissionService);

        RequestHandlerDelegate<string> next = _ =>
        {
            handlerCalled = true;
            return Task.FromResult("ok");
        };

        await behavior.Handle(new AccountNullResourceRequest(), next, CancellationToken.None);

        handlerCalled.Should().BeTrue("handler should execute when permission is granted");

        // Verify permission was evaluated with account resource resolved from tenant context
        permissionService.Verify(
            x => x.EvaluateAsync(
                It.Is<PermissionContext>(ctx =>
                    ctx.ResourceType == ResourceType.Account &&
                    ctx.ResourceId == accountId &&
                    ctx.Scope == Notrelix.Application.Common.Security.PermissionScope.Account),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AccountScopedNullResource_WithoutTenantAccount_ThrowsSecurityMisconfiguration()
    {
        var handlerCalled = false;
        var tenant = CreateTenantContext(accountId: null);
        var behavior = CreateBehavior<AccountNullResourceRequest>(tenant: tenant);

        RequestHandlerDelegate<string> next = _ =>
        {
            handlerCalled = true;
            return Task.FromResult("ok");
        };

        Func<Task> act = () => behavior.Handle(new AccountNullResourceRequest(), next, CancellationToken.None);

        await act.Should().ThrowAsync<SecurityMisconfigurationException>();
        handlerCalled.Should().BeFalse("handler should not execute when tenant account is missing");
    }

    [Fact]
    public async Task WorkspaceScopedNullResource_ThrowsSecurityMisconfiguration()
    {
        var handlerCalled = false;
        var behavior = CreateBehavior<WorkspaceNullResourceRequest>();

        RequestHandlerDelegate<string> next = _ =>
        {
            handlerCalled = true;
            return Task.FromResult("ok");
        };

        Func<Task> act = () => behavior.Handle(new WorkspaceNullResourceRequest(), next, CancellationToken.None);

        await act.Should().ThrowAsync<SecurityMisconfigurationException>();
        handlerCalled.Should().BeFalse("handler should not execute");
    }

    [Fact]
    public async Task UnclassifiedRequest_DoesNotThrow_SinceNotAnnotated()
    {
        // Unclassified requests without any security interface pass through.
        // This is expected to be caught by architecture tests, not by runtime behavior.
        var handlerCalled = false;
        var behavior = CreateBehavior<UnclassifiedRequest>();

        RequestHandlerDelegate<string> next = _ =>
        {
            handlerCalled = true;
            return Task.FromResult("ok");
        };

        var result = await behavior.Handle(new UnclassifiedRequest(), next, CancellationToken.None);

        result.Should().Be("ok");
        handlerCalled.Should().BeTrue("unclassified request should pass through (no security marker)");
    }
}
