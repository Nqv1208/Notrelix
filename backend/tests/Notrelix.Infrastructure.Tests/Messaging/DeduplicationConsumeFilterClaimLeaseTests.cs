using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Projections.Search;
using Notrelix.Infrastructure.Messaging;
using Notrelix.Infrastructure.Messaging.Options;

namespace Notrelix.Infrastructure.Tests.Messaging;

/// <summary>
/// PF-07 — crash-point proof for the command-owned consumer path. A delivery that
/// dies after the claim commit and before completion leaves a durable "Processing"
/// claim; once its lease expires another delivery of the same (message, consumer)
/// identity must take it over, a live claim must never be stolen, and a settled
/// claim must keep ignoring duplicates. The business effect is modelled by an
/// idempotency ledger keyed by message id, mirroring the real command pipeline's
/// atomic effect + IdempotencyBehavior record (see DeduplicationConsumeFilter docs).
/// Process death is simulated by never completing the first delivery's pipe, so no
/// catch/cleanup of the filter runs (exactly what a killed process does).
/// </summary>
public sealed class DeduplicationConsumeFilterClaimLeaseTests : IDisposable
{
    private const string CommandOwnedEndpoint = "notrelix-identity-registration-completed-workspace-provision-v1";
    private static readonly DateTimeOffset T0 = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Lease = TimeSpan.FromSeconds(300);

    private readonly FakeClock _clock = new(T0);
    private readonly FakeDedupStore _store;
    private readonly ApplicationDbContext _db;
    private readonly EffectLedger _ledger = new();

    public DeduplicationConsumeFilterClaimLeaseTests()
    {
        _store = new FakeDedupStore(_clock);
        _db = CreateContext();
    }

    [Fact]
    public async Task CrashAfterClaim_BeforeEffect_StaleClaimIsReclaimedAndEffectRunsOnce()
    {
        var evt = NewEvent();
        var crashed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // Delivery 1: claim committed, then the process dies before any effect.
        var firstDelivery = DeliverAsync(evt, async () => await crashed.Task);
        _store.Row(evt).Status.Should().Be("Processing");

        _clock.Advance(Lease + TimeSpan.FromSeconds(1));

        // Redelivery after lease expiry.
        await DeliverAsync(evt, () => _ledger.ApplyOnceAsync(evt.EventId));

        _ledger.EffectCount(evt.EventId).Should().Be(1);
        _store.Row(evt).Status.Should().Be("Succeeded");
        firstDelivery.IsCompleted.Should().BeFalse("the crashed delivery never completes");
    }

