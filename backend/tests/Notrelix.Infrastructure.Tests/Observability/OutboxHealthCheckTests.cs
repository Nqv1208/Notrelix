using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Notrelix.Application.Common.Context;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Data.Projections.Search;
using Notrelix.Infrastructure.Observability.HealthChecks;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Infrastructure.Tests.Observability;

public sealed class OutboxHealthCheckTests : IDisposable
{
    private readonly string _databaseName = Guid.NewGuid().ToString();
    private readonly ApplicationDbContext _context;
    private readonly OutboxHealthCheck _check;

    public OutboxHealthCheckTests()
    {
        _context = CreateContext(_databaseName);
        _check = new OutboxHealthCheck(
            CreateContext(_databaseName),
            Microsoft.Extensions.Options.Options.Create(new OutboxHealthCheckOptions()));
    }

    [Fact]
    public async Task EmptyOutbox_IsHealthy()
    {
        var result = await CheckAsync();

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task RecentPendingRow_IsHealthy()
    {
        Add(createdAt: DateTimeOffset.UtcNow.AddMinutes(-1));

        var result = await CheckAsync();

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task PendingRowOlderThanDegradedThreshold_IsDegraded()
    {
        Add(createdAt: DateTimeOffset.UtcNow.AddMinutes(-6));

        var result = await CheckAsync();

        result.Status.Should().Be(HealthStatus.Degraded);
    }

    [Fact]
    public async Task PendingRow20MinutesOld_IsUnhealthy()
    {
        Add(createdAt: DateTimeOffset.UtcNow.AddMinutes(-20));

        var result = await CheckAsync();

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task AnyDeadLetter_IsUnhealthy_EvenWithFreshPending()
    {
        var deadLetter = Add(createdAt: DateTimeOffset.UtcNow.AddMinutes(-1));
        deadLetter.MarkDeadLetter("ERR", "exhausted", DateTimeOffset.UtcNow);
        _context.SaveChanges();
        Add(createdAt: DateTimeOffset.UtcNow.AddMinutes(-1));

        var result = await CheckAsync();

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("Dead-letter count: 1");
    }

    [Fact]
    public async Task RetryableFailedRow_IsNotDeadLetter_AndStaysHealthy()
    {
        var failed = Add(createdAt: DateTimeOffset.UtcNow.AddMinutes(-1));
        failed.MarkFailed("ERR", "retry later", DateTimeOffset.UtcNow);
        _context.SaveChanges();

        var result = await CheckAsync();

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("Failed count: 1");
    }

    private Task<HealthCheckResult> CheckAsync() =>
        _check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

    private MessagingOutboxMessage Add(DateTimeOffset createdAt)
    {
        var message = new MessagingOutboxMessage(
            eventId: Guid.NewGuid(),
            sourceEventId: null,
            sourceContext: "Test",
            messageName: "Test.Message",
            schemaVersion: 1,
            destination: null,
            subjectType: null,
            subjectId: null,
            aggregateType: null,
            aggregateId: null,
            workspaceId: null,
            accountId: null,
            actorUserId: null,
            correlationId: null,
            causationId: null,
            partitionKey: null,
            payloadJson: System.Text.Json.JsonDocument.Parse("{}"),
            headersJson: null,
            metadataJson: null,
            createdAt: createdAt);
        _context.Set<MessagingOutboxMessage>().Add(message);
        _context.SaveChanges();
        return message;
    }

    private static ApplicationDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return new OutboxTestContext(options, tenant);
    }

    // The in-memory provider cannot map the Npgsql tsvector column; the outbox queries do not use it.
    private sealed class OutboxTestContext(DbContextOptions<ApplicationDbContext> options, ICurrentTenantContext tenant)
        : ApplicationDbContext(options, tenant)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<SearchDocumentRecord>();
        }
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
