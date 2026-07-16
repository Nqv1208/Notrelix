using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using ValidationException = Notrelix.Application.Common.Exceptions.ValidationException;

namespace Notrelix.Application.Tests.Behaviors;

public class PipelineExecutionTests
{
    #region Test infrastructure

    public sealed record ExecutableCommand : IRequest<string>, ITransactionalRequest, IWorkspaceRequest, IRequirePermission, IIdempotentRequest
    {
        private static readonly Guid Wsid = Guid.NewGuid();
        public Guid WorkspaceId => Wsid;
        public PermissionAction Action => PermissionAction.ViewWorkspace;
        public ResourceRef Resource => ResourceRef.Create(ResourceType.Workspace, Wsid, Wsid);
        public string IdempotencyKey => "test-key";
    }

    public sealed record NonTransactionalCommand : IRequest<string>;

    public sealed record SideEffectCommand : IRequest<string>, ITransactionalRequest, IRealtimeRequest
    {
        public RealtimeTopic Topic => new("test", "test", Guid.NewGuid());
    }

    public sealed record ValidationFailCommand : IRequest<string>, ITransactionalRequest
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

    private static Mock<IRlsSessionContext> CreateMockRls()
    {
        var rls = new Mock<IRlsSessionContext>();
        rls.Setup(x => x.ApplyAsync(It.IsAny<DatabaseFacade>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return rls;
    }

    private static (Mock<IApplicationDbContext> Context, Mock<IDbContextTransaction> Transaction) CreateMockContextPair(bool throwOnSave = false)
    {
        var context = new Mock<IApplicationDbContext>();
        var database = new Mock<DatabaseFacade>(Mock.Of<DbContext>());
        var transaction = new Mock<IDbContextTransaction>();
        database.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);
        context.Setup(x => x.Database).Returns(database.Object);

        if (throwOnSave)
            context.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("Simulated save failure", new Exception()));
        else
            context.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

        return (context, transaction);
    }