    [Fact]
    public async Task CrashAfterEffectCommit_BeforeCompletion_ReclaimDoesNotDuplicateEffect()
    {
        var evt = NewEvent();
        var crashed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // Delivery 1: the command committed its effect (+ message-keyed idempotency
        // record), then the process died before the Succeeded marker was written.
        var firstDelivery = DeliverAsync(evt, async () =>
        {
            await _ledger.ApplyOnceAsync(evt.EventId);
            await crashed.Task;
        });
        _ledger.EffectCount(evt.EventId).Should().Be(1);
        _store.Row(evt).Status.Should().Be("Processing");

        _clock.Advance(Lease + TimeSpan.FromSeconds(1));

        // Redelivery re-enters the command, which replays the stored result.
        await DeliverAsync(evt, () => _ledger.ApplyOnceAsync(evt.EventId));

        _ledger.EffectCount(evt.EventId).Should().Be(1, "exactly one business effect");
        _ledger.ReplayCount(evt.EventId).Should().Be(1);
        _store.Row(evt).Status.Should().Be("Succeeded");
        firstDelivery.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task LiveProcessingClaim_WithinLease_IsNotStolen_AndDeliveryIsDeferredNotAcked()
    {
        var evt = NewEvent();
        var inFlight = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstDelivery = DeliverAsync(evt, async () => await inFlight.Task);
        var claimedAt = _store.Row(evt).ClaimedAt;

        _clock.Advance(Lease - TimeSpan.FromSeconds(1));

        var act = () => DeliverAsync(evt, () => _ledger.ApplyOnceAsync(evt.EventId));

        await act.Should().ThrowAsync<MessageClaimInFlightException>();
        _ledger.EffectCount(evt.EventId).Should().Be(0, "a live claim must not run the handler");
        _store.Row(evt).ClaimedAt.Should().Be(claimedAt, "the lease is not renewed by a non-winner");
        _store.Row(evt).Status.Should().Be("Processing");

        // The live attempt then completes normally.
        inFlight.SetResult();
        await firstDelivery;
        _store.Row(evt).Status.Should().Be("Succeeded");
    }

    [Fact]
    public async Task ExactlyLeaseOld_IsNotYetStale_ButOlderThanLeaseIs()
    {
        var evt = NewEvent();
        var hang = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = DeliverAsync(evt, async () => await hang.Task);

        _clock.Advance(Lease);
        var atBoundary = () => DeliverAsync(evt, () => _ledger.ApplyOnceAsync(evt.EventId));
        await atBoundary.Should().ThrowAsync<MessageClaimInFlightException>();

        _clock.Advance(TimeSpan.FromMilliseconds(1));
        await DeliverAsync(evt, () => _ledger.ApplyOnceAsync(evt.EventId));
        _store.Row(evt).Status.Should().Be("Succeeded");
    }

    [Fact]
    public async Task OnlyOneRedeliveryWinsTheReclaim_TheOtherSeesRenewedLease()
    {
        var evt = NewEvent();
        var hang = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = DeliverAsync(evt, async () => await hang.Task);
        _clock.Advance(Lease + TimeSpan.FromSeconds(1));

        var winnerHold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var winner = DeliverAsync(evt, async () =>
        {
            await _ledger.ApplyOnceAsync(evt.EventId);
            await winnerHold.Task;
        });

        var loser = () => DeliverAsync(evt, () => _ledger.ApplyOnceAsync(evt.EventId));
        await loser.Should().ThrowAsync<MessageClaimInFlightException>();
        _ledger.EffectCount(evt.EventId).Should().Be(1);

        winnerHold.SetResult();
        await winner;
        _store.Row(evt).Status.Should().Be("Succeeded");
    }

    [Fact]
    public async Task DuplicateAfterCompletion_IsIgnored_EvenAfterLeaseExpiry()
    {
        var evt = NewEvent();
        await DeliverAsync(evt, () => _ledger.ApplyOnceAsync(evt.EventId));
        _store.Row(evt).Status.Should().Be("Succeeded");

        _clock.Advance(Lease * 10);
        var handlerRuns = 0;
        await DeliverAsync(evt, () =>
        {
            handlerRuns++;
            return Task.CompletedTask;
        });

        handlerRuns.Should().Be(0);
        _ledger.EffectCount(evt.EventId).Should().Be(1);
        _store.Row(evt).Status.Should().Be("Succeeded");
    }

    [Fact]
    public async Task SuccessMarkerIsNotAdvanced_WhenProtectedHandlerFails()
    {
        var evt = NewEvent();

        var act = () => DeliverAsync(evt, () => throw new InvalidOperationException("handler failed"));

        await act.Should().ThrowAsync<InvalidOperationException>();
        _store.Row(evt, optional: true)?.Status.Should().NotBe("Succeeded");
    }

    [Theory]
    [InlineData(300, true)]
    [InlineData(30, true)]
    [InlineData(86_400, true)]
    [InlineData(29, false)]
    [InlineData(0, false)]
    [InlineData(-5, false)]
    [InlineData(86_401, false)]
    public void LeaseOption_IsValidatedWithinBounds(int seconds, bool valid)
    {
        var result = new MessageDeduplicationOptionsValidator()
            .Validate(null, new MessageDeduplicationOptions { ProcessingClaimLeaseSeconds = seconds });

        result.Succeeded.Should().Be(valid);
    }

    [Fact]
    public void LeaseOption_DefaultIsFiveMinutes_AndValid()
    {
        var options = new MessageDeduplicationOptions();

        options.ProcessingClaimLeaseSeconds.Should().Be(300);
        new MessageDeduplicationOptionsValidator().Validate(null, options).Succeeded.Should().BeTrue();
    }

    // ------------------------------------------------------------------ helpers

    private static TestEvent NewEvent() => new() { EventId = Guid.NewGuid() };

    /// <summary>Starts one delivery through a fresh filter instance (as a new worker would).</summary>
    private Task DeliverAsync(TestEvent evt, Func<Task> consumer)
    {
        var filter = new DeduplicationConsumeFilter<TestEvent>(
            _store,
            _db,
            new NoopRls(),
            _clock,
            NullLogger<DeduplicationConsumeFilter<TestEvent>>.Instance,
            Microsoft.Extensions.Options.Options.Create(new MessageDeduplicationOptions { ProcessingClaimLeaseSeconds = (int)Lease.TotalSeconds }));

        var context = new Mock<ConsumeContext<TestEvent>>();
        context.Setup(x => x.Message).Returns(evt);
        context.Setup(x => x.CancellationToken).Returns(CancellationToken.None);
        var receive = new Mock<ReceiveContext>();
        receive.Setup(x => x.InputAddress).Returns(new Uri($"queue:{CommandOwnedEndpoint}"));
        context.Setup(x => x.ReceiveContext).Returns(receive.Object);

        var next = new Mock<IPipe<ConsumeContext<TestEvent>>>();
        next.Setup(x => x.Send(It.IsAny<ConsumeContext<TestEvent>>())).Returns(consumer);

        return filter.Send(context.Object, next.Object);
    }

    private static ApplicationDbContext CreateContext() =>
        new InMemoryApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    private sealed class InMemoryApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<SearchDocumentRecord>();
        }
    }

    public void Dispose() => _db.Dispose();

    private sealed class FakeClock(DateTimeOffset start) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; private set; } = start;
        public void Advance(TimeSpan by) => UtcNow += by;
    }

    private sealed class NoopRls : IRlsSessionContext
    {
        public Task ApplyAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ApplyUserScopeAsync(Guid userId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<TokenScopeBinding?> EnterTokenScopeAsync(TokenScopeRequest request, CancellationToken cancellationToken) =>
            Task.FromResult<TokenScopeBinding?>(null);
        public bool IsSessionContextEnabled => false;
        public RlsBoundedScope ActiveBoundedScope => RlsBoundedScope.None;
        public Task<bool> EnterInviteeScopeAsync(Guid userId, Notrelix.Application.Common.SystemOperations.SystemOperationReason reason, CancellationToken cancellationToken) =>
            Task.FromResult(false);
        public Task<bool> EnterRegistrationScopeAsync(Guid userId, Notrelix.Application.Common.SystemOperations.SystemOperationReason reason, CancellationToken cancellationToken) =>
            Task.FromResult(false);
    }

    /// <summary>Message-id keyed effect: first apply performs the effect, later applies replay.</summary>
    private sealed class EffectLedger
    {
        private readonly Dictionary<Guid, int> _effects = new();
        private readonly Dictionary<Guid, int> _replays = new();

        public Task ApplyOnceAsync(Guid messageId)
        {
            if (_effects.ContainsKey(messageId))
            {
                _replays[messageId] = ReplayCount(messageId) + 1;
            }
            else
            {
                _effects[messageId] = 1;
            }

            return Task.CompletedTask;
        }

        public int EffectCount(Guid id) => _effects.GetValueOrDefault(id);
        public int ReplayCount(Guid id) => _replays.GetValueOrDefault(id);
    }

    /// <summary>
    /// Dictionary-backed dedup store with the same contract as MessageDeduplicationStore:
    /// unique (message, consumer) claim insert and an atomic reclaim compare-and-swap.
    /// </summary>
    private sealed class FakeDedupStore(IDateTimeProvider clock) : IMessageDeduplicationStore
    {
        private readonly Dictionary<Guid, ClaimRow> _rows = new();
        private readonly object _gate = new();

        public ClaimRow Row(TestEvent evt) => Row(evt, optional: false)!;

        public ClaimRow? Row(TestEvent evt, bool optional)
        {
            lock (_gate)
            {
                if (_rows.TryGetValue(evt.EventId, out var row))
                {
                    return row;
                }
            }

            return optional ? null : throw new InvalidOperationException("claim row missing");
        }

        public Task<bool> IsProcessedAsync(Guid messageId, string consumerName, CancellationToken ct)
        {
            lock (_gate)
            {
                return Task.FromResult(_rows.TryGetValue(messageId, out var r) && r.Status == "Succeeded");
            }
        }

        public Task<bool> TryClaimProcessingAsync(
            Guid messageId, string consumerName, string messageName, int messageVersion,
            Guid? sourceEventId, Guid? workspaceId, CancellationToken ct)
        {
            lock (_gate)
            {
                if (_rows.ContainsKey(messageId))
                {
                    return Task.FromResult(false);
                }

                _rows[messageId] = new ClaimRow { Status = "Processing", ClaimedAt = clock.UtcNow };
                return Task.FromResult(true);
            }
        }

        public Task<bool> TryReclaimStaleProcessingAsync(
            Guid messageId, string consumerName, DateTimeOffset staleBefore,
            DateTimeOffset reclaimedAt, CancellationToken ct)
        {
            lock (_gate)
            {
                if (_rows.TryGetValue(messageId, out var r)
                    && r.Status == "Processing"
                    && r.ClaimedAt < staleBefore)
                {
                    r.ClaimedAt = reclaimedAt;
                    return Task.FromResult(true);
                }

                return Task.FromResult(false);
            }
        }

        public void MarkSucceeded(Guid messageId, string consumerName, DateTimeOffset processedAt)
        {
            lock (_gate)
            {
                if (_rows.TryGetValue(messageId, out var r))
                {
                    r.Status = "Succeeded";
                }
            }
        }
    }

    private sealed class ClaimRow
    {
        public string Status { get; set; } = "Processing";
        public DateTimeOffset ClaimedAt { get; set; }
    }

    public sealed class TestEvent : IIntegrationEvent
    {
        public Guid EventId { get; set; }
        public Guid? SourceEventId { get; set; }
        public string MessageName { get; set; } = "identity.registration-completed";
        public int SchemaVersion { get; set; } = 1;
        public Guid? AccountId { get; set; } = Guid.NewGuid();
        public Guid? WorkspaceId { get; set; }
        public Guid? ActorUserId { get; set; }
        public Guid CorrelationId { get; set; } = Guid.NewGuid();
        public Guid? CausationId { get; set; }
        public DateTimeOffset OccurredAt { get; set; } = T0;
    }
}
