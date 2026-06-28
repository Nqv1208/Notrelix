using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Behaviors;
using Notrelix.Application.Common.CQRS;
using Notrelix.Application.Common.Exceptions;
using Notrelix.Application.Common.Security;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.SharedKernel;
using ValidationException = Notrelix.Application.Common.Exceptions.ValidationException;

namespace Notrelix.Application.Tests.Behaviors;

public class PipelineExecutionTests
{
    #region Test infrastructure

    public sealed record ExecutableCommand : IRequest<string>, ITransactionalRequest, IWorkspaceRequest, IRequirePermission, IIdempotentRequest
    {
        public Guid WorkspaceId => Guid.NewGuid();
        public PermissionAction Action => PermissionAction.ViewWorkspace;
        public ResourceRef Resource => ResourceRef.Create(ResourceType.Workspace, Guid.NewGuid());
        public string IdempotencyKey => "test-key";
    }

    public sealed record NonTransactionalCommand : IRequest<string>;

    public sealed record SideEffectCommand : IRequest<string>, ITransactionalRequest, IInvalidateCacheRequest, IRealtimeRequest
    {
        public RealtimeTopic Topic => new("test", "test", Guid.NewGuid());
        public IReadOnlyCollection<CacheInvalidationKey> GetInvalidationKeys() =>
            [new CacheInvalidationKey("test:*")];
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

