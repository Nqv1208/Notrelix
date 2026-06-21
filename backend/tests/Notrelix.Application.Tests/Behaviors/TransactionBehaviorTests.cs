using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Behaviors;
using Notrelix.Application.Common.CQRS;

namespace Notrelix.Application.Tests.Behaviors;

public class TransactionBehaviorTests
{
    public sealed record TestCommand : ITransactionalRequest;
    public sealed record TestResponse(bool Success);

    [Fact]
    public async Task Handle_WhenTransactionalRequestSucceeds_CommitsTransaction()
    {
        var context = new Mock<IApplicationDbContext>();
        var database = new Mock<DatabaseFacade>(Mock.Of<DbContext>());
        var transaction = new Mock<IDbContextTransaction>();
        database.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);
        context.Setup(x => x.Database).Returns(database.Object);
        context.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var behavior = new TransactionBehavior<TestCommand, TestResponse>(
            context.Object, Mock.Of<ILogger<TransactionBehavior<TestCommand, TestResponse>>>());

        var response = await behavior.Handle(new TestCommand(), ct => Task.FromResult(new TestResponse(true)), default);

        response.Success.Should().BeTrue();
        context.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        transaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTransactionalRequestThrows_RollsBackTransaction()
    {
        var context = new Mock<IApplicationDbContext>();
        var database = new Mock<DatabaseFacade>(Mock.Of<DbContext>());
        var transaction = new Mock<IDbContextTransaction>();
        database.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);
        context.Setup(x => x.Database).Returns(database.Object);

        var behavior = new TransactionBehavior<TestCommand, TestResponse>(
            context.Object, Mock.Of<ILogger<TransactionBehavior<TestCommand, TestResponse>>>());

        Func<Task> act = () => behavior.Handle(
            new TestCommand(),
            ct => throw new InvalidOperationException("fail"),
            default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        transaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNonTransactionalRequest_SkipsTransaction()
    {
        var context = new Mock<IApplicationDbContext>();
        var behavior = new TransactionBehavior<NonTransactionalCommand, TestResponse>(
            context.Object, Mock.Of<ILogger<TransactionBehavior<NonTransactionalCommand, TestResponse>>>());

        var response = await behavior.Handle(new NonTransactionalCommand(), ct => Task.FromResult(new TestResponse(true)), default);

        response.Success.Should().BeTrue();
        context.Verify(x => x.Database, Times.Never);
    }

    public sealed record NonTransactionalCommand : IRequest<TestResponse>;
}
