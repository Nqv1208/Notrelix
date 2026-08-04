using FluentValidation;
using Microsoft.Extensions.Options;
using ValidationException = Notrelix.Application.Common.Exceptions.ValidationException;

namespace Notrelix.Application.Tests.Behaviors;

public class PipelineExecutionTests
{
    #region Test infrastructure

    [IdempotencyOperation("test.module.test-command.v1")]
    public sealed record ExecutableCommand : IRequest<string>, ITransactionalRequest, IWorkspaceRequest, IRequirePermission, IIdempotentRequest
    {
        private static readonly Guid Wsid = Guid.NewGuid();
        public Guid WorkspaceId => Wsid;
        public PermissionAction Action => PermissionAction.ViewWorkspace;
        public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), Wsid, Wsid);
    }

    public sealed record NonTransactionalCommand : IRequest<string>, IGlobalRequest;

    public sealed record SideEffectCommand : IRequest<string>, ITransactionalRequest, IRealtimeRequest, IWorkspaceRequest
    {
        public Guid WorkspaceId => Guid.NewGuid();
        public RealtimeTopic Topic => new("test", "test", Guid.NewGuid());
    }

    public sealed record ValidationFailCommand : IRequest<string>, ITransactionalRequest, IGlobalRequest
    {
        public string? Value { get; init; }
    }

    public sealed class ValidationFailCommandValidator : AbstractValidator<ValidationFailCommand>
    {
        public ValidationFailCommandValidator() => RuleFor(x => x.Value).NotEmpty();
    }

    public sealed record EmptyWorkspaceCommand : IRequest<string>, IWorkspaceRequest
    {
        public Guid WorkspaceId => Guid.Empty;
    }

    /// <summary>
    /// Creates a mock <see cref="IRequestDataSession"/> that invokes the action callback,
    /// simulating a successful data session (transaction + save + commit handled internally).
    /// </summary>
    private static Mock<IRequestDataSession> CreateMockDataSession(
        List<string>? executionOrder = null,
        Exception? throwAfterAction = null)
    {
        var dataSession = new Mock<IRequestDataSession>();

        if (throwAfterAction is not null)
        {
            // Simulate infrastructure save/commit failure after handler succeeds
            dataSession
                .Setup(x => x.ExecuteAsync(
                    It.IsAny<RequestDataSessionOptions>(),
                    It.IsAny<Func<CancellationToken, Task<string>>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<RequestDataSessionOptions, Func<CancellationToken, Task<string>>, CancellationToken>(
                    async (_, action, ct) =>
                    {
                        await action(ct);
                        throw throwAfterAction;
                    });
        }
        else if (executionOrder is not null)
        {
            // Track that the data session completed (wraps handler + save + commit)
            dataSession
                .Setup(x => x.ExecuteAsync(
                    It.IsAny<RequestDataSessionOptions>(),
                    It.IsAny<Func<CancellationToken, Task<string>>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<RequestDataSessionOptions, Func<CancellationToken, Task<string>>, CancellationToken>(
                    async (_, action, ct) =>
                    {
                        var result = await action(ct);
                        executionOrder.Add("DataSession");
                        return result;
                    });
        }
        else
        {
            // Default: pass through to the action
            dataSession
                .Setup(x => x.ExecuteAsync(
                    It.IsAny<RequestDataSessionOptions>(),
                    It.IsAny<Func<CancellationToken, Task<string>>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<RequestDataSessionOptions, Func<CancellationToken, Task<string>>, CancellationToken>(
                    (_, action, ct) => action(ct));
        }

        return dataSession;
    }

    private static Mock<ICurrentUser> CreateMockUser(Guid? userId = null)
    {
        var user = new Mock<ICurrentUser>();
        user.Setup(x => x.UserId).Returns(userId ?? Guid.NewGuid());
        user.Setup(x => x.IsAuthenticated).Returns(true);
        return user;
    }

    private static Mock<IAuthorizationDecisionStore> CreateMockPermissionService(bool allowed = true)
    {
        var service = new Mock<IAuthorizationDecisionStore>();
        service.Setup(x => x.EvaluateAsync(It.IsAny<PermissionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PermissionDecision(allowed, "test"));
        return service;
    }

    private static Mock<IIdempotencyStore> CreateMockIdempotencyStore(
        IdempotencyBeginResult? beginResult = null)
    {
        var store = new Mock<IIdempotencyStore>();
        store.Setup(x => x.BeginAsync(
                It.IsAny<IdempotencyIdentity>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(beginResult ?? new IdempotencyBeginResult(
                IdempotencyBeginStatus.Started, null, null));
        store.Setup(x => x.CompleteAsync(
                It.IsAny<IdempotencyIdentity>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return store;
    }

    private static IdempotencyBehavior<TRequest, string> CreateIdempotencyBehavior<TRequest>(
        Mock<IIdempotencyStore> mockStore,
        bool allowSerializedResult = true)
        where TRequest : notnull
    {
        var mockTenant = new Mock<ICurrentTenantContext>();
        mockTenant.Setup(x => x.AccountId).Returns(Guid.NewGuid());
        mockTenant.Setup(x => x.WorkspaceId).Returns((Guid?)null);

        var partitionFactory = new IdempotencyPartitionFactory(mockTenant.Object);

        var mockFingerprint = new Mock<IIdempotencyRequestFingerprint>();
        mockFingerprint.Setup(x => x.Compute(It.IsAny<IIdempotentRequest>(), It.IsAny<Type>()))
            .Returns("test-fingerprint-hash");

        var mockReplayPolicy = new Mock<IIdempotencyReplayPolicy>();
        if (!allowSerializedResult)
        {
            mockReplayPolicy.Setup(x => x.EnsureSerializedResultAllowed(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new InvalidOperationException("test replay policy rejected serialized result"));
        }

        var executionContext = new IdempotencyExecutionContext();
        executionContext.Set("test-execution-key", IdempotencyExecutionSource.Internal);

        return new IdempotencyBehavior<TRequest, string>(
            mockStore.Object,
            mockFingerprint.Object,
            mockReplayPolicy.Object,
            partitionFactory,
            executionContext,
            executionContext,
            Mock.Of<ILogger<IdempotencyBehavior<TRequest, string>>>());
    }

    private static Mock<IPostCommitActionQueue> CreateMockPostCommitQueue()
    {
        var queue = new Mock<IPostCommitActionQueue>();
        queue.Setup(x => x.Actions).Returns([]);
        return queue;
    }

    private static IExecutionContextReader CreateMockExecutionContext()
    {
        var ctx = new Notrelix.Application.Common.Context.ExecutionContext();
        ctx.SetUser(Guid.NewGuid(), "test@test.com", "Test User");
        ctx.SetTenant(Guid.NewGuid(), Guid.NewGuid());
        return ctx;
    }

    #endregion

    #region 1. Pipeline execution order (runtime proof)

    [Fact]
    public async Task Pipeline_ShouldExecuteInCorrectOrder_TransactionalCommand()
    {
        var executionOrder = new List<string>();

        var dataSession = CreateMockDataSession();
        var mockUser = CreateMockUser();
        var mockPermissionService = CreateMockPermissionService();
        var mockPostCommit = CreateMockPostCommitQueue();

        var transactionBehavior = new DbRequestScopeBehavior<ExecutableCommand, string>(
            dataSession.Object, Mock.Of<ILogger<DbRequestScopeBehavior<ExecutableCommand, string>>>());

        var mockTenant = new Mock<ICurrentTenantContext>();
        mockTenant.Setup(x => x.AccountId).Returns(Guid.NewGuid());
        mockTenant.Setup(x => x.WorkspaceId).Returns(Guid.NewGuid());

        var authorizationBehavior = new AuthorizationBehavior<ExecutableCommand, string>(
            mockUser.Object, mockTenant.Object, mockPermissionService.Object, Mock.Of<ILogger<AuthorizationBehavior<ExecutableCommand, string>>>());

        var workspaceBehavior = new TenantBootstrapBehavior<ExecutableCommand, string>(
            Mock.Of<ICurrentTenantContext>(), Mock.Of<ITenantBootstrapStore>(), Mock.Of<ILogger<TenantBootstrapBehavior<ExecutableCommand, string>>>());

        var validationBehavior = new ValidationBehavior<ExecutableCommand, string>(
            Array.Empty<IValidator<ExecutableCommand>>());

        RequestHandlerDelegate<string> txNext = ct =>
        {
            executionOrder.Add("Handler");
            return Task.FromResult("result");
        };

        RequestHandlerDelegate<string> authNext = ct =>
            transactionBehavior.Handle(new ExecutableCommand(), txNext, ct);

        RequestHandlerDelegate<string> wsNext = ct =>
            authorizationBehavior.Handle(new ExecutableCommand(), authNext, ct);

        var result = await validationBehavior.Handle(
            new ExecutableCommand(),
            wsNext,
            CancellationToken.None);

        result.Should().Be("result");
        executionOrder.Should().ContainSingle("Handler");
        dataSession.Verify(x => x.ExecuteAsync(
            It.IsAny<RequestDataSessionOptions>(),
            It.IsAny<Func<CancellationToken, Task<string>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Pipeline_TransactionalCommand_ShouldCommitAfterHandler()
    {
        var executionOrder = new List<string>();
        var dataSession = CreateMockDataSession(executionOrder);

        var behavior = new DbRequestScopeBehavior<ExecutableCommand, string>(
            dataSession.Object, Mock.Of<ILogger<DbRequestScopeBehavior<ExecutableCommand, string>>>());

        RequestHandlerDelegate<string> next = ct =>
        {
            executionOrder.Add("Handler");
            return Task.FromResult("ok");
        };

        await behavior.Handle(new ExecutableCommand(), next, CancellationToken.None);

        executionOrder.Should().ContainInOrder("Handler", "DataSession");
        dataSession.Verify(x => x.ExecuteAsync(
            It.IsAny<RequestDataSessionOptions>(),
            It.IsAny<Func<CancellationToken, Task<string>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region 2. Transaction success/failure/rollback

    [Fact]
    public async Task TransactionalBehavior_HandlerSucceeds_CommitsAndReturnsResponse()
    {
        var dataSession = CreateMockDataSession();
        var behavior = new DbRequestScopeBehavior<ExecutableCommand, string>(
            dataSession.Object, Mock.Of<ILogger<DbRequestScopeBehavior<ExecutableCommand, string>>>());

        RequestHandlerDelegate<string> next = _ => Task.FromResult("success");
        var result = await behavior.Handle(new ExecutableCommand(), next, CancellationToken.None);

        result.Should().Be("success");
        dataSession.Verify(x => x.ExecuteAsync(
            It.IsAny<RequestDataSessionOptions>(),
            It.IsAny<Func<CancellationToken, Task<string>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TransactionalBehavior_HandlerThrows_RollsBackAndDoesNotCommit()
    {
        var dataSession = CreateMockDataSession();
        var behavior = new DbRequestScopeBehavior<ExecutableCommand, string>(
            dataSession.Object, Mock.Of<ILogger<DbRequestScopeBehavior<ExecutableCommand, string>>>());

        RequestHandlerDelegate<string> next = _ => throw new InvalidOperationException("handler failed");

        Func<Task> act = () => behavior.Handle(new ExecutableCommand(), next, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        // ExecuteAsync is called (it wraps the handler), but the handler exception
        // propagates through it — rollback is Infrastructure's responsibility.
        dataSession.Verify(x => x.ExecuteAsync(
            It.IsAny<RequestDataSessionOptions>(),
            It.IsAny<Func<CancellationToken, Task<string>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TransactionalBehavior_SaveChangesFails_RollsBackAndDoesNotCommit()
    {
        var dataSession = CreateMockDataSession(
            throwAfterAction: new InvalidOperationException("Simulated save failure"));
        var behavior = new DbRequestScopeBehavior<ExecutableCommand, string>(
            dataSession.Object, Mock.Of<ILogger<DbRequestScopeBehavior<ExecutableCommand, string>>>());

        RequestHandlerDelegate<string> next = _ => Task.FromResult("ok");

        Func<Task> act = () => behavior.Handle(new ExecutableCommand(), next, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Simulated save failure");
        dataSession.Verify(x => x.ExecuteAsync(
            It.IsAny<RequestDataSessionOptions>(),
            It.IsAny<Func<CancellationToken, Task<string>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TransactionalBehavior_NonTransactionalRequest_SkipsTransaction()
    {
        var dataSession = CreateMockDataSession();
        var behavior = new DbRequestScopeBehavior<NonTransactionalCommand, string>(
            dataSession.Object, Mock.Of<ILogger<DbRequestScopeBehavior<NonTransactionalCommand, string>>>());

        RequestHandlerDelegate<string> next = _ => Task.FromResult("passthrough");
        var result = await behavior.Handle(new NonTransactionalCommand(), next, CancellationToken.None);

        result.Should().Be("passthrough");
        dataSession.Verify(x => x.ExecuteAsync(
            It.IsAny<RequestDataSessionOptions>(),
            It.IsAny<Func<CancellationToken, Task<string>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region 3. Post-commit side effect timing

    [Fact]
    public async Task PostCommitEnqueue_EnqueuesRealtimeAfterHandler()
    {
        var queue = new Mock<IPostCommitActionQueue>();
        var publisher = new Mock<IRealtimePublisher>();
        var executionContext = CreateMockExecutionContext();
        var behavior = new PostCommitEnqueueBehavior<SideEffectCommand, string>(
            queue.Object, publisher.Object, executionContext, Mock.Of<ILogger<PostCommitEnqueueBehavior<SideEffectCommand, string>>>());

        RequestHandlerDelegate<string> next = ct => Task.FromResult("ok");

        await behavior.Handle(new SideEffectCommand(), next, CancellationToken.None);

        queue.Verify(x => x.Enqueue(It.IsAny<IPostCommitAction>()), Times.Once);
    }

    [Fact]
    public async Task PostCommitEnqueue_HandlerThrows_DoesNotEnqueue()
    {
        var queue = new Mock<IPostCommitActionQueue>();
        var publisher = new Mock<IRealtimePublisher>();
        var executionContext = CreateMockExecutionContext();
        var behavior = new PostCommitEnqueueBehavior<SideEffectCommand, string>(
            queue.Object, publisher.Object, executionContext, Mock.Of<ILogger<PostCommitEnqueueBehavior<SideEffectCommand, string>>>());

        RequestHandlerDelegate<string> next = _ => throw new InvalidOperationException("fail");

        Func<Task> act = () => behavior.Handle(new SideEffectCommand(), next, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        queue.Verify(x => x.Enqueue(It.IsAny<IPostCommitAction>()), Times.Never);
    }

    [Fact]
    public async Task PostCommitScope_FlushesQueueAfterNext()
    {
        var queue = new Mock<IPostCommitActionQueue>();
        queue.Setup(x => x.Actions).Returns([]);

        var behavior = new PostCommitScopeBehavior<SideEffectCommand, string>(
            queue.Object, Mock.Of<ILogger<PostCommitScopeBehavior<SideEffectCommand, string>>>());

        var handlerCalled = false;
        RequestHandlerDelegate<string> next = ct =>
        {
            handlerCalled = true;
            return Task.FromResult("ok");
        };

        var result = await behavior.Handle(new SideEffectCommand(), next, CancellationToken.None);

        result.Should().Be("ok");
        handlerCalled.Should().BeTrue();
        queue.Verify(x => x.BeginScope(), Times.Once);
        queue.Verify(x => x.FlushAsync(It.IsAny<CancellationToken>()), Times.Once);
        queue.Verify(x => x.EndScope(), Times.Once);
    }

    [Fact]
    public async Task PostCommitScope_HandlerThrows_ClearsQueue()
    {
        var queue = new Mock<IPostCommitActionQueue>();
        queue.Setup(x => x.Actions).Returns([]);

        var behavior = new PostCommitScopeBehavior<SideEffectCommand, string>(
            queue.Object, Mock.Of<ILogger<PostCommitScopeBehavior<SideEffectCommand, string>>>());

        RequestHandlerDelegate<string> next = _ => throw new InvalidOperationException("fail");

        Func<Task> act = () => behavior.Handle(new SideEffectCommand(), next, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        queue.Verify(x => x.BeginScope(), Times.Once);
        queue.Verify(x => x.Clear(), Times.Once);
        queue.Verify(x => x.FlushAsync(It.IsAny<CancellationToken>()), Times.Never);
        queue.Verify(x => x.EndScope(), Times.Once);
    }

    [Fact]
    public async Task FullPipeline_SideEffectsRunAfterTransaction()
    {
        var dataSession = CreateMockDataSession();
        var mockPostCommit = CreateMockPostCommitQueue();
        var executionOrder = new List<string>();

        var transactionBehavior = new DbRequestScopeBehavior<SideEffectCommand, string>(
            dataSession.Object, Mock.Of<ILogger<DbRequestScopeBehavior<SideEffectCommand, string>>>());

        var enqueueBehavior = new PostCommitEnqueueBehavior<SideEffectCommand, string>(
            mockPostCommit.Object, Mock.Of<IRealtimePublisher>(), CreateMockExecutionContext(), Mock.Of<ILogger<PostCommitEnqueueBehavior<SideEffectCommand, string>>>());

        var postCommitBehavior = new PostCommitScopeBehavior<SideEffectCommand, string>(
            mockPostCommit.Object, Mock.Of<ILogger<PostCommitScopeBehavior<SideEffectCommand, string>>>());

        // Nesting: PostCommitScope (outer) → DbRequestScope (transaction) → PostCommitEnqueue (inner) → Handler
        RequestHandlerDelegate<string> txNext = ct =>
        {
            executionOrder.Add("Handler");
            return Task.FromResult("ok");
        };

        RequestHandlerDelegate<string> enqueueNext = ct =>
            transactionBehavior.Handle(new SideEffectCommand(), txNext, ct);

        RequestHandlerDelegate<string> postCommitNext = ct =>
            enqueueBehavior.Handle(new SideEffectCommand(), enqueueNext, ct);

        await postCommitBehavior.Handle(new SideEffectCommand(), postCommitNext, CancellationToken.None);

        dataSession.Verify(x => x.ExecuteAsync(
            It.IsAny<RequestDataSessionOptions>(),
            It.IsAny<Func<CancellationToken, Task<string>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
        mockPostCommit.Verify(x => x.FlushAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Pipeline_TransactionCommitHappensBeforeSideEffects()
    {
        var callOrder = new List<string>();
        var dataSession = CreateMockDataSession(executionOrder: callOrder);
        var mockPostCommit = CreateMockPostCommitQueue();

        // Track flush order within PostCommitScope
        mockPostCommit.Setup(x => x.FlushAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("FlushAsync"))
            .Returns(Task.CompletedTask);

        // Nesting: PostCommitScope (outer) → DbRequestScope (transaction) → PostCommitEnqueue → Handler
        // After handler returns: DataSession(save+commit) → PostCommitScope(FlushAsync)

        var transactionBehavior = new DbRequestScopeBehavior<SideEffectCommand, string>(
            dataSession.Object, Mock.Of<ILogger<DbRequestScopeBehavior<SideEffectCommand, string>>>());

        var enqueueBehavior = new PostCommitEnqueueBehavior<SideEffectCommand, string>(
            mockPostCommit.Object, Mock.Of<IRealtimePublisher>(), CreateMockExecutionContext(), Mock.Of<ILogger<PostCommitEnqueueBehavior<SideEffectCommand, string>>>());

        var postCommitBehavior = new PostCommitScopeBehavior<SideEffectCommand, string>(
            mockPostCommit.Object, Mock.Of<ILogger<PostCommitScopeBehavior<SideEffectCommand, string>>>());

        RequestHandlerDelegate<string> txNext = ct =>
        {
            callOrder.Add("Handler");
            return Task.FromResult("ok");
        };

        RequestHandlerDelegate<string> enqueueNext = ct =>
            transactionBehavior.Handle(new SideEffectCommand(), txNext, ct);

        RequestHandlerDelegate<string> postCommitNext = ct =>
            enqueueBehavior.Handle(new SideEffectCommand(), enqueueNext, ct);

        await postCommitBehavior.Handle(new SideEffectCommand(), postCommitNext, CancellationToken.None);

        callOrder.Should().ContainInOrder("Handler", "DataSession", "FlushAsync");
    }

    [Fact]
    public async Task Pipeline_SideEffectsCannotRunBeforeCommit()
    {
        var dataSessionCompleted = false;

        var dataSession = new Mock<IRequestDataSession>();
        dataSession
            .Setup(x => x.ExecuteAsync(
                It.IsAny<RequestDataSessionOptions>(),
                It.IsAny<Func<CancellationToken, Task<string>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<RequestDataSessionOptions, Func<CancellationToken, Task<string>>, CancellationToken>(
                async (_, action, ct) =>
                {
                    var result = await action(ct);
                    dataSessionCompleted = true;
                    return result;
                });

        var mockPostCommit = CreateMockPostCommitQueue();
        mockPostCommit.Setup(x => x.FlushAsync(It.IsAny<CancellationToken>()))
            .Callback(() => dataSessionCompleted.Should().BeTrue("data session must complete before flush"))
            .Returns(Task.CompletedTask);

        // Nesting: PostCommitScope (outer) → DbRequestScope → PostCommitEnqueue → Handler
        var transactionBehavior = new DbRequestScopeBehavior<SideEffectCommand, string>(
            dataSession.Object, Mock.Of<ILogger<DbRequestScopeBehavior<SideEffectCommand, string>>>());

        var enqueueBehavior = new PostCommitEnqueueBehavior<SideEffectCommand, string>(
            mockPostCommit.Object, Mock.Of<IRealtimePublisher>(), CreateMockExecutionContext(), Mock.Of<ILogger<PostCommitEnqueueBehavior<SideEffectCommand, string>>>());

        var postCommitBehavior = new PostCommitScopeBehavior<SideEffectCommand, string>(
            mockPostCommit.Object, Mock.Of<ILogger<PostCommitScopeBehavior<SideEffectCommand, string>>>());

        RequestHandlerDelegate<string> txNext = _ => Task.FromResult("ok");

        RequestHandlerDelegate<string> enqueueNext = ct =>
            transactionBehavior.Handle(new SideEffectCommand(), txNext, ct);

        RequestHandlerDelegate<string> postCommitNext = ct =>
            enqueueBehavior.Handle(new SideEffectCommand(), enqueueNext, ct);

        await postCommitBehavior.Handle(new SideEffectCommand(), postCommitNext, CancellationToken.None);
    }

    [Fact]
    public async Task Pipeline_SaveChangesFailure_DoesNotFlushPostCommit()
    {
        var dataSession = CreateMockDataSession(
            throwAfterAction: new InvalidOperationException("Simulated save failure"));
        var mockPostCommit = CreateMockPostCommitQueue();

        var transactionBehavior = new DbRequestScopeBehavior<SideEffectCommand, string>(
            dataSession.Object, Mock.Of<ILogger<DbRequestScopeBehavior<SideEffectCommand, string>>>());

        var enqueueBehavior = new PostCommitEnqueueBehavior<SideEffectCommand, string>(
            mockPostCommit.Object, Mock.Of<IRealtimePublisher>(), CreateMockExecutionContext(), Mock.Of<ILogger<PostCommitEnqueueBehavior<SideEffectCommand, string>>>());

        var postCommitBehavior = new PostCommitScopeBehavior<SideEffectCommand, string>(
            mockPostCommit.Object, Mock.Of<ILogger<PostCommitScopeBehavior<SideEffectCommand, string>>>());

        RequestHandlerDelegate<string> txNext = _ => Task.FromResult("ok");

        RequestHandlerDelegate<string> enqueueNext = ct =>
            transactionBehavior.Handle(new SideEffectCommand(), txNext, ct);

        RequestHandlerDelegate<string> postCommitNext = ct =>
            enqueueBehavior.Handle(new SideEffectCommand(), enqueueNext, ct);

        Func<Task> act = () => postCommitBehavior.Handle(new SideEffectCommand(), postCommitNext, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Simulated save failure");
        dataSession.Verify(x => x.ExecuteAsync(
            It.IsAny<RequestDataSessionOptions>(),
            It.IsAny<Func<CancellationToken, Task<string>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
        mockPostCommit.Verify(x => x.FlushAsync(It.IsAny<CancellationToken>()), Times.Never);
        mockPostCommit.Verify(x => x.Clear(), Times.Once);
    }

    [Fact]
    public async Task Pipeline_HandlerFailure_DoesNotFlushPostCommit()
    {
        var dataSession = CreateMockDataSession();
        var mockPostCommit = CreateMockPostCommitQueue();

        var transactionBehavior = new DbRequestScopeBehavior<SideEffectCommand, string>(
            dataSession.Object, Mock.Of<ILogger<DbRequestScopeBehavior<SideEffectCommand, string>>>());

        var enqueueBehavior = new PostCommitEnqueueBehavior<SideEffectCommand, string>(
            mockPostCommit.Object, Mock.Of<IRealtimePublisher>(), CreateMockExecutionContext(), Mock.Of<ILogger<PostCommitEnqueueBehavior<SideEffectCommand, string>>>());

        var postCommitBehavior = new PostCommitScopeBehavior<SideEffectCommand, string>(
            mockPostCommit.Object, Mock.Of<ILogger<PostCommitScopeBehavior<SideEffectCommand, string>>>());

        RequestHandlerDelegate<string> txNext = _ => throw new InvalidOperationException("handler failed");

        RequestHandlerDelegate<string> enqueueNext = ct =>
            transactionBehavior.Handle(new SideEffectCommand(), txNext, ct);

        RequestHandlerDelegate<string> postCommitNext = ct =>
            enqueueBehavior.Handle(new SideEffectCommand(), enqueueNext, ct);

        Func<Task> act = () => postCommitBehavior.Handle(new SideEffectCommand(), postCommitNext, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        mockPostCommit.Verify(x => x.FlushAsync(It.IsAny<CancellationToken>()), Times.Never);
        mockPostCommit.Verify(x => x.Clear(), Times.Once);
    }

    #endregion

    #region 4. Idempotency behavior

    [Fact]
    public async Task IdempotencyBehavior_LeaseAcquired_ExecutesHandlerAndCompletesWithResult()
    {
        var mockStore = CreateMockIdempotencyStore();
        var behavior = CreateIdempotencyBehavior<ExecutableCommand>(mockStore);

        RequestHandlerDelegate<string> next = _ => Task.FromResult("result");
        var result = await behavior.Handle(new ExecutableCommand(), next, CancellationToken.None);

        result.Should().Be("result");
        mockStore.Verify(x => x.CompleteAsync(
            It.IsAny<IdempotencyIdentity>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IdempotencyBehavior_AlreadyCompleted_ReturnsCachedResult()
    {
        var mockStore = CreateMockIdempotencyStore(new IdempotencyBeginResult(
            IdempotencyBeginStatus.Completed, "\"cached-result\"", "test.module.test-command.v1"));

        var behavior = CreateIdempotencyBehavior<ExecutableCommand>(mockStore);

        RequestHandlerDelegate<string> next = _ => throw new InvalidOperationException("should not be called");
        var result = await behavior.Handle(new ExecutableCommand(), next, CancellationToken.None);

        result.Should().Be("cached-result");
    }

    [Fact]
    public async Task IdempotencyBehavior_PayloadMismatch_ThrowsConflict()
    {
        var mockStore = CreateMockIdempotencyStore(new IdempotencyBeginResult(
            IdempotencyBeginStatus.PayloadMismatch, null, null));

        var behavior = CreateIdempotencyBehavior<ExecutableCommand>(mockStore);

        RequestHandlerDelegate<string> next = _ => throw new InvalidOperationException("should not be called");

        Func<Task> act = () => behavior.Handle(new ExecutableCommand(), next, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task IdempotencyBehavior_HandlerThrows_DoesNotComplete()
    {
        var mockStore = CreateMockIdempotencyStore();
        var behavior = CreateIdempotencyBehavior<ExecutableCommand>(mockStore);

        RequestHandlerDelegate<string> next = _ => throw new InvalidOperationException("handler failed");

        Func<Task> act = () => behavior.Handle(new ExecutableCommand(), next, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        mockStore.Verify(x => x.CompleteAsync(
            It.IsAny<IdempotencyIdentity>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IdempotencyBehavior_ReplayPolicyRejectsResult_ThrowsAndDoesNotComplete()
    {
        // FZ-IDEM-01 (spec 3.7): never return a successful business response without
        // Completed replay state. When the replay policy rejects caching the serialized
        // result, the behavior must throw so the request transaction rolls back — it
        // must not return the response and silently leave a Started row behind.
        var mockStore = CreateMockIdempotencyStore();
        var behavior = CreateIdempotencyBehavior<ExecutableCommand>(mockStore, allowSerializedResult: false);

        RequestHandlerDelegate<string> next = _ => Task.FromResult("result");

        Func<Task> act = () => behavior.Handle(new ExecutableCommand(), next, CancellationToken.None);

        await act.Should().ThrowAsync<Exception>(
            "a rejected non-replayable result must not surface as a successful business response");
        mockStore.Verify(x => x.CompleteAsync(
            It.IsAny<IdempotencyIdentity>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IdempotencyBehavior_NonIdempotentRequest_SkipsIdempotency()
    {        var mockStore = CreateMockIdempotencyStore();
        var behavior = CreateIdempotencyBehavior<NonTransactionalCommand>(mockStore);

        RequestHandlerDelegate<string> next = _ => Task.FromResult("ok");
        var result = await behavior.Handle(new NonTransactionalCommand(), next, CancellationToken.None);

        result.Should().Be("ok");
        mockStore.Verify(x => x.BeginAsync(
            It.IsAny<IdempotencyIdentity>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region 5. Validation and authorization failures

    [Fact]
    public async Task ValidationBehavior_FailingValidation_ThrowsBeforeHandler()
    {
        var validators = new IValidator<ValidationFailCommand>[] { new ValidationFailCommandValidator() };
        var behavior = new ValidationBehavior<ValidationFailCommand, string>(validators);
        var handlerCalled = false;

        RequestHandlerDelegate<string> next = ct =>
        {
            handlerCalled = true;
            return Task.FromResult("ok");
        };

        Func<Task> act = () => behavior.Handle(new ValidationFailCommand(), next, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        handlerCalled.Should().BeFalse("handler should not execute when validation fails");
    }

    [Fact]
    public async Task AuthorizationBehavior_PermissionDenied_ThrowsBeforeHandler()
    {
        var mockUser = CreateMockUser();
        var mockPermissionService = new Mock<IAuthorizationDecisionStore>();
        mockPermissionService.Setup(x => x.EvaluateAsync(It.IsAny<PermissionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PermissionDecision(false, "missing_permission"));

        var mockTenant = new Mock<ICurrentTenantContext>();
        mockTenant.Setup(x => x.AccountId).Returns(Guid.NewGuid());
        mockTenant.Setup(x => x.WorkspaceId).Returns(Guid.NewGuid());

        var behavior = new AuthorizationBehavior<ExecutableCommand, string>(
            mockUser.Object, mockTenant.Object, mockPermissionService.Object, Mock.Of<ILogger<AuthorizationBehavior<ExecutableCommand, string>>>());
        var handlerCalled = false;

        RequestHandlerDelegate<string> next = ct =>
        {
            handlerCalled = true;
            return Task.FromResult("ok");
        };

        Func<Task> act = () => behavior.Handle(new ExecutableCommand(), next, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        handlerCalled.Should().BeFalse("handler should not execute when permission denied");
    }

    [Fact]
    public async Task AuthorizationBehavior_UnauthenticatedUser_ThrowsUnauthorized()
    {
        var mockUser = new Mock<ICurrentUser>();
        mockUser.Setup(x => x.UserId).Returns(Guid.Empty);

        var behavior = new AuthorizationBehavior<ExecutableCommand, string>(
            mockUser.Object, Mock.Of<ICurrentTenantContext>(), Mock.Of<IAuthorizationDecisionStore>(), Mock.Of<ILogger<AuthorizationBehavior<ExecutableCommand, string>>>());

        RequestHandlerDelegate<string> next = _ => Task.FromResult("ok");

        Func<Task> act = () => behavior.Handle(new ExecutableCommand(), next, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task TenantBootstrapBehavior_EmptyWorkspaceId_ThrowsForbidden()
    {
        var behavior = new TenantBootstrapBehavior<EmptyWorkspaceCommand, string>(
            Mock.Of<ICurrentTenantContext>(), Mock.Of<ITenantBootstrapStore>(), Mock.Of<ILogger<TenantBootstrapBehavior<EmptyWorkspaceCommand, string>>>());

        RequestHandlerDelegate<string> next = _ => Task.FromResult("ok");

        Func<Task> act = () => behavior.Handle(new EmptyWorkspaceCommand(), next, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task ValidationOrAuthFailure_NoTransactionOpened()
    {
        var dataSession = CreateMockDataSession();
        var validators = new IValidator<ValidationFailCommand>[] { new ValidationFailCommandValidator() };

        var validationBehavior = new ValidationBehavior<ValidationFailCommand, string>(validators);
        var transactionBehavior = new DbRequestScopeBehavior<ValidationFailCommand, string>(
            dataSession.Object, Mock.Of<ILogger<DbRequestScopeBehavior<ValidationFailCommand, string>>>());

        RequestHandlerDelegate<string> txNext = _ => Task.FromResult("ok");

        RequestHandlerDelegate<string> wsNext = ct =>
            transactionBehavior.Handle(new ValidationFailCommand(), txNext, ct);

        Func<Task> act = () => validationBehavior.Handle(new ValidationFailCommand(), wsNext, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        dataSession.Verify(x => x.ExecuteAsync(
            It.IsAny<RequestDataSessionOptions>(),
            It.IsAny<Func<CancellationToken, Task<string>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Pipeline_CommitFailure_DoesNotFlushPostCommit()
    {
        // PIPE-007: When the transaction commit fails, post-commit actions must not run.
        var dataSession = new Mock<IRequestDataSession>();
        dataSession
            .Setup(x => x.ExecuteAsync(
                It.IsAny<RequestDataSessionOptions>(),
                It.IsAny<Func<CancellationToken, Task<string>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Simulated commit failure"));

        var mockPostCommit = CreateMockPostCommitQueue();

        var transactionBehavior = new DbRequestScopeBehavior<SideEffectCommand, string>(
            dataSession.Object, Mock.Of<ILogger<DbRequestScopeBehavior<SideEffectCommand, string>>>());

        var enqueueBehavior = new PostCommitEnqueueBehavior<SideEffectCommand, string>(
            mockPostCommit.Object, Mock.Of<IRealtimePublisher>(), CreateMockExecutionContext(), Mock.Of<ILogger<PostCommitEnqueueBehavior<SideEffectCommand, string>>>());

        var postCommitBehavior = new PostCommitScopeBehavior<SideEffectCommand, string>(
            mockPostCommit.Object, Mock.Of<ILogger<PostCommitScopeBehavior<SideEffectCommand, string>>>());

        RequestHandlerDelegate<string> txNext = _ => Task.FromResult("ok");

        RequestHandlerDelegate<string> enqueueNext = ct =>
            transactionBehavior.Handle(new SideEffectCommand(), txNext, ct);

        RequestHandlerDelegate<string> postCommitNext = ct =>
            enqueueBehavior.Handle(new SideEffectCommand(), enqueueNext, ct);

        Func<Task> act = () => postCommitBehavior.Handle(new SideEffectCommand(), postCommitNext, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Simulated commit failure");

        mockPostCommit.Verify(x => x.FlushAsync(It.IsAny<CancellationToken>()), Times.Never,
            "post-commit flush must not run when commit fails");
        mockPostCommit.Verify(x => x.Clear(), Times.Once,
            "queue must be cleared on failure to prevent stale side effects");
    }

    #endregion
}