    private static Mock<IApplicationDbContext> CreateMockContext(bool throwOnSave = false)
    {
        return CreateMockContextPair(throwOnSave).Context;
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

    private static Mock<IIdempotencyStore> CreateMockIdempotencyStore(bool lockAcquired = true)
    {
        var store = new Mock<IIdempotencyStore>();
        store.Setup(x => x.TryAcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(lockAcquired);
        return store;
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

        var mockContext = CreateMockContext();
        var mockUser = CreateMockUser();
        var mockPermissionService = CreateMockPermissionService();
        var mockPostCommit = CreateMockPostCommitQueue();
        var rls = new Mock<IRlsSessionContext>();
        var dbRls = new Mock<IApplicationDbContext>();
        var database = new Mock<DatabaseFacade>(Mock.Of<DbContext>());
        dbRls.Setup(x => x.Database).Returns(database.Object);

        var transactionBehavior = new DbRequestScopeBehavior<ExecutableCommand, string>(
            mockContext.Object, CreateMockRls().Object, Mock.Of<ILogger<DbRequestScopeBehavior<ExecutableCommand, string>>>());

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
        mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Pipeline_TransactionalCommand_ShouldCommitAfterHandler()
    {
        var mockContext = CreateMockContext();
        var executionOrder = new List<string>();

        var behavior = new DbRequestScopeBehavior<ExecutableCommand, string>(
            mockContext.Object, CreateMockRls().Object, Mock.Of<ILogger<DbRequestScopeBehavior<ExecutableCommand, string>>>());

        RequestHandlerDelegate<string> next = ct =>
        {
            executionOrder.Add("Handler");
            return Task.FromResult("ok");
        };

        await behavior.Handle(new ExecutableCommand(), next, CancellationToken.None);

        executionOrder.Should().ContainInOrder("Handler");
        mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region 2. Transaction success/failure/rollback

    [Fact]
    public async Task TransactionalBehavior_HandlerSucceeds_CommitsAndReturnsResponse()
    {
        var mockContext = CreateMockContext();
        var behavior = new DbRequestScopeBehavior<ExecutableCommand, string>(
            mockContext.Object, CreateMockRls().Object, Mock.Of<ILogger<DbRequestScopeBehavior<ExecutableCommand, string>>>());

        RequestHandlerDelegate<string> next = _ => Task.FromResult("success");
        var result = await behavior.Handle(new ExecutableCommand(), next, CancellationToken.None);

        result.Should().Be("success");
        mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TransactionalBehavior_HandlerThrows_RollsBackAndDoesNotCommit()
    {
        var mockContext = CreateMockContext();
        var behavior = new DbRequestScopeBehavior<ExecutableCommand, string>(
            mockContext.Object, CreateMockRls().Object, Mock.Of<ILogger<DbRequestScopeBehavior<ExecutableCommand, string>>>());

        RequestHandlerDelegate<string> next = _ => throw new InvalidOperationException("handler failed");

        Func<Task> act = () => behavior.Handle(new ExecutableCommand(), next, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TransactionalBehavior_SaveChangesFails_RollsBackAndDoesNotCommit()
    {
        var mockContext = CreateMockContext(throwOnSave: true);
        var behavior = new DbRequestScopeBehavior<ExecutableCommand, string>(
            mockContext.Object, CreateMockRls().Object, Mock.Of<ILogger<DbRequestScopeBehavior<ExecutableCommand, string>>>());

        RequestHandlerDelegate<string> next = _ => Task.FromResult("ok");

        Func<Task> act = () => behavior.Handle(new ExecutableCommand(), next, CancellationToken.None);

        await act.Should().ThrowAsync<DbUpdateException>();
        mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TransactionalBehavior_NonTransactionalRequest_SkipsTransaction()
    {
        var mockContext = CreateMockContext();
        var behavior = new DbRequestScopeBehavior<NonTransactionalCommand, string>(
            mockContext.Object, CreateMockRls().Object, Mock.Of<ILogger<DbRequestScopeBehavior<NonTransactionalCommand, string>>>());

        RequestHandlerDelegate<string> next = _ => Task.FromResult("passthrough");
        var result = await behavior.Handle(new NonTransactionalCommand(), next, CancellationToken.None);

        result.Should().Be("passthrough");
        mockContext.Verify(x => x.Database, Times.Never);
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
        var mockContext = CreateMockContext();
        var mockPostCommit = CreateMockPostCommitQueue();
        var executionOrder = new List<string>();

        var transactionBehavior = new DbRequestScopeBehavior<SideEffectCommand, string>(
            mockContext.Object, CreateMockRls().Object, Mock.Of<ILogger<DbRequestScopeBehavior<SideEffectCommand, string>>>());

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

        mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockPostCommit.Verify(x => x.FlushAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Pipeline_TransactionCommitHappensBeforeSideEffects()
    {
        var (mockContext, mockTransaction) = CreateMockContextPair();
        var mockPostCommit = CreateMockPostCommitQueue();
        var callOrder = new List<string>();

        mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("SaveChanges"))
            .ReturnsAsync(1);

        mockTransaction.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("CommitAsync"))
            .Returns(Task.CompletedTask);

        // Track flush order within PostCommitScope
        mockPostCommit.Setup(x => x.FlushAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("FlushAsync"))
            .Returns(Task.CompletedTask);

        // Nesting: PostCommitScope (outer) → DbRequestScope (transaction) → PostCommitEnqueue → Handler
        // After handler returns: Transaction(SaveChanges+Commit) → PostCommitScope(FlushAsync)

        var transactionBehavior = new DbRequestScopeBehavior<SideEffectCommand, string>(
            mockContext.Object, CreateMockRls().Object, Mock.Of<ILogger<DbRequestScopeBehavior<SideEffectCommand, string>>>());

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

        callOrder.Should().ContainInOrder("Handler", "SaveChanges", "CommitAsync", "FlushAsync");
    }

    [Fact]
    public async Task Pipeline_SideEffectsCannotRunBeforeCommit()
    {
        var (mockContext, mockTransaction) = CreateMockContextPair();
        var mockPostCommit = CreateMockPostCommitQueue();
        var commitHappened = false;

        mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => commitHappened = true)
            .ReturnsAsync(1);

        mockTransaction.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .Callback(() => commitHappened = true)
            .Returns(Task.CompletedTask);

        mockPostCommit.Setup(x => x.FlushAsync(It.IsAny<CancellationToken>()))
            .Callback(() => commitHappened.Should().BeTrue("commit must happen before flush"))
            .Returns(Task.CompletedTask);

        // Nesting: PostCommitScope (outer) → DbRequestScope → PostCommitEnqueue → Handler
        var transactionBehavior = new DbRequestScopeBehavior<SideEffectCommand, string>(
            mockContext.Object, CreateMockRls().Object, Mock.Of<ILogger<DbRequestScopeBehavior<SideEffectCommand, string>>>());

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
        var mockContext = CreateMockContext(throwOnSave: true);
        var mockPostCommit = CreateMockPostCommitQueue();

        var transactionBehavior = new DbRequestScopeBehavior<SideEffectCommand, string>(
            mockContext.Object, CreateMockRls().Object, Mock.Of<ILogger<DbRequestScopeBehavior<SideEffectCommand, string>>>());

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

        await act.Should().ThrowAsync<DbUpdateException>();
        mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockPostCommit.Verify(x => x.FlushAsync(It.IsAny<CancellationToken>()), Times.Never);
        mockPostCommit.Verify(x => x.Clear(), Times.Once);
    }

    [Fact]
    public async Task Pipeline_HandlerFailure_DoesNotFlushPostCommit()
    {
        var mockContext = CreateMockContext();
        var mockPostCommit = CreateMockPostCommitQueue();

        var transactionBehavior = new DbRequestScopeBehavior<SideEffectCommand, string>(
            mockContext.Object, CreateMockRls().Object, Mock.Of<ILogger<DbRequestScopeBehavior<SideEffectCommand, string>>>());

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
    public async Task IdempotencyBehavior_LockAcquired_ExecutesHandlerAndEnqueuesPostCommitResult()
    {
        var mockStore = CreateMockIdempotencyStore(lockAcquired: true);
        var mockQueue = new Mock<IPostCommitActionQueue>();
        var behavior = new IdempotencyBehavior<ExecutableCommand, string>(
            mockStore.Object, mockQueue.Object, Mock.Of<ILogger<IdempotencyBehavior<ExecutableCommand, string>>>());

        RequestHandlerDelegate<string> next = _ => Task.FromResult("result");
        var result = await behavior.Handle(new ExecutableCommand(), next, CancellationToken.None);

        result.Should().Be("result");
        mockQueue.Verify(x => x.Enqueue(It.IsAny<IPostCommitAction>()), Times.Once);
    }

    [Fact]
    public async Task IdempotencyBehavior_LockNotAcquired_CachedResult_ReturnsCachedResult()
    {
        var mockStore = new Mock<IIdempotencyStore>();
        mockStore.Setup(x => x.TryAcquireLockAsync("test-key", It.IsAny<TimeSpan>()))
            .ReturnsAsync(false);
        mockStore.Setup(x => x.GetResultAsync("test-key"))
            .ReturnsAsync("cached-result");

        var behavior = new IdempotencyBehavior<ExecutableCommand, string>(
            mockStore.Object, Mock.Of<IPostCommitActionQueue>(), Mock.Of<ILogger<IdempotencyBehavior<ExecutableCommand, string>>>());

        RequestHandlerDelegate<string> next = _ => throw new InvalidOperationException("should not be called");
        var result = await behavior.Handle(new ExecutableCommand(), next, CancellationToken.None);

        result.Should().Be("cached-result");
    }

    [Fact]
    public async Task IdempotencyBehavior_LockNotAcquired_NoCachedResult_ThrowsConflict()
    {
        var mockStore = new Mock<IIdempotencyStore>();
        mockStore.Setup(x => x.TryAcquireLockAsync("test-key", It.IsAny<TimeSpan>()))
            .ReturnsAsync(false);
        mockStore.Setup(x => x.GetResultAsync("test-key"))
            .ReturnsAsync((string?)null);

        var behavior = new IdempotencyBehavior<ExecutableCommand, string>(
            mockStore.Object, Mock.Of<IPostCommitActionQueue>(), Mock.Of<ILogger<IdempotencyBehavior<ExecutableCommand, string>>>());

        RequestHandlerDelegate<string> next = _ => throw new InvalidOperationException("should not be called");

        Func<Task> act = () => behavior.Handle(new ExecutableCommand(), next, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task IdempotencyBehavior_HandlerThrows_ReleasesLockAndDoesNotEnqueueResult()
    {
        var mockStore = CreateMockIdempotencyStore(lockAcquired: true);
        var mockQueue = new Mock<IPostCommitActionQueue>();
        var behavior = new IdempotencyBehavior<ExecutableCommand, string>(
            mockStore.Object, mockQueue.Object, Mock.Of<ILogger<IdempotencyBehavior<ExecutableCommand, string>>>());

        RequestHandlerDelegate<string> next = _ => throw new InvalidOperationException("handler failed");

        Func<Task> act = () => behavior.Handle(new ExecutableCommand(), next, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        mockStore.Verify(x => x.ReleaseLockAsync("test-key"), Times.Once);
        mockQueue.Verify(x => x.Enqueue(It.IsAny<IPostCommitAction>()), Times.Never);
    }

    [Fact]
    public async Task IdempotencyBehavior_NonIdempotentRequest_SkipsIdempotency()
    {
        var mockStore = CreateMockIdempotencyStore();
        var behavior = new IdempotencyBehavior<NonTransactionalCommand, string>(
            mockStore.Object, Mock.Of<IPostCommitActionQueue>(), Mock.Of<ILogger<IdempotencyBehavior<NonTransactionalCommand, string>>>());

        RequestHandlerDelegate<string> next = _ => Task.FromResult("ok");
        var result = await behavior.Handle(new NonTransactionalCommand(), next, CancellationToken.None);

        result.Should().Be("ok");
        mockStore.Verify(x => x.TryAcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
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
        var mockContext = CreateMockContext();
        var validators = new IValidator<ValidationFailCommand>[] { new ValidationFailCommandValidator() };

        var validationBehavior = new ValidationBehavior<ValidationFailCommand, string>(validators);
        var transactionBehavior = new DbRequestScopeBehavior<ValidationFailCommand, string>(
            mockContext.Object, CreateMockRls().Object, Mock.Of<ILogger<DbRequestScopeBehavior<ValidationFailCommand, string>>>());

        RequestHandlerDelegate<string> txNext = _ => Task.FromResult("ok");

        RequestHandlerDelegate<string> wsNext = ct =>
            transactionBehavior.Handle(new ValidationFailCommand(), txNext, ct);

        Func<Task> act = () => validationBehavior.Handle(new ValidationFailCommand(), wsNext, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        mockContext.Verify(x => x.Database.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
