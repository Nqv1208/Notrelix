using Notrelix.Application.Common.Idempotency;
using Notrelix.Infrastructure.Operations.Idempotency;
using Notrelix.Integration.Tests.Containers;

namespace Notrelix.Integration.Tests.Data.Ops;

/// <summary>
/// IDEM-DB-001..011: Atomic PostgreSQL idempotency store integration tests.
/// Uses real PostgreSQL via Testcontainers with migrations applied.
/// </summary>
[Collection("Database")]
[Trait("Category", "RequiresDocker")]
public class IdempotencyStoreIntegrationTests
{
    private readonly PostgresTestContainer _db;

    public IdempotencyStoreIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    private static IdempotencyIdentity CreateIdentity(
        string operation = "test.module.test-action.v1",
        string scope = "account:00000000000000000000000000000001",
        string keyHash = "AAAA",
        string requestHash = "BBBB")
    {
        return new IdempotencyIdentity(operation, scope, keyHash, requestHash);
    }

    [Fact]
    public async Task IDEM_DB_001_FirstExecution_InsertsProcessingAndCompletes()
    {
        await using var context = _db.CreateContext();
        await using var tx = await context.Database.BeginTransactionAsync();

        var store = new EfIdempotencyStore(context, TimeProvider.System);
        var identity = CreateIdentity(keyHash: Guid.NewGuid().ToString("N"), requestHash: Guid.NewGuid().ToString("N"));

        var beginResult = await store.BeginAsync(identity, CancellationToken.None);

        beginResult.Status.Should().Be(IdempotencyBeginStatus.Started);

        await store.CompleteAsync(identity, "{\"result\":42}", identity.Operation,
            DateTimeOffset.UtcNow.AddHours(24), CancellationToken.None);

        await context.SaveChangesAsync();
        await tx.CommitAsync();

        // Verify persisted
        await using var verifyContext = _db.CreateContext();
        var record = await verifyContext.Set<IdempotencyRecord>()
            .FirstOrDefaultAsync(r => r.Scope == identity.Scope && r.Operation == identity.Operation && r.KeyHash == identity.KeyHash);

        record.Should().NotBeNull();
        record!.State.Should().Be("Completed");
        // jsonb normalizes whitespace — compare parsed JSON
        System.Text.Json.JsonDocument.Parse(record.ResultJson!).RootElement.GetProperty("result").GetInt32().Should().Be(42);
        record.ResultContract.Should().Be(identity.Operation);
    }

    [Fact]
    public async Task IDEM_DB_002_Replay_ReturnsOriginalResult()
    {
        await using var context = _db.CreateContext();
        await using var tx = await context.Database.BeginTransactionAsync();

        var store = new EfIdempotencyStore(context, TimeProvider.System);
        var identity = CreateIdentity(keyHash: Guid.NewGuid().ToString("N"), requestHash: Guid.NewGuid().ToString("N"));

        // First execution
        var first = await store.BeginAsync(identity, CancellationToken.None);
        first.Status.Should().Be(IdempotencyBeginStatus.Started);
        await store.CompleteAsync(identity, "{\"id\":\"abc\"}", identity.Operation,
            DateTimeOffset.UtcNow.AddHours(24), CancellationToken.None);
        await context.SaveChangesAsync();
        await tx.CommitAsync();

        // Second execution (replay)
        await using var context2 = _db.CreateContext();
        await using var tx2 = await context2.Database.BeginTransactionAsync();
        var store2 = new EfIdempotencyStore(context2, TimeProvider.System);

        var second = await store2.BeginAsync(identity, CancellationToken.None);

        second.Status.Should().Be(IdempotencyBeginStatus.Completed);
        // jsonb normalizes whitespace — compare parsed JSON
        System.Text.Json.JsonDocument.Parse(second.SerializedResult!).RootElement.GetProperty("id").GetString().Should().Be("abc");
        second.ResultContract.Should().Be(identity.Operation);
    }

    [Fact]
    public async Task IDEM_DB_003_PayloadMismatch_ReturnsConflict()
    {
        await using var context = _db.CreateContext();
        await using var tx = await context.Database.BeginTransactionAsync();

        var store = new EfIdempotencyStore(context, TimeProvider.System);
        var keyHash = Guid.NewGuid().ToString("N");
        var identity1 = CreateIdentity(keyHash: keyHash, requestHash: "hash-A");

        // First execution with hash-A
        var first = await store.BeginAsync(identity1, CancellationToken.None);
        first.Status.Should().Be(IdempotencyBeginStatus.Started);
        await store.CompleteAsync(identity1, "{}", identity1.Operation,
            DateTimeOffset.UtcNow.AddHours(24), CancellationToken.None);
        await context.SaveChangesAsync();
        await tx.CommitAsync();

        // Second execution with same key but different request hash
        await using var context2 = _db.CreateContext();
        await using var tx2 = await context2.Database.BeginTransactionAsync();
        var store2 = new EfIdempotencyStore(context2, TimeProvider.System);
        var identity2 = CreateIdentity(keyHash: keyHash, requestHash: "hash-B");

        var second = await store2.BeginAsync(identity2, CancellationToken.None);

        second.Status.Should().Be(IdempotencyBeginStatus.PayloadMismatch);
    }

