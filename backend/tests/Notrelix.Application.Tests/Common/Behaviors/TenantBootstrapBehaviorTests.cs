namespace Notrelix.Application.Tests.Common.Behaviors;

/// <summary>
/// IA-ACC-006: tenant spoofing protection at the Application pipeline seam.
/// A client-controlled WorkspaceId/AccountId must never bypass the actor's
/// real tenant scope — the tenant context (session) is the authority and the
/// bootstrap store is consulted before the handler executes.
/// </summary>
public class TenantBootstrapBehaviorTests
{
    private static readonly Guid TestAccountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TestOtherAccountId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TestWorkspaceId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TestUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public sealed record TestWorkspaceCommand(Guid WorkspaceId) : IRequest<string>, IWorkspaceRequest;

    public sealed record TestAccountCommand : IRequest<string>, IAccountRequest;

    [Fact]
    public async Task WorkspaceRequest_RequestedWorkspaceOfOtherTenant_ThrowsForbidden_HandlerNotCalled()
    {
        var tenantMock = CreateTenantMock(accountId: TestAccountId);
        var storeMock = new Mock<ITenantBootstrapStore>();
        storeMock
            .Setup(x => x.ResolveWorkspaceAccessAsync(TestWorkspaceId, TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceAccessSnapshot(TestOtherAccountId, TestWorkspaceId, TestUserId, CanAccess: false, IsWorkspaceActive: true));

        var behavior = CreateBehavior<TestWorkspaceCommand>(tenantMock.Object, storeMock.Object);
        var handlerCalled = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            handlerCalled = true;
            return Task.FromResult("ok");
        };

        Func<Task> act = () => behavior.Handle(new TestWorkspaceCommand(TestWorkspaceId), next, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        handlerCalled.Should().BeFalse("handler must not execute when the requested workspace is outside the actor's tenant");
        tenantMock.Verify(x => x.SetWorkspace(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task WorkspaceRequest_AuthorizedActor_ResolvesScopeAndSetsTenantContext()
    {
        var tenantMock = CreateTenantMock(accountId: TestAccountId);
        var storeMock = new Mock<ITenantBootstrapStore>();
        storeMock
            .Setup(x => x.ResolveWorkspaceAccessAsync(TestWorkspaceId, TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceAccessSnapshot(TestAccountId, TestWorkspaceId, TestUserId, CanAccess: true, IsWorkspaceActive: true));

        var behavior = CreateBehavior<TestWorkspaceCommand>(tenantMock.Object, storeMock.Object);
        var handlerCalled = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            handlerCalled = true;
            return Task.FromResult("ok");
        };

        var result = await behavior.Handle(new TestWorkspaceCommand(TestWorkspaceId), next, CancellationToken.None);

        result.Should().Be("ok");
        handlerCalled.Should().BeTrue();
        tenantMock.Verify(x => x.SetWorkspace(TestAccountId, TestWorkspaceId, TestUserId), Times.Once);
    }

    [Fact]
    public async Task AccountRequest_AccountIdComesFromTenantContext_NotFromRequestPayload()
    {
        var tenantMock = CreateTenantMock(accountId: TestAccountId);
        var storeMock = new Mock<ITenantBootstrapStore>();
        storeMock
            .Setup(x => x.VerifyAccountAccessAsync(TestAccountId, TestUserId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var behavior = CreateBehavior<TestAccountCommand>(tenantMock.Object, storeMock.Object);
        var handlerCalled = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            handlerCalled = true;
            return Task.FromResult("ok");
        };

        var result = await behavior.Handle(new TestAccountCommand(), next, CancellationToken.None);

        result.Should().Be("ok");
        handlerCalled.Should().BeTrue();
        storeMock.Verify(x => x.VerifyAccountAccessAsync(TestAccountId, TestUserId, It.IsAny<CancellationToken>()), Times.Once);
        tenantMock.Verify(x => x.SetAccount(TestAccountId, TestUserId), Times.Once);
    }

    [Fact]
    public async Task AccountRequest_NoAccountSelected_ThrowsAccountSelectionRequired_HandlerNotCalled()
    {
        var tenantMock = CreateTenantMock(accountId: null);
        var behavior = CreateBehavior<TestAccountCommand>(tenantMock.Object, Mock.Of<ITenantBootstrapStore>());
        var handlerCalled = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            handlerCalled = true;
            return Task.FromResult("ok");
        };

        Func<Task> act = () => behavior.Handle(new TestAccountCommand(), next, CancellationToken.None);

        await act.Should().ThrowAsync<AccountSelectionRequiredException>();
        handlerCalled.Should().BeFalse("handler must not execute when no account is selected in the tenant context");
    }

    [Fact]
    public async Task AccountRequest_ActorWithoutActiveMembership_ThrowsForbidden_HandlerNotCalled()
    {
        var tenantMock = CreateTenantMock(accountId: TestAccountId);
        var storeMock = new Mock<ITenantBootstrapStore>();
        storeMock
            .Setup(x => x.VerifyAccountAccessAsync(TestAccountId, TestUserId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException($"User {TestUserId} does not have active access to account {TestAccountId}."));

        var behavior = CreateBehavior<TestAccountCommand>(tenantMock.Object, storeMock.Object);
        var handlerCalled = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            handlerCalled = true;
            return Task.FromResult("ok");
        };

        Func<Task> act = () => behavior.Handle(new TestAccountCommand(), next, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        handlerCalled.Should().BeFalse("handler must not execute when the actor has no active membership in the selected account");
    }

    private static Mock<ICurrentTenantContext> CreateTenantMock(Guid? accountId)
    {
        var tenantMock = new Mock<ICurrentTenantContext>();
        tenantMock.Setup(x => x.UserId).Returns(TestUserId);
        tenantMock.Setup(x => x.AccountId).Returns(accountId);
        return tenantMock;
    }

    private static TenantBootstrapBehavior<TRequest, string> CreateBehavior<TRequest>(
        ICurrentTenantContext tenant,
        ITenantBootstrapStore store)
        where TRequest : notnull
        => new(tenant, store, Mock.Of<ILogger<TenantBootstrapBehavior<TRequest, string>>>());
}