    private static Mock<IPermissionService> CreateMockPermissionService(bool allowed = true)
    {
        var service = new Mock<IPermissionService>();
        service.Setup(x => x.EvaluateAsync(It.IsAny<PermissionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PermissionDecision(allowed, "test"));
        return service;
    }

    private static Mock<IWorkspacePermissionService> CreateMockWorkspacePermissionService(bool canView = true)
    {
        var service = new Mock<IWorkspacePermissionService>();
        service.Setup(x => x.CanViewWorkspaceAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(canView);
        return service;
    }

    private static Mock<IIdempotencyStore> CreateMockIdempotencyStore(bool lockAcquired = true)
    {
        var store = new Mock<IIdempotencyStore>();
        store.Setup(x => x.TryAcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(lockAcquired);
        return store;
    }

    private static Mock<IRedisCacheService> CreateMockCacheService() => new();
    private static Mock<IRealtimePublisher> CreateMockRealtimePublisher() => new();

    #endregion

    #region 1. Pipeline execution order (runtime proof)

    [Fact]
    public async Task Pipeline_ShouldExecuteInCorrectOrder_TransactionalCommand()
    {
        var executionOrder = new List<string>();

        var mockContext = CreateMockContext();
        var mockUser = CreateMockUser();
        var mockPermissionService = CreateMockPermissionService();
        var mockWorkspaceService = CreateMockWorkspacePermissionService();
        var mockCache = CreateMockCacheService();
        var mockRealtime = CreateMockRealtimePublisher();

        var transactionBehavior = new TransactionBehavior<ExecutableCommand, string>(
            mockContext.Object, Mock.Of<ILogger<TransactionBehavior<ExecutableCommand, string>>>());

        var realtimeBehavior = new RealtimeBehavior<ExecutableCommand, string>(
            mockRealtime.Object, Mock.Of<ILogger<RealtimeBehavior<ExecutableCommand, string>>>());

        var cacheInvalidationBehavior = new CacheInvalidationBehavior<ExecutableCommand, string>(
            mockCache.Object, Mock.Of<ILogger<CacheInvalidationBehavior<ExecutableCommand, string>>>());

        var authorizationBehavior = new AuthorizationBehavior<ExecutableCommand, string>(
            mockUser.Object, mockPermissionService.Object);

        var workspaceBehavior = new WorkspaceContextBehavior<ExecutableCommand, string>(
            mockUser.Object, Mock.Of<ICurrentWorkspace>(), mockWorkspaceService.Object);

        var validationBehavior = new ValidationBehavior<ExecutableCommand, string>(
            Array.Empty<IValidator<ExecutableCommand>>());

        RequestHandlerDelegate<string> txNext = ct =>
        {
            executionOrder.Add("Handler");
            return Task.FromResult("result");
        };

        RequestHandlerDelegate<string> realtimeNext = ct =>
            transactionBehavior.Handle(new ExecutableCommand(), txNext, ct);

        RequestHandlerDelegate<string> cacheNext = ct =>
            realtimeBehavior.Handle(new ExecutableCommand(), realtimeNext, ct);

        RequestHandlerDelegate<string> authNext = ct =>
            cacheInvalidationBehavior.Handle(new ExecutableCommand(), cacheNext, ct);

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

        var behavior = new TransactionBehavior<ExecutableCommand, string>(
            mockContext.Object, Mock.Of<ILogger<TransactionBehavior<ExecutableCommand, string>>>());

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
    public async Task TransactionBehavior_HandlerSucceeds_CommitsAndReturnsResponse()
    {
        var mockContext = CreateMockContext();
        var behavior = new TransactionBehavior<ExecutableCommand, string>(
            mockContext.Object, Mock.Of<ILogger<TransactionBehavior<ExecutableCommand, string>>>());

        RequestHandlerDelegate<string> next = _ => Task.FromResult("success");
        var result = await behavior.Handle(new ExecutableCommand(), next, CancellationToken.None);

        result.Should().Be("success");
        mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TransactionBehavior_HandlerThrows_RollsBackAndDoesNotCommit()
    {
        var mockContext = CreateMockContext();
        var behavior = new TransactionBehavior<ExecutableCommand, string>(
            mockContext.Object, Mock.Of<ILogger<TransactionBehavior<ExecutableCommand, string>>>());

        RequestHandlerDelegate<string> next = _ => throw new InvalidOperationException("handler failed");

        Func<Task> act = () => behavior.Handle(new ExecutableCommand(), next, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TransactionBehavior_SaveChangesFails_RollsBackAndDoesNotCommit()
    {
        var mockContext = CreateMockContext(throwOnSave: true);
        var behavior = new TransactionBehavior<ExecutableCommand, string>(
            mockContext.Object, Mock.Of<ILogger<TransactionBehavior<ExecutableCommand, string>>>());

        RequestHandlerDelegate<string> next = _ => Task.FromResult("ok");

        Func<Task> act = () => behavior.Handle(new ExecutableCommand(), next, CancellationToken.None);

        await act.Should().ThrowAsync<DbUpdateException>();
        mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TransactionBehavior_NonTransactionalRequest_SkipsTransaction()
    {
        var mockContext = CreateMockContext();
        var behavior = new TransactionBehavior<NonTransactionalCommand, string>(
            mockContext.Object, Mock.Of<ILogger<TransactionBehavior<NonTransactionalCommand, string>>>());

        RequestHandlerDelegate<string> next = _ => Task.FromResult("passthrough");
        var result = await behavior.Handle(new NonTransactionalCommand(), next, CancellationToken.None);

        result.Should().Be("passthrough");
        mockContext.Verify(x => x.Database, Times.Never);
    }

    #endregion

    #region 3. Post-commit side effect timing

    [Fact]
    public async Task CacheInvalidation_AfterNext_CalledAfterHandler()
    {
        var mockCache = CreateMockCacheService();
        var executionOrder = new List<string>();

        var behavior = new CacheInvalidationBehavior<SideEffectCommand, string>(
            mockCache.Object, Mock.Of<ILogger<CacheInvalidationBehavior<SideEffectCommand, string>>>());

        RequestHandlerDelegate<string> next = ct =>
        {
            executionOrder.Add("Handler");
            return Task.FromResult("ok");
        };

        var result = await behavior.Handle(new SideEffectCommand(), next, CancellationToken.None);

        executionOrder.Should().ContainInOrder("Handler");
        mockCache.Verify(x => x.RemoveAsync("test:*"), Times.Once);
    }

    [Fact]
    public async Task CacheInvalidation_HandlerThrows_DoesNotInvalidateCache()
    {
        var mockCache = CreateMockCacheService();
        var behavior = new CacheInvalidationBehavior<SideEffectCommand, string>(
            mockCache.Object, Mock.Of<ILogger<CacheInvalidationBehavior<SideEffectCommand, string>>>());

        RequestHandlerDelegate<string> next = _ => throw new InvalidOperationException("fail");

        Func<Task> act = () => behavior.Handle(new SideEffectCommand(), next, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        mockCache.Verify(x => x.RemoveAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RealtimeBehavior_AfterNext_CalledAfterHandler()
    {
        var mockRealtime = CreateMockRealtimePublisher();
        var executionOrder = new List<string>();

        var behavior = new RealtimeBehavior<SideEffectCommand, string>(
            mockRealtime.Object, Mock.Of<ILogger<RealtimeBehavior<SideEffectCommand, string>>>());

        RequestHandlerDelegate<string> next = ct =>
        {
            executionOrder.Add("Handler");
            return Task.FromResult("ok");
        };

        var result = await behavior.Handle(new SideEffectCommand(), next, CancellationToken.None);

        executionOrder.Should().ContainInOrder("Handler");
        mockRealtime.Verify(x => x.PublishAsync(It.IsAny<RealtimeTopic>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RealtimeBehavior_HandlerThrows_DoesNotPublish()
    {
        var mockRealtime = CreateMockRealtimePublisher();
        var behavior = new RealtimeBehavior<SideEffectCommand, string>(
            mockRealtime.Object, Mock.Of<ILogger<RealtimeBehavior<SideEffectCommand, string>>>());

        RequestHandlerDelegate<string> next = _ => throw new InvalidOperationException("fail");

        Func<Task> act = () => behavior.Handle(new SideEffectCommand(), next, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        mockRealtime.Verify(x => x.PublishAsync(It.IsAny<RealtimeTopic>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FullPipeline_SideEffectsRunAfterTransaction()
    {
        var mockContext = CreateMockContext();
        var mockCache = CreateMockCacheService();
        var mockRealtime = CreateMockRealtimePublisher();
        var executionOrder = new List<string>();

        var transactionBehavior = new TransactionBehavior<SideEffectCommand, string>(
            mockContext.Object, Mock.Of<ILogger<TransactionBehavior<SideEffectCommand, string>>>());

        var realtimeBehavior = new RealtimeBehavior<SideEffectCommand, string>(
            mockRealtime.Object, Mock.Of<ILogger<RealtimeBehavior<SideEffectCommand, string>>>());

        var cacheInvalidationBehavior = new CacheInvalidationBehavior<SideEffectCommand, string>(
            mockCache.Object, Mock.Of<ILogger<CacheInvalidationBehavior<SideEffectCommand, string>>>());

        RequestHandlerDelegate<string> txNext = ct =>
        {
            executionOrder.Add("Handler");
            return Task.FromResult("ok");
        };

        RequestHandlerDelegate<string> realtimeNext = ct =>
            transactionBehavior.Handle(new SideEffectCommand(), txNext, ct);

        RequestHandlerDelegate<string> cacheNext = ct =>
            realtimeBehavior.Handle(new SideEffectCommand(), realtimeNext, ct);

        await cacheInvalidationBehavior.Handle(new SideEffectCommand(), cacheNext, CancellationToken.None);

        mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockCache.Verify(x => x.RemoveAsync("test:*"), Times.Once);
        mockRealtime.Verify(x => x.PublishAsync(It.IsAny<RealtimeTopic>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Pipeline_TransactionCommitHappensBeforeSideEffects()
    {
        var (mockContext, mockTransaction) = CreateMockContextPair();
        var mockCache = CreateMockCacheService();
        var mockRealtime = CreateMockRealtimePublisher();
        var callOrder = new List<string>();

        mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("SaveChanges"))
            .ReturnsAsync(1);

        mockTransaction.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("CommitAsync"))
            .Returns(Task.CompletedTask);

        mockCache.Setup(x => x.RemoveAsync(It.IsAny<string>()))
            .Callback(() => callOrder.Add("RemoveAsync"))
            .Returns(Task.CompletedTask);

        mockRealtime.Setup(x => x.PublishAsync(It.IsAny<RealtimeTopic>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("PublishAsync"))
            .Returns(Task.CompletedTask);

        var transactionBehavior = new TransactionBehavior<SideEffectCommand, string>(
            mockContext.Object, Mock.Of<ILogger<TransactionBehavior<SideEffectCommand, string>>>());

        var realtimeBehavior = new RealtimeBehavior<SideEffectCommand, string>(
            mockRealtime.Object, Mock.Of<ILogger<RealtimeBehavior<SideEffectCommand, string>>>());

        var cacheInvalidationBehavior = new CacheInvalidationBehavior<SideEffectCommand, string>(
            mockCache.Object, Mock.Of<ILogger<CacheInvalidationBehavior<SideEffectCommand, string>>>());

        RequestHandlerDelegate<string> txNext = ct =>
        {
            callOrder.Add("Handler");
            return Task.FromResult("ok");
        };

        RequestHandlerDelegate<string> realtimeNext = ct =>
            transactionBehavior.Handle(new SideEffectCommand(), txNext, ct);

        RequestHandlerDelegate<string> cacheNext = ct =>
            realtimeBehavior.Handle(new SideEffectCommand(), realtimeNext, ct);

        await cacheInvalidationBehavior.Handle(new SideEffectCommand(), cacheNext, CancellationToken.None);

        callOrder.Should().ContainInOrder("Handler", "SaveChanges", "CommitAsync", "PublishAsync", "RemoveAsync");
    }

    [Fact]
    public async Task Pipeline_SideEffectsCannotRunBeforeCommit()
    {
        var (mockContext, mockTransaction) = CreateMockContextPair();
        var mockCache = CreateMockCacheService();
        var mockRealtime = CreateMockRealtimePublisher();
        var commitHappened = false;

        mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => commitHappened = true)
            .ReturnsAsync(1);

        mockTransaction.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .Callback(() => commitHappened = true)
            .Returns(Task.CompletedTask);

        mockCache.Setup(x => x.RemoveAsync(It.IsAny<string>()))
            .Callback(() => commitHappened.Should().BeTrue("commit must happen before cache invalidation"))
            .Returns(Task.CompletedTask);

        mockRealtime.Setup(x => x.PublishAsync(It.IsAny<RealtimeTopic>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback(() => commitHappened.Should().BeTrue("commit must happen before realtime publish"))
            .Returns(Task.CompletedTask);

        var transactionBehavior = new TransactionBehavior<SideEffectCommand, string>(
            mockContext.Object, Mock.Of<ILogger<TransactionBehavior<SideEffectCommand, string>>>());

        var realtimeBehavior = new RealtimeBehavior<SideEffectCommand, string>(
            mockRealtime.Object, Mock.Of<ILogger<RealtimeBehavior<SideEffectCommand, string>>>());

        var cacheInvalidationBehavior = new CacheInvalidationBehavior<SideEffectCommand, string>(
            mockCache.Object, Mock.Of<ILogger<CacheInvalidationBehavior<SideEffectCommand, string>>>());

        RequestHandlerDelegate<string> txNext = _ => Task.FromResult("ok");

        RequestHandlerDelegate<string> realtimeNext = ct =>
            transactionBehavior.Handle(new SideEffectCommand(), txNext, ct);

        RequestHandlerDelegate<string> cacheNext = ct =>
            realtimeBehavior.Handle(new SideEffectCommand(), realtimeNext, ct);

        await cacheInvalidationBehavior.Handle(new SideEffectCommand(), cacheNext, CancellationToken.None);
    }

    #endregion

    #region 4. Idempotency behavior

    [Fact]
    public async Task IdempotencyBehavior_LockAcquired_ExecutesHandlerAndStoresResult()
    {
        var mockStore = CreateMockIdempotencyStore(lockAcquired: true);
        var behavior = new IdempotencyBehavior<ExecutableCommand, string>(
            mockStore.Object, Mock.Of<ILogger<IdempotencyBehavior<ExecutableCommand, string>>>());

        RequestHandlerDelegate<string> next = _ => Task.FromResult("result");
        var result = await behavior.Handle(new ExecutableCommand(), next, CancellationToken.None);

        result.Should().Be("result");
        mockStore.Verify(x => x.SetResultAsync("test-key", "result"), Times.Once);
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
            mockStore.Object, Mock.Of<ILogger<IdempotencyBehavior<ExecutableCommand, string>>>());

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
            mockStore.Object, Mock.Of<ILogger<IdempotencyBehavior<ExecutableCommand, string>>>());

        RequestHandlerDelegate<string> next = _ => throw new InvalidOperationException("should not be called");

        Func<Task> act = () => behavior.Handle(new ExecutableCommand(), next, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task IdempotencyBehavior_HandlerThrows_ReleasesLockAndDoesNotStoreResult()
    {
        var mockStore = CreateMockIdempotencyStore(lockAcquired: true);
        var behavior = new IdempotencyBehavior<ExecutableCommand, string>(
            mockStore.Object, Mock.Of<ILogger<IdempotencyBehavior<ExecutableCommand, string>>>());

        RequestHandlerDelegate<string> next = _ => throw new InvalidOperationException("handler failed");

        Func<Task> act = () => behavior.Handle(new ExecutableCommand(), next, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        mockStore.Verify(x => x.ReleaseLockAsync("test-key"), Times.Once);
        mockStore.Verify(x => x.SetResultAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task IdempotencyBehavior_NonIdempotentRequest_SkipsIdempotency()
    {
        var mockStore = CreateMockIdempotencyStore();
        var behavior = new IdempotencyBehavior<NonTransactionalCommand, string>(
            mockStore.Object, Mock.Of<ILogger<IdempotencyBehavior<NonTransactionalCommand, string>>>());

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
        var mockPermissionService = new Mock<IPermissionService>();
        mockPermissionService.Setup(x => x.EvaluateAsync(It.IsAny<PermissionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PermissionDecision(false, "missing_permission"));

        var behavior = new AuthorizationBehavior<ExecutableCommand, string>(
            mockUser.Object, mockPermissionService.Object);
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
            mockUser.Object, Mock.Of<IPermissionService>());

        RequestHandlerDelegate<string> next = _ => Task.FromResult("ok");

        Func<Task> act = () => behavior.Handle(new ExecutableCommand(), next, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task WorkspaceContextBehavior_EmptyWorkspaceId_ThrowsForbidden()
    {
        var mockUser = CreateMockUser();
        var mockWorkspaceService = CreateMockWorkspacePermissionService();

        var behavior = new WorkspaceContextBehavior<EmptyWorkspaceCommand, string>(
            mockUser.Object, Mock.Of<ICurrentWorkspace>(), mockWorkspaceService.Object);

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
        var transactionBehavior = new TransactionBehavior<ValidationFailCommand, string>(
            mockContext.Object, Mock.Of<ILogger<TransactionBehavior<ValidationFailCommand, string>>>());

        RequestHandlerDelegate<string> txNext = _ => Task.FromResult("ok");

        RequestHandlerDelegate<string> wsNext = ct =>
            transactionBehavior.Handle(new ValidationFailCommand(), txNext, ct);

        Func<Task> act = () => validationBehavior.Handle(new ValidationFailCommand(), wsNext, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        mockContext.Verify(x => x.Database.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
