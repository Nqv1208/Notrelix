using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Messaging;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Messaging;

[Collection("Database")]
public class DeduplicationConsumeFilterIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public DeduplicationConsumeFilterIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private (ApplicationDbContext Context, MessageDeduplicationStore Store) CreateFixture()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        var context = _db.CreateContext(tenant);
        var store = new MessageDeduplicationStore(context, new DateTimeProvider());
        return (context, store);
    }

    [Fact]
    public async Task TryClaimProcessingAsync_FirstClaim_ShouldSucceed()
    {
        var (context, store) = CreateFixture();
        var eventId = Guid.NewGuid();
        var consumerName = "test-consumer";

        var claimed = await store.TryClaimProcessingAsync(
            eventId, consumerName, "TestEvent", 1, null, null, default);

        claimed.Should().BeTrue();

        var record = context.Set<MessagingProcessedEvent>()
            .FirstOrDefault(e => e.EventId == eventId && e.ConsumerName == consumerName);
        record.Should().NotBeNull();
        record!.Status.Should().Be("Processing");
    }

    [Fact]
    public async Task TryClaimProcessingAsync_DuplicateClaim_ShouldFail()
    {
        var (context, store) = CreateFixture();
        var eventId = Guid.NewGuid();
        var consumerName = "test-consumer";

        var firstClaim = await store.TryClaimProcessingAsync(
            eventId, consumerName, "TestEvent", 1, null, null, default);
        firstClaim.Should().BeTrue();

        var secondClaim = await store.TryClaimProcessingAsync(
            eventId, consumerName, "TestEvent", 1, null, null, default);
        secondClaim.Should().BeFalse();
    }

    [Fact]
    public async Task TryClaimProcessingAsync_DifferentConsumers_ShouldBothSucceed()
    {
        var (context, store) = CreateFixture();
        var eventId = Guid.NewGuid();

        var claimA = await store.TryClaimProcessingAsync(
            eventId, "consumer-A", "TestEvent", 1, null, null, default);
        var claimB = await store.TryClaimProcessingAsync(
            eventId, "consumer-B", "TestEvent", 1, null, null, default);

        claimA.Should().BeTrue();
        claimB.Should().BeTrue();

        var records = context.Set<MessagingProcessedEvent>()
            .Where(e => e.EventId == eventId)
            .ToList();
        records.Count.Should().Be(2);
    }

    [Fact]
    public async Task MarkSucceeded_ShouldUpdateStatus()
    {
        var (context, store) = CreateFixture();
        var eventId = Guid.NewGuid();
        var consumerName = "test-consumer";

        await store.TryClaimProcessingAsync(
            eventId, consumerName, "TestEvent", 1, null, null, default);

        store.MarkSucceeded(eventId, consumerName, DateTimeOffset.UtcNow);
        await context.SaveChangesAsync();

        var record = context.Set<MessagingProcessedEvent>()
            .FirstOrDefault(e => e.EventId == eventId && e.ConsumerName == consumerName);
        record.Should().NotBeNull();
        record!.Status.Should().Be("Succeeded");
        record.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task IsProcessedAsync_OnlyReturnsTrueForSucceeded()
    {
        var (context, store) = CreateFixture();
        var eventId = Guid.NewGuid();
        var consumerName = "test-consumer";

        await store.TryClaimProcessingAsync(
            eventId, consumerName, "TestEvent", 1, null, null, default);

        var isProcessedWhileProcessing = await store.IsProcessedAsync(eventId, consumerName, default);
        isProcessedWhileProcessing.Should().BeFalse();

        store.MarkSucceeded(eventId, consumerName, DateTimeOffset.UtcNow);
        await context.SaveChangesAsync();

        var isProcessedAfterSucceeded = await store.IsProcessedAsync(eventId, consumerName, default);
        isProcessedAfterSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task ConcurrentClaims_OnlyOneSucceeds()
    {
        var eventId = Guid.NewGuid();
        var consumerName = "test-consumer";

        var (context1, store1) = CreateFixture();
        var (context2, store2) = CreateFixture();

        var task1 = store1.TryClaimProcessingAsync(
            eventId, consumerName, "TestEvent", 1, null, null, default);
        var task2 = store2.TryClaimProcessingAsync(
            eventId, consumerName, "TestEvent", 1, null, null, default);

        var results = await Task.WhenAll(task1, task2);

        var successCount = results.Count(r => r);
        successCount.Should().Be(1);

        var records = context1.Set<MessagingProcessedEvent>()
            .Where(e => e.EventId == eventId && e.ConsumerName == consumerName)
            .ToList();
        records.Count.Should().Be(1);
    }

    [Fact]
    public async Task ClaimThenRollback_ShouldAllowRetry()
    {
        var (context, store) = CreateFixture();
        var eventId = Guid.NewGuid();
        var consumerName = "test-consumer";

        var firstClaim = await store.TryClaimProcessingAsync(
            eventId, consumerName, "TestEvent", 1, null, null, default);
        firstClaim.Should().BeTrue();

        context.Set<MessagingProcessedEvent>().RemoveRange(
            context.Set<MessagingProcessedEvent>()
                .Where(e => e.EventId == eventId && e.ConsumerName == consumerName));
        await context.SaveChangesAsync();

        var retryClaim = await store.TryClaimProcessingAsync(
            eventId, consumerName, "TestEvent", 1, null, null, default);
        retryClaim.Should().BeTrue();

        var records = context.Set<MessagingProcessedEvent>()
            .Where(e => e.EventId == eventId && e.ConsumerName == consumerName)
            .ToList();
        records.Count.Should().Be(1);
    }
}
