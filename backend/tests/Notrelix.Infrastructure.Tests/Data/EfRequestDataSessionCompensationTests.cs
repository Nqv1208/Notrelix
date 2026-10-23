using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Common.Exceptions;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Projections.Search;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Infrastructure.Tests.Data;

/// <summary>
/// Rollback-compensation contract of the data session (infrastructure-and-data.md): effects
/// registered through <see cref="IRequestRollbackCompensations"/> (for example an uploaded
/// object) are compensated when the request fails BEFORE the commit starts, and are never
/// compensated once the commit has started, because its outcome is then unknown and deleting
/// an object behind a possibly committed row is the unsafe direction.
/// Runs on the In-Memory provider (no Docker); the commit failure is injected through a
/// replaced transaction manager.
/// </summary>
public sealed class EfRequestDataSessionCompensationTests
{
    private static readonly RequestDataSessionOptions Transactional =
        new(RequestDataAccess.Transactional, ApplyTenantScope: false, ApplyResourceScope: false, ExpectedVersion: null);

    private readonly RequestRollbackCompensations _compensations =
        new(NullLogger<RequestRollbackCompensations>.Instance);

    private int _compensated;

    private void RegisterCounting(string description = "delete object") =>
        _compensations.Register(description, _ =>
        {
            _compensated++;
            return Task.CompletedTask;
        });

    [Fact]
    public async Task Success_DoesNotCompensate()
    {
        await using var context = CreateContext();
        var session = CreateSession(context);

        var result = await session.ExecuteAsync(Transactional, _ =>
        {
            RegisterCounting();
            return Task.FromResult("ok");
        }, CancellationToken.None);

        result.Should().Be("ok");
        _compensated.Should().Be(0);
    }

    public static TheoryData<string> PreCommitFailures => new()
    {
        "stream-error", "size-cap", "domain-rejection", "cancellation",
    };

    [Theory]
    [MemberData(nameof(PreCommitFailures))]
    public async Task HandlerFailureBeforeCommit_RunsCompensationsAndRethrowsTheOriginalFailure(string failure)
    {
        await using var context = CreateContext();
        var session = CreateSession(context);
        using var cts = new CancellationTokenSource();
        var expected = failure switch
        {
            "stream-error" => (Exception)new IOException("connection reset"),
            "size-cap" => new PayloadTooLargeException(1024),
            "domain-rejection" => new DomainBusinessRuleException("Collaboration_Attachment_MaxAttachmentsExceeded", "limit"),
            _ => new OperationCanceledException(cts.Token),
        };

        var act = () => session.ExecuteAsync<string>(Transactional, _ =>
        {
            RegisterCounting();
            if (failure == "cancellation")
            {
                // The client abort is observed before the compensation runs; cleanup must still happen.
                cts.Cancel();
            }

            throw expected;
        }, cts.Token);

        (await act.Should().ThrowAsync<Exception>()).Which.Should().BeSameAs(expected);
        _compensated.Should().Be(1);
    }

    [Fact]
    public async Task SaveChangesFailure_RunsCompensations()
    {
        await using var context = CreateContext(failSaveChanges: true);
        var session = CreateSession(context);

        var act = () => session.ExecuteAsync(Transactional, _ =>
        {
            RegisterCounting();
            return Task.FromResult("ok");
        }, CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Message.Should().Contain("save failed");
        _compensated.Should().Be(1, "the database state is definitely rolled back when SaveChanges fails");
    }

    [Fact]
    public async Task CommitFailure_DoesNotRunCompensations()
    {
        await using var context = CreateContext(failCommit: true);
        var session = CreateSession(context);

        var act = () => session.ExecuteAsync(Transactional, _ =>
        {
            RegisterCounting();
            return Task.FromResult("ok");
        }, CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Message.Should().Contain("commit failed");
        _compensated.Should().Be(0, "a failed commit has an unknown outcome; the stored object must be kept");
    }

    [Fact]
    public async Task CompensationsRunInReverseRegistrationOrder_AndAFailingOneNeitherMasksTheOriginalFailureNorStopsTheRest()
    {
        await using var context = CreateContext();
        var session = CreateSession(context);
        var order = new List<string>();
        var original = new IOException("boom");

        var act = () => session.ExecuteAsync<string>(Transactional, _ =>
        {
            _compensations.Register("first", _ => { order.Add("first"); return Task.CompletedTask; });
            _compensations.Register("failing", _ => { order.Add("failing"); throw new InvalidOperationException("compensation failed"); });
            _compensations.Register("last", _ => { order.Add("last"); return Task.CompletedTask; });
            throw original;
        }, CancellationToken.None);

        (await act.Should().ThrowAsync<IOException>()).Which.Should().BeSameAs(original);
        order.Should().Equal("last", "failing", "first");
    }

    [Fact]
    public async Task AccessNone_RunsTheActionWithoutTransactionOrCompensation()
    {
        await using var context = CreateContext();
        var session = CreateSession(context);
        var options = new RequestDataSessionOptions(RequestDataAccess.None, false, false, null);

        var act = () => session.ExecuteAsync<string>(options, _ =>
        {
            RegisterCounting();
            throw new IOException("boom");
        }, CancellationToken.None);

        await act.Should().ThrowAsync<IOException>();
        _compensated.Should().Be(0, "no data session was opened, so nothing is rolled back by it");
    }

    private EfRequestDataSession CreateSession(ApplicationDbContext context) =>
        new(context, Mock.Of<IRlsSessionContext>(), Mock.Of<ILogger<EfRequestDataSession>>(),
            compensations: _compensations);

    private static ApplicationDbContext CreateContext(bool failSaveChanges = false, bool failCommit = false)
    {
        var builder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));

        if (failCommit)
        {
            builder.ReplaceService<IDbContextTransactionManager, FailingCommitTransactionManager>();
        }

        return new TestContext(builder.Options, failSaveChanges);
    }

    private sealed class TestContext(DbContextOptions<ApplicationDbContext> options, bool failSaveChanges)
        : ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<SearchDocumentRecord>();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            failSaveChanges
                ? throw new InvalidOperationException("save failed")
                : base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Transaction manager whose transactions fail on commit, standing in for a lost connection.</summary>
    private sealed class FailingCommitTransactionManager : IDbContextTransactionManager
    {
        private IDbContextTransaction? _current;

        public IDbContextTransaction BeginTransaction() => _current = new FailingTransaction();

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IDbContextTransaction>(_current = new FailingTransaction());

        public void CommitTransaction() => throw new InvalidOperationException("commit failed");

        public Task CommitTransactionAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("commit failed");

        public void RollbackTransaction() => _current = null;

        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            _current = null;
            return Task.CompletedTask;
        }

        public IDbContextTransaction? CurrentTransaction => _current;

        public void ResetState() => _current = null;

        public Task ResetStateAsync(CancellationToken cancellationToken = default)
        {
            _current = null;
            return Task.CompletedTask;
        }
    }

    private sealed class FailingTransaction : IDbContextTransaction
    {
        public Guid TransactionId { get; } = Guid.NewGuid();
        public void Commit() => throw new InvalidOperationException("commit failed");
        public Task CommitAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("commit failed");
        public void Rollback() { }
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