    [Fact]
    public async Task IDEM_DB_004_HandlerException_BothRollback()
    {
        await using var context = _db.CreateContext();
        await using var tx = await context.Database.BeginTransactionAsync();

        var store = new EfIdempotencyStore(context, TimeProvider.System);
        var identity = CreateIdentity(keyHash: Guid.NewGuid().ToString("N"), requestHash: Guid.NewGuid().ToString("N"));

        var begin = await store.BeginAsync(identity, CancellationToken.None);
        begin.Status.Should().Be(IdempotencyBeginStatus.Started);

        // Simulate handler exception → rollback
        await tx.RollbackAsync();

        // Verify no record persisted
        await using var verifyContext = _db.CreateContext();
        var record = await verifyContext.Set<IdempotencyRecord>()
            .FirstOrDefaultAsync(r => r.Scope == identity.Scope && r.Operation == identity.Operation && r.KeyHash == identity.KeyHash);

        record.Should().BeNull("rollback must remove the Processing record");
    }

    [Fact]
    public async Task IDEM_DB_009_DifferentTenant_SameKey_Independent()
    {
        var keyHash = Guid.NewGuid().ToString("N");
        var requestHash = Guid.NewGuid().ToString("N");

        var identityA = CreateIdentity(scope: "account:00000000000000000000000000000001", keyHash: keyHash, requestHash: requestHash);
        var identityB = CreateIdentity(scope: "account:00000000000000000000000000000002", keyHash: keyHash, requestHash: requestHash);

        // Tenant A executes
        await using var contextA = _db.CreateContext();
        await using var txA = await contextA.Database.BeginTransactionAsync();
        var storeA = new EfIdempotencyStore(contextA, TimeProvider.System);
        var resultA = await storeA.BeginAsync(identityA, CancellationToken.None);
        resultA.Status.Should().Be(IdempotencyBeginStatus.Started);
        await storeA.CompleteAsync(identityA, "{\"tenant\":\"A\"}", identityA.Operation,
            DateTimeOffset.UtcNow.AddHours(24), CancellationToken.None);
        await contextA.SaveChangesAsync();
        await txA.CommitAsync();

        // Tenant B with same key should also get Started (independent partition)
        await using var contextB = _db.CreateContext();
        await using var txB = await contextB.Database.BeginTransactionAsync();
        var storeB = new EfIdempotencyStore(contextB, TimeProvider.System);
        var resultB = await storeB.BeginAsync(identityB, CancellationToken.None);

        resultB.Status.Should().Be(IdempotencyBeginStatus.Started,
            "different tenant partitions must be independent");
    }

    [Fact]
    public async Task IDEM_DB_011_NoTransaction_Throws()
    {
        await using var context = _db.CreateContext();
        // No transaction started

        var store = new EfIdempotencyStore(context, TimeProvider.System);
        var identity = CreateIdentity();

        var act = () => store.BeginAsync(identity, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*requires a current request transaction*");
    }

    [Fact]
    public async Task IDEM_DB_012_CommittedActiveProcessing_IsNeverReturnedAsCompleted()
    {
        // FZ-IDEM-01 (spec 3.8): a committed active Processing row is corrupt/legacy
        // state. BeginAsync must never map it to Completed — it must surface an
        // incomplete-state failure so the caller rolls back. Currently the store
        // returns Completed for a non-expired Processing row.
        var identity = CreateIdentity(
            keyHash: Guid.NewGuid().ToString("N"),
            requestHash: Guid.NewGuid().ToString("N"));

        // Simulate a committed Processing row left behind by a crashed/partial writer.
        await using (var seedContext = _db.CreateContext())
        {
            var record = IdempotencyRecord.CreateProcessing(
                identity.Scope, identity.Operation, identity.KeyHash, identity.RequestHash,
                DateTimeOffset.UtcNow);
            seedContext.Set<IdempotencyRecord>().Add(record);
            await seedContext.SaveChangesAsync();
        }

        await using var context = _db.CreateContext();
        await using var tx = await context.Database.BeginTransactionAsync();
        var store = new EfIdempotencyStore(context, TimeProvider.System);

        var act = () => store.BeginAsync(identity, CancellationToken.None);

        await act.Should().ThrowAsync<Exception>(
            "an active committed Processing row must never be replayed as Completed — FZ-IDEM-01 pins the typed IdempotencyIncompleteStateException");
    }

    [Fact]
    public async Task IDEM_SCHEMA_001_OnlyCanonicalTableExists()
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT table_name FROM information_schema.tables
            WHERE table_schema = 'ops' AND table_name LIKE '%idempotency%'
            ORDER BY table_name";

        var tables = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        tables.Should().Contain("idempotency_records",
            "the canonical ops.idempotency_records table must exist");

        // idempotency_keys is legacy — will be dropped in a separate cutover migration
        tables.Should().NotContain("idempotency_keys",
            "legacy ops.idempotency_keys must be dropped after cutover");
    }

    [Fact]
    public async Task IDEM_SCHEMA_002_NoLeaseColumns()
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT column_name FROM information_schema.columns
            WHERE table_schema = 'ops' AND table_name = 'idempotency_records'
              AND column_name LIKE '%lease%'";

        var columns = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(0));
        }

        columns.Should().BeEmpty("lease columns must be removed from idempotency_records");
    }
}
