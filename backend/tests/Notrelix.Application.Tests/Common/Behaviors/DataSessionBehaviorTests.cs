namespace Notrelix.Application.Tests.Common.Behaviors;

public sealed class DataSessionBehaviorTests
{
    private sealed record NoDataRequest : IRequest<string>, IAnonymousRequest, IGlobalRequest, INoDataRequest;
    private sealed record ReadRequest(Guid WorkspaceId)
        : IRequest<string>, IAuthenticatedRequest, IWorkspaceRequest, IReadRequest;
    private sealed record WriteRequest(Guid WorkspaceId)
        : IRequest<string>, IAuthenticatedRequest, IWorkspaceRequest, IWriteRequest;
    private sealed record SecurityReadRequest(Guid WorkspaceId)
        : IRequest<string>, IAuthenticatedRequest, IWorkspaceRequest, INoDataRequest, IRequireVerifiedEmail;

    [Fact]
    public async Task NoData_without_access_facts_opens_no_session()
    {
        var fixture = CreateFixture<NoDataRequest>(ApplicationScopeKind.Global);

        await fixture.Behavior.Handle(new NoDataRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

        fixture.Session.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(typeof(ReadRequest), RequestDataAccess.ReadOnly)]
    [InlineData(typeof(WriteRequest), RequestDataAccess.Transactional)]
    [InlineData(typeof(SecurityReadRequest), RequestDataAccess.ReadOnly)]
    public async Task Descriptor_derives_the_expected_session(Type requestType, RequestDataAccess expectedAccess)
    {
        var method = typeof(DataSessionBehaviorTests)
            .GetMethod(nameof(VerifySession), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(requestType);

        await (Task)method.Invoke(null, [expectedAccess])!;
    }

    private static async Task VerifySession<TRequest>(RequestDataAccess expectedAccess)
        where TRequest : IRequest<string>
    {
        var fixture = CreateFixture<TRequest>(ApplicationScopeKind.Workspace);
        var request = (TRequest)Activator.CreateInstance(typeof(TRequest), Guid.NewGuid())!;

        await fixture.Behavior.Handle(request, _ => Task.FromResult("ok"), CancellationToken.None);

        fixture.Session.Verify(session => session.ExecuteAsync(
            It.Is<RequestDataSessionOptions>(options =>
                options.Access == expectedAccess && options.ApplyTenantScope),
            It.IsAny<Func<CancellationToken, Task<string>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Fixture<TRequest> CreateFixture<TRequest>(ApplicationScopeKind scope)
        where TRequest : IRequest<string>
    {
        var descriptor = RequestDescriptorValidator.Create(typeof(TRequest));
        var descriptors = new Mock<IRequestDescriptorRegistry>();
        descriptors.Setup(registry => registry.GetRequired(typeof(TRequest))).Returns(descriptor);
        var executionContext = new Mock<IExecutionContextReader>();
        executionContext.SetupGet(context => context.Snapshot).Returns(new ExecutionContextSnapshot(
            Guid.NewGuid(), Guid.NewGuid(), scope == ApplicationScopeKind.Workspace ? Guid.NewGuid() : null,
            null, descriptor.Principal, scope, Guid.NewGuid().ToString("D")));
        var session = new Mock<IRequestDataSession>();
        session.Setup(candidate => candidate.ExecuteAsync(
                It.IsAny<RequestDataSessionOptions>(),
                It.IsAny<Func<CancellationToken, Task<string>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<RequestDataSessionOptions, Func<CancellationToken, Task<string>>, CancellationToken>(
                (_, action, cancellationToken) => action(cancellationToken));

        return new Fixture<TRequest>(
            new DataSessionBehavior<TRequest, string>(descriptors.Object, executionContext.Object, session.Object),
            session);
    }

    private sealed record Fixture<TRequest>(
        DataSessionBehavior<TRequest, string> Behavior,
        Mock<IRequestDataSession> Session)
        where TRequest : IRequest<string>;
}
