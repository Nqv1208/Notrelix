using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Notrelix.Application.Common.CQRS.Scoping;

namespace Notrelix.Application.Tests.Common.Behaviors;

public class DbRequestScopeBehaviorTests
{
    // --- Test request types ---

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

    // --- Helpers ---

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

    private static DbRequestScopeBehavior<T, string> CreateBehavior<T>(
        Mock<IApplicationDbContext>? context = null,
        Mock<IRlsSessionContext>? rls = null)
        where T : IRequest<string>
    {
        return new DbRequestScopeBehavior<T, string>(
            (context ?? CreateMockContextPair().Context).Object,
            (rls ?? CreateMockRls()).Object,
            Mock.Of<ILogger<DbRequestScopeBehavior<T, string>>>());
    }

    // --- Tests ---

    [Fact]
    public async Task GlobalTransactionalRequest_Does_Not_Apply_Rls()
    {
        var rls = CreateMockRls();
        var behavior = CreateBehavior<GlobalTransactionalRequest>(rls: rls);

        await behavior.Handle(new GlobalTransactionalRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

        rls.Verify(
            x => x.ApplyAsync(It.IsAny<DatabaseFacade>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GlobalTransactionalRequest_SavesChanges()
    {
        var (context, _) = CreateMockContextPair();
        var behavior = CreateBehavior<GlobalTransactionalRequest>(context: context);

        await behavior.Handle(new GlobalTransactionalRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

        context.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task WorkspaceTransactionalRequest_Applies_Rls()
    {
        var rls = CreateMockRls();
        var behavior = CreateBehavior<WorkspaceTransactionalRequest>(rls: rls);

        await behavior.Handle(new WorkspaceTransactionalRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

        rls.Verify(
            x => x.ApplyAsync(It.IsAny<DatabaseFacade>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task WorkspaceTransactionalRequest_SavesChanges()
    {
        var (context, _) = CreateMockContextPair();
        var behavior = CreateBehavior<WorkspaceTransactionalRequest>(context: context);

        await behavior.Handle(new WorkspaceTransactionalRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

        context.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RlsReadRequest_Applies_Rls_And_SavesChanges()
    {
        var rls = CreateMockRls();
        var (context, _) = CreateMockContextPair();
        var behavior = CreateBehavior<RlsReadRequest>(context: context, rls: rls);

        await behavior.Handle(new RlsReadRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

        rls.Verify(
            x => x.ApplyAsync(It.IsAny<DatabaseFacade>(), It.IsAny<CancellationToken>()),
            Times.Once);

        context.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
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
    public async Task NonTransactionalRequest_Does_Not_Open_DbScope()
    {
        var (context, _) = CreateMockContextPair();
        var behavior = CreateBehavior<NonTransactionalRequest>(context: context);

        var result = await behavior.Handle(
            new NonTransactionalRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
        context.Verify(
            x => x.Database,
            Times.Never);
    }
}
