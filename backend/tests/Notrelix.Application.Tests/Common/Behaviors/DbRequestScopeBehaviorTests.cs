using Notrelix.Application.Common.Requests.Scoping;

namespace Notrelix.Application.Tests.Common.Behaviors;

public class DbRequestScopeBehaviorTests
{
    public sealed record GlobalTransactionalRequest : IRequest<string>, IGlobalRequest, ITransactionalRequest;

    public sealed record WorkspaceTransactionalRequest : IRequest<string>, IWorkspaceRequest, ITransactionalRequest
    {
        public Guid WorkspaceId => Guid.NewGuid();
    }

    public sealed record RlsReadRequest : IRequest<string>, IRlsReadRequest, ITransactionalRequest;

    public sealed record GlobalPermissionRequest : IRequest<string>, IGlobalRequest, IRequirePermission
    {
        public PermissionAction Action => PermissionAction.ViewBoard;
        public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, Guid.NewGuid());
    }

    public sealed record NonTransactionalRequest : IRequest<string>;

    private static Mock<IRequestDataSession> CreateMockDataSession()
    {
        var session = new Mock<IRequestDataSession>();
        session
            .Setup(x => x.ExecuteAsync(
                It.IsAny<RequestDataSessionOptions>(),
                It.IsAny<Func<CancellationToken, Task<string>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<RequestDataSessionOptions, Func<CancellationToken, Task<string>>, CancellationToken>(
                (_, action, ct) => action(ct));
        return session;
    }

    private static DbRequestScopeBehavior<T, string> CreateBehavior<T>(
        Mock<IRequestDataSession>? dataSession = null)
        where T : IRequest<string>
    {
        return new DbRequestScopeBehavior<T, string>(
            (dataSession ?? CreateMockDataSession()).Object,
            Mock.Of<ILogger<DbRequestScopeBehavior<T, string>>>());
    }

    [Fact]
    public async Task GlobalTransactionalRequest_PassesTransactionalOptions()
    {
        var dataSession = CreateMockDataSession();
        var behavior = CreateBehavior<GlobalTransactionalRequest>(dataSession);
        RequestDataSessionOptions? capturedOptions = null;

        dataSession
            .Setup(x => x.ExecuteAsync(
                It.IsAny<RequestDataSessionOptions>(),
                It.IsAny<Func<CancellationToken, Task<string>>>(),
                It.IsAny<CancellationToken>()))
            .Callback<RequestDataSessionOptions, Func<CancellationToken, Task<string>>, CancellationToken>(
                (opts, _, _) => capturedOptions = opts)
            .Returns<RequestDataSessionOptions, Func<CancellationToken, Task<string>>, CancellationToken>(
                (_, action, ct) => action(ct));

        await behavior.Handle(new GlobalTransactionalRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

        capturedOptions.Should().NotBeNull();
        capturedOptions!.Access.Should().Be(RequestDataAccess.Transactional);
        capturedOptions.ApplyTenantScope.Should().BeFalse();
    }

    [Fact]
    public async Task WorkspaceTransactionalRequest_PassesTenantScope()
    {
        var dataSession = CreateMockDataSession();
        var behavior = CreateBehavior<WorkspaceTransactionalRequest>(dataSession);
        RequestDataSessionOptions? capturedOptions = null;

        dataSession
            .Setup(x => x.ExecuteAsync(
                It.IsAny<RequestDataSessionOptions>(),
                It.IsAny<Func<CancellationToken, Task<string>>>(),
                It.IsAny<CancellationToken>()))
            .Callback<RequestDataSessionOptions, Func<CancellationToken, Task<string>>, CancellationToken>(
                (opts, _, _) => capturedOptions = opts)
            .Returns<RequestDataSessionOptions, Func<CancellationToken, Task<string>>, CancellationToken>(
                (_, action, ct) => action(ct));

        await behavior.Handle(new WorkspaceTransactionalRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

        capturedOptions.Should().NotBeNull();
        capturedOptions!.Access.Should().Be(RequestDataAccess.Transactional);
        capturedOptions.ApplyTenantScope.Should().BeTrue();
    }

    [Fact]
    public async Task GlobalPermissionRequest_Throws_SecurityMisconfiguration()
    {
        var behavior = CreateBehavior<GlobalPermissionRequest>();

        Func<Task> act = () => behavior.Handle(
            new GlobalPermissionRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

        await act.Should().ThrowAsync<SecurityMisconfigurationException>()
            .WithMessage("*is global but requires tenant RLS.*");
    }

    [Fact]
    public async Task NonTransactionalRequest_SkipsDataSession()
    {
        var dataSession = CreateMockDataSession();
        var behavior = CreateBehavior<NonTransactionalRequest>(dataSession);

        var result = await behavior.Handle(
            new NonTransactionalRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
        dataSession.Verify(
            x => x.ExecuteAsync(
                It.IsAny<RequestDataSessionOptions>(),
                It.IsAny<Func<CancellationToken, Task<string>>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
