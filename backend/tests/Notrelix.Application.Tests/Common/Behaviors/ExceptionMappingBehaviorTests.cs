using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Models;

namespace Notrelix.Application.Tests.Common.Behaviors;

public sealed class ExceptionMappingBehaviorTests
{
    private sealed record SampleRequest : IRequest<Result>;

    private readonly Mock<IExecutionContextReader> _executionContext = new();
    private readonly ILogger<ExceptionMappingBehavior<SampleRequest, Result>> _logger =
        NullLogger<ExceptionMappingBehavior<SampleRequest, Result>>.Instance;
    private readonly ExceptionMappingBehavior<SampleRequest, Result> _behavior;

    public ExceptionMappingBehaviorTests()
    {
        _executionContext.Setup(x => x.CorrelationId).Returns(Guid.NewGuid());
        _behavior = new ExceptionMappingBehavior<SampleRequest, Result>(_logger, _executionContext.Object);
    }

    [Fact]
    public async Task Handle_WhenUniqueViolation_RethrowsAsConflictException()
    {
        var dbException = new DbUpdateException(
            "An error occurred while saving the entity changes.",
            new InvalidOperationException("23505: duplicate key value violates unique constraint \"ix_users_normalized_email\""));

        var act = async () => await _behavior.Handle(
            new SampleRequest(),
            (_) => throw dbException,
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*unique identity already exists*");
    }

    [Fact]
    public async Task Handle_WhenUniqueViolationWithDuplicateKeyMessage_RethrowsAsConflictException()
    {
        var dbException = new DbUpdateException(
            "An error occurred while saving the entity changes.",
            new InvalidOperationException("duplicate key value violates unique constraint"));

        var act = async () => await _behavior.Handle(
            new SampleRequest(),
            (_) => throw dbException,
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_WhenNonUniqueDbUpdateException_PropagatesOriginalException()
    {
        var dbException = new DbUpdateException("Some other database failure.", new InvalidOperationException("connection refused"));

        var act = async () => await _behavior.Handle(
            new SampleRequest(),
            (_) => throw dbException,
            CancellationToken.None);

        await act.Should().ThrowAsync<DbUpdateException>().Where(e => e == dbException);
    }

    [Fact]
    public async Task Handle_WhenConcurrencyConflict_RethrowsAsConflictException()
    {
        var concurrencyException = new DbUpdateConcurrencyException("Store update conflict");

        var act = async () => await _behavior.Handle(
            new SampleRequest(),
            (_) => throw concurrencyException,
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*modified by another request*");
    }

    [Fact]
    public async Task Handle_WhenSuccess_ReturnsResult()
    {
        var result = await _behavior.Handle(
            new SampleRequest(),
            (_) => Task.FromResult(Result.Success()),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }
}
