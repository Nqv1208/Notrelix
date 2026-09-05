using MassTransit;
using Microsoft.Extensions.Logging;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Messaging;
using Notrelix.Infrastructure.Observability.Metrics;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Messaging;

[Collection("Database")]
public class DeduplicationConsumeFilterFullIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;
    private static int _consumerExecutionCount;
    private static readonly object _lock = new();

    public DeduplicationConsumeFilterFullIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
        _consumerExecutionCount = 0;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private (ApplicationDbContext Context, MessageDeduplicationStore Store, RlsSessionContext Rls) CreateFixture()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        var context = _db.CreateContext(tenant);
        var store = new MessageDeduplicationStore(context, new DateTimeProvider(), new MetricsService());
        var rls = new RlsSessionContext(
            context,
            Microsoft.Extensions.Options.Options.Create(new RlsOptions { SetSessionContext = true }),
            tenant);
        return (context, store, rls);
    }

    [Fact]
    public async Task ConcurrentDuplicateDelivery_OnlyOneConsumerExecutes()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var consumerName = "test-consumer";
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        _consumerExecutionCount = 0;

        var integrationEvent = new TestIntegrationEvent
        {
            EventId = eventId,
            MessageName = "TestEvent",
            SchemaVersion = 1,
            AccountId = accountId,
            WorkspaceId = workspaceId,
            ActorUserId = Guid.NewGuid()
        };

        // TaskCompletionSource để giữ worker 1 sau khi claim
        var consumerEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseConsumer = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // Worker 1: claim → vào consumer → chờ release
        var task1 = DeliverMessageInNewScope(
            integrationEvent,
            consumerName,
            beforeComplete: async () =>
            {
                consumerEntered.SetResult();
                await releaseConsumer.Task;
            });

        // Chờ worker 1 vào consumer (đã claim thành công)
        await consumerEntered.Task;

        // Worker 2: chạy khi worker 1 chưa commit → phải fail claim
        var task2 = DeliverMessageInNewScope(integrationEvent, consumerName);

        // Release worker 1
        releaseConsumer.SetResult();

        await Task.WhenAll(task1, task2);

        // Assert: chỉ 1 consumer thực sự chạy
        _consumerExecutionCount.Should().Be(1);

        var (context, _, _) = CreateFixture();
        var inboxRowCount = await context.Set<MessagingProcessedEvent>()
            .CountAsync(e => e.EventId == eventId && e.ConsumerName == consumerName);
        inboxRowCount.Should().Be(1);

        var status = await context.Set<MessagingProcessedEvent>()
            .Where(e => e.EventId == eventId && e.ConsumerName == consumerName)
            .Select(e => e.Status)
            .FirstOrDefaultAsync();
        status.Should().Be("Succeeded");
    }

    [Fact]
    public async Task SequentialDuplicate_SecondMessageSkipped()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var consumerName = "test-consumer";
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        _consumerExecutionCount = 0;

        var integrationEvent = new TestIntegrationEvent
        {
            EventId = eventId,
            MessageName = "TestEvent",
            SchemaVersion = 1,
            AccountId = accountId,
            WorkspaceId = workspaceId,
            ActorUserId = Guid.NewGuid()
        };

        // Act: First delivery
        await DeliverMessageInNewScope(integrationEvent, consumerName);
        _consumerExecutionCount.Should().Be(1);

        // Second delivery (duplicate)
        await DeliverMessageInNewScope(integrationEvent, consumerName);

        // Assert
        _consumerExecutionCount.Should().Be(1, "second delivery should be skipped");

        var (context, _, _) = CreateFixture();
        var inboxRowCount = await context.Set<MessagingProcessedEvent>()
            .CountAsync(e => e.EventId == eventId && e.ConsumerName == consumerName);
        inboxRowCount.Should().Be(1);
    }

    [Fact]
    public async Task DifferentConsumersSameEvent_BothExecute()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        _consumerExecutionCount = 0;

        var integrationEvent = new TestIntegrationEvent
        {
            EventId = eventId,
            MessageName = "TestEvent",
            SchemaVersion = 1,
            AccountId = accountId,
            WorkspaceId = workspaceId,
            ActorUserId = Guid.NewGuid()
        };

        // Act: Deliver to 2 different consumers (KHÔNG có prefix "queue:")
        await DeliverMessageInNewScope(integrationEvent, "consumer-a");
        await DeliverMessageInNewScope(integrationEvent, "consumer-b");

        // Assert
        _consumerExecutionCount.Should().Be(2, "both consumers should execute");

        var (context, _, _) = CreateFixture();
        var inboxRows = await context.Set<MessagingProcessedEvent>()
            .Where(e => e.EventId == eventId)
            .ToListAsync();
        inboxRows.Count.Should().Be(2);
        inboxRows.Should().Contain(r => r.ConsumerName == "consumer-a");
        inboxRows.Should().Contain(r => r.ConsumerName == "consumer-b");
    }

    [Fact]
    public async Task ConsumerFailureAfterClaim_TransactionRollsBack()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var consumerName = "test-consumer";
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        _consumerExecutionCount = 0;

        var integrationEvent = new TestIntegrationEvent
        {
            EventId = eventId,
            MessageName = "TestEvent",
            SchemaVersion = 1,
            AccountId = accountId,
            WorkspaceId = workspaceId,
            ActorUserId = Guid.NewGuid()
        };

        // Act: First delivery - consumer throws
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            DeliverMessageInNewScope(integrationEvent, consumerName, shouldThrow: true));

        // Assert: No row should exist after rollback
        var (context1, _, _) = CreateFixture();
        var inboxRowCountAfterFail = await context1.Set<MessagingProcessedEvent>()
            .CountAsync(e => e.EventId == eventId && e.ConsumerName == consumerName);
        inboxRowCountAfterFail.Should().Be(0, "claim should be rolled back");

        // Act: Second delivery - consumer succeeds
        await DeliverMessageInNewScope(integrationEvent, consumerName, shouldThrow: false);

        // Assert: Row should exist and be Succeeded
        var (context2, _, _) = CreateFixture();
        var status = await context2.Set<MessagingProcessedEvent>()
            .Where(e => e.EventId == eventId && e.ConsumerName == consumerName)
            .Select(e => e.Status)
            .FirstOrDefaultAsync();
        status.Should().Be("Succeeded");
    }

    [Fact]
    public async Task CommandOwnedConsumerFailureAfterClaim_ClaimRemovedSoRetrySucceeds()
    {
        // Command-dispatching consumers (e.g. WorkspaceProvisioningConsumer) must
        // not be wrapped in the dedup transaction (their MediatR command opens its
        // own data-session transaction). On effect failure the "Processing" claim
        // must still be removed so unique-constraint dedup does not block retry.
        var eventId = Guid.NewGuid();
        var consumerName = "notrelix-identity-registration-completed-workspace-provision-v1";
        _consumerExecutionCount = 0;

        var integrationEvent = new TestIntegrationEvent
        {
            EventId = eventId,
            MessageName = "identity.registration-completed",
            SchemaVersion = 1,
            AccountId = Guid.NewGuid(),
            ActorUserId = Guid.NewGuid()
        };

        // First delivery - the command-owned consumer throws; claim must be removed.
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            DeliverMessageInNewScope(integrationEvent, consumerName, shouldThrow: true));

        var (context1, _, _) = CreateFixture();
        var rowsAfterFail = await context1.Set<MessagingProcessedEvent>()
            .CountAsync(e => e.EventId == eventId && e.ConsumerName == consumerName);
        rowsAfterFail.Should().Be(0, "claim must be removed so the message can be retried");

        // Second delivery - succeeds; exactly one Succeeded row.
        await DeliverMessageInNewScope(integrationEvent, consumerName, shouldThrow: false);

        var (context2, _, _) = CreateFixture();
        var rows = await context2.Set<MessagingProcessedEvent>()
            .Where(e => e.EventId == eventId && e.ConsumerName == consumerName)
            .ToListAsync();
        rows.Count.Should().Be(1);
        rows[0].Status.Should().Be("Succeeded");
    }

    [Fact]
    public async Task CommandOwnedConsumerFailure_DoesNotFlushRolledBackTrackedCommandChanges()
    {
        // A command-owned consumer's MediatR command opens its own data-session
        // transaction on the shared scoped ApplicationDbContext. When that
        // transaction rolls back, its Added aggregates remain tracked in the
        // ChangeTracker. The dedup failure-path claim cleanup must remove ONLY
        // the claim — it must never re-commit the rolled-back command writes by
        // flushing the still-tracked entities on its own SaveChanges.
        // Regression for the workspace-provisioning partial-commit defect found
        // by M4 phase-04 runtime proof.
        var eventId = Guid.NewGuid();
        var consumerName = "notrelix-identity-registration-completed-workspace-provision-v1";
        var accountId = Guid.NewGuid();
        _consumerExecutionCount = 0;

        var integrationEvent = new TestIntegrationEvent
        {
            EventId = eventId,
            MessageName = "identity.registration-completed",
            SchemaVersion = 1,
            AccountId = accountId,
            ActorUserId = Guid.NewGuid()
        };

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();

        var context = _db.CreateContext(tenant);
        var dateTimeProvider = new DateTimeProvider();
        var store = new MessageDeduplicationStore(context, dateTimeProvider, new MetricsService());
        var rls = new RlsSessionContext(
            context,
            Microsoft.Extensions.Options.Options.Create(new RlsOptions { SetSessionContext = true }),
            tenant);

        var logger = new Mock<ILogger<DeduplicationConsumeFilter<TestIntegrationEvent>>>();
        var filter = new DeduplicationConsumeFilter<TestIntegrationEvent>(
            store, context, rls, dateTimeProvider, logger.Object);

        var consumeContext = new Mock<ConsumeContext<TestIntegrationEvent>>();
        consumeContext.Setup(x => x.Message).Returns(integrationEvent);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        var receiveContext = new Mock<ReceiveContext>();
        receiveContext.Setup(x => x.InputAddress).Returns(new Uri($"queue:{consumerName}"));
        consumeContext.Setup(x => x.ReceiveContext).Returns(receiveContext.Object);

        // The consumer pipeline adds a personal Workspace (tracked as Added) to
        // the shared context and THEN fails — mirroring the provisioning shape
        // where the rolled-back DataSession left its aggregate in the tracker.
        Workspace? createdWorkspace = null;
        var next = new Mock<IPipe<ConsumeContext<TestIntegrationEvent>>>();
        next.Setup(x => x.Send(It.IsAny<ConsumeContext<TestIntegrationEvent>>()))
            .Callback(() =>
            {
                lock (_lock)
                {
                    _consumerExecutionCount++;
                }

                createdWorkspace = Workspace.Create(
                    accountId, Guid.NewGuid(), "Account's Workspace", "account-workspace",
                    DateTimeOffset.UtcNow, isPersonal: true);
                context.Workspaces.Add(createdWorkspace);
            })
            .ThrowsAsync(new InvalidOperationException("injected command pipeline failure"));

        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                filter.Send(consumeContext.Object, next.Object));
        }
        finally
        {
            tenant.Clear();
        }

        _consumerExecutionCount.Should().Be(1);

        (await context.Set<MessagingProcessedEvent>()
            .CountAsync(e => e.EventId == eventId && e.ConsumerName == consumerName))
            .Should().Be(0, "claim must be removed so the message can be retried");

        (await context.Workspaces.CountAsync(w => w.Id == createdWorkspace!.Id))
            .Should().Be(0, "failure-path claim cleanup must not flush rolled-back tracked command entities");
    }

    [Fact]
    public async Task RLSAppliedInsideTransaction_ConsumerQueryRespectsTenant()
    {
        // Arrange: Create 2 workspaces with different boards
        var (context, store, rls) = CreateFixture();
        var accountId = Guid.NewGuid();
        var workspaceA = Guid.NewGuid();
        var workspaceB = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Create boards using domain factory (signature đúng)
        var boardA = Board.Create(
            accountId,
            workspaceA,
            userId,
            "Board A",
            null,
            DateTimeOffset.UtcNow);

        var boardB = Board.Create(
            accountId,
            workspaceB,
            userId,
            "Board B",
            null,
            DateTimeOffset.UtcNow);

        context.Set<Board>().AddRange(boardA, boardB);
        await context.SaveChangesAsync();

        // Act: Deliver event for Workspace A
        var integrationEvent = new TestIntegrationEvent
        {
            EventId = Guid.NewGuid(),
            MessageName = "TestEvent",
            SchemaVersion = 1,
            AccountId = accountId,
            WorkspaceId = workspaceA,
            ActorUserId = userId
        };

        List<Guid> visibleBoardIds = new();
        await DeliverMessageInNewScopeWithQuery(integrationEvent, "test-consumer", async (ctx) =>
        {
            // Consumer queries boards - should only see Board A due to RLS
            visibleBoardIds = await ctx.Set<Board>()
                .Where(b => b.AccountId == accountId)
                .Select(b => b.Id)
                .ToListAsync();
        });

        // Assert: Only Board A should be visible
        visibleBoardIds.Should().Contain(boardA.Id);
        visibleBoardIds.Should().NotContain(boardB.Id, "RLS should prevent seeing Board B");
    }

    private async Task DeliverMessageInNewScope(
        TestIntegrationEvent integrationEvent,
        string consumerName,
        bool shouldThrow = false,
        Func<Task>? beforeComplete = null)
    {
        // Tạo scope/DbContext riêng cho mỗi delivery
        var tenant = new FakeCurrentTenantContext();
        if (integrationEvent.AccountId.HasValue && integrationEvent.WorkspaceId.HasValue)
        {
            tenant.SetWorkspace(
                integrationEvent.AccountId.Value,
                integrationEvent.WorkspaceId.Value,
                integrationEvent.ActorUserId);
        }
        else
        {
            tenant.SetSystem();
        }

        var context = _db.CreateContext(tenant);
        var dateTimeProvider = new DateTimeProvider();
        var store = new MessageDeduplicationStore(context, dateTimeProvider, new MetricsService());
        var rls = new RlsSessionContext(
            context,
            Microsoft.Extensions.Options.Options.Create(new RlsOptions { SetSessionContext = true }),
            tenant);

        var logger = new Mock<ILogger<DeduplicationConsumeFilter<TestIntegrationEvent>>>();
        var filter = new DeduplicationConsumeFilter<TestIntegrationEvent>(
            store, context, rls, dateTimeProvider, logger.Object);

        var consumeContext = new Mock<ConsumeContext<TestIntegrationEvent>>();
        consumeContext.Setup(x => x.Message).Returns(integrationEvent);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        var receiveContext = new Mock<ReceiveContext>();
        receiveContext.Setup(x => x.InputAddress).Returns(new Uri($"queue:{consumerName}"));
        consumeContext.Setup(x => x.ReceiveContext).Returns(receiveContext.Object);

        var next = new Mock<IPipe<ConsumeContext<TestIntegrationEvent>>>();
        next.Setup(x => x.Send(It.IsAny<ConsumeContext<TestIntegrationEvent>>()))
            .Returns(async () =>
            {
                lock (_lock)
                {
                    _consumerExecutionCount++;
                }

                if (beforeComplete is not null)
                {
                    await beforeComplete();
                }

                if (shouldThrow)
                {
                    throw new InvalidOperationException("Consumer failed");
                }
            });

        try
        {
            await filter.Send(consumeContext.Object, next.Object);
        }
        finally
        {
            tenant.Clear();
        }
    }

    private async Task DeliverMessageInNewScopeWithQuery(
        TestIntegrationEvent integrationEvent,
        string consumerName,
        Func<ApplicationDbContext, Task> consumerAction)
    {
        var tenant = new FakeCurrentTenantContext();
        if (integrationEvent.AccountId.HasValue && integrationEvent.WorkspaceId.HasValue)
        {
            tenant.SetWorkspace(
                integrationEvent.AccountId.Value,
                integrationEvent.WorkspaceId.Value,
                integrationEvent.ActorUserId);
        }
        else
        {
            tenant.SetSystem();
        }

        var context = _db.CreateContext(tenant);
        var dateTimeProvider = new DateTimeProvider();
        var store = new MessageDeduplicationStore(context, dateTimeProvider, new MetricsService());
        var rls = new RlsSessionContext(
            context,
            Microsoft.Extensions.Options.Options.Create(new RlsOptions { SetSessionContext = true }),
            tenant);

        var logger = new Mock<ILogger<DeduplicationConsumeFilter<TestIntegrationEvent>>>();
        var filter = new DeduplicationConsumeFilter<TestIntegrationEvent>(
            store, context, rls, dateTimeProvider, logger.Object);

        var consumeContext = new Mock<ConsumeContext<TestIntegrationEvent>>();
        consumeContext.Setup(x => x.Message).Returns(integrationEvent);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        var receiveContext = new Mock<ReceiveContext>();
        receiveContext.Setup(x => x.InputAddress).Returns(new Uri($"queue:{consumerName}"));
        consumeContext.Setup(x => x.ReceiveContext).Returns(receiveContext.Object);

        var next = new Mock<IPipe<ConsumeContext<TestIntegrationEvent>>>();
        next.Setup(x => x.Send(It.IsAny<ConsumeContext<TestIntegrationEvent>>()))
            .Returns(() => consumerAction(context));

        try
        {
            await filter.Send(consumeContext.Object, next.Object);
        }
        finally
        {
            tenant.Clear();
        }
    }

    public class TestIntegrationEvent : IIntegrationEvent
    {
        public Guid EventId { get; set; }
        public string MessageName { get; set; } = "TestEvent";
        public int SchemaVersion { get; set; } = 1;
        public Guid? SourceEventId { get; set; }
        public Guid? WorkspaceId { get; set; }
        public Guid CorrelationId { get; set; }
        public Guid? CausationId { get; set; }
        public Guid? ActorUserId { get; set; }
        public Guid? AccountId { get; set; }
        public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
