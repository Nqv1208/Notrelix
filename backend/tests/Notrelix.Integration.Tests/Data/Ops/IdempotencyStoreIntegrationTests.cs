using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Npgsql;
using Notrelix.Application.Common.Idempotency;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Operations.Idempotency;
using Notrelix.Integration.Tests.Containers;

namespace Notrelix.Integration.Tests.Data.Ops;

/// <summary>
/// IDEM-DB-001..013: Atomic PostgreSQL idempotency store integration tests.
/// Uses real PostgreSQL via Testcontainers with migrations applied.
/// The store owns all expiry calculations through TimeProvider + IdempotencyOptions.
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

    private static EfIdempotencyStore CreateStore(
        ApplicationDbContext context,
        IdempotencyOptions? options = null,
        TimeProvider? timeProvider = null)
    {
        return new EfIdempotencyStore(
            context,
            timeProvider ?? TimeProvider.System,
            Options.Create(options ?? new IdempotencyOptions()));
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

        var store = CreateStore(context);
        var identity = CreateIdentity(keyHash: Guid.NewGuid().ToString("N"), requestHash: Guid.NewGuid().ToString("N"));

        var beginResult = await store.BeginAsync(identity, CancellationToken.None);

        beginResult.Status.Should().Be(IdempotencyBeginStatus.Started);

        await store.CompleteAsync(identity, "{\"result\":42}", identity.Operation, CancellationToken.None);

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

        var store = CreateStore(context);
        var identity = CreateIdentity(keyHash: Guid.NewGuid().ToString("N"), requestHash: Guid.NewGuid().ToString("N"));

        // First execution
        var first = await store.BeginAsync(identity, CancellationToken.None);
        first.Status.Should().Be(IdempotencyBeginStatus.Started);
        await store.CompleteAsync(identity, "{\"id\":\"abc\"}", identity.Operation, CancellationToken.None);
        await context.SaveChangesAsync();
        await tx.CommitAsync();

        // Second execution (replay)
        await using var context2 = _db.CreateContext();
        await using var tx2 = await context2.Database.BeginTransactionAsync();
        var store2 = CreateStore(context2);

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

        var store = CreateStore(context);
        var keyHash = Guid.NewGuid().ToString("N");
        var identity1 = CreateIdentity(keyHash: keyHash, requestHash: "hash-A");

        // First execution with hash-A
        var first = await store.BeginAsync(identity1, CancellationToken.None);
        first.Status.Should().Be(IdempotencyBeginStatus.Started);
        await store.CompleteAsync(identity1, "{}", identity1.Operation, CancellationToken.None);
        await context.SaveChangesAsync();
        await tx.CommitAsync();

        // Second execution with same key but different request hash
        await using var context2 = _db.CreateContext();
        await using var tx2 = await context2.Database.BeginTransactionAsync();
        var store2 = CreateStore(context2);
        var identity2 = CreateIdentity(keyHash: keyHash, requestHash: "hash-B");

        var second = await store2.BeginAsync(identity2, CancellationToken.None);

        second.Status.Should().Be(IdempotencyBeginStatus.PayloadMismatch);
    }

    [Fact]
    public async Task IDEM_DB_004_HandlerException_BothRollback()
    {
        await using var context = _db.CreateContext();
        await using var tx = await context.Database.BeginTransactionAsync();

        var store = CreateStore(context);
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
    public async Task IDEM_DB_005_SaveChangesFailure_RollsBackIdempotencyRecord()
    {
        // The store participates in the request transaction: when the business
        // SaveChanges fails, the idempotency record must roll back with it.
        await using var context = _db.CreateContext();
        await using var tx = await context.Database.BeginTransactionAsync();
        var store = CreateStore(context);
        var identity = CreateIdentity(
            keyHash: Guid.NewGuid().ToString("N"),
            requestHash: Guid.NewGuid().ToString("N"));

        var begin = await store.BeginAsync(identity, CancellationToken.None);
        begin.Status.Should().Be(IdempotencyBeginStatus.Started);
        await store.CompleteAsync(identity, "{\"ok\":true}", identity.Operation, CancellationToken.None);

        // Force a SaveChanges failure: an entity duplicating the idempotency
        // unique key (scope, operation, key_hash) inserted by the store above.
        var now = DateTimeOffset.UtcNow;
        var duplicate = IdempotencyRecord.CreateProcessing(
            identity.Scope, identity.Operation, identity.KeyHash, "other-request-hash",
            now, now.AddMinutes(5));
        context.Set<IdempotencyRecord>().Add(duplicate);

        var save = () => context.SaveChangesAsync();
        await save.Should().ThrowAsync<DbUpdateException>(
            "the duplicated unique key must make SaveChanges fail");

        await tx.RollbackAsync();

        await using var verifyContext = _db.CreateContext();
        var record = await verifyContext.Set<IdempotencyRecord>()
            .FirstOrDefaultAsync(r => r.Scope == identity.Scope && r.Operation == identity.Operation && r.KeyHash == identity.KeyHash);

        record.Should().BeNull("a SaveChanges failure must roll back the idempotency record atomically");
    }

    [Fact]
    public async Task IDEM_DB_006_CommitCancellation_DoesNotPersist()
    {
        // A cancelled/failed commit must leave no idempotency record behind.
        await using var context = _db.CreateContext();
        await using var tx = await context.Database.BeginTransactionAsync();
        var store = CreateStore(context);
        var identity = CreateIdentity(
            keyHash: Guid.NewGuid().ToString("N"),
            requestHash: Guid.NewGuid().ToString("N"));

        var begin = await store.BeginAsync(identity, CancellationToken.None);
        begin.Status.Should().Be(IdempotencyBeginStatus.Started);
        await store.CompleteAsync(identity, "{\"ok\":true}", identity.Operation, CancellationToken.None);

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var commit = () => tx.CommitAsync(cts.Token);
        await commit.Should().ThrowAsync<OperationCanceledException>();

        await tx.RollbackAsync();

        await using var verifyContext = _db.CreateContext();
        var record = await verifyContext.Set<IdempotencyRecord>()
            .FirstOrDefaultAsync(r => r.Scope == identity.Scope && r.Operation == identity.Operation && r.KeyHash == identity.KeyHash);

        record.Should().BeNull("a cancelled commit must not persist the idempotency record");
    }

    [Fact]
    public async Task IDEM_DB_007_ConcurrentFirstCommit_WaiterReplaysResult()
    {
        // Two transactions race for the same identity. The waiter's INSERT blocks on
        // the holder's uncommitted Processing row; after the holder completes and
        // commits, the waiter must replay the stored result — never double-execute.
        var identity = CreateIdentity(
            keyHash: Guid.NewGuid().ToString("N"),
            requestHash: Guid.NewGuid().ToString("N"));

        await using var holderContext = _db.CreateContext();
        await using var holderTx = await holderContext.Database.BeginTransactionAsync();
        var holderStore = CreateStore(holderContext);

        var holderBegin = await holderStore.BeginAsync(identity, CancellationToken.None);
        holderBegin.Status.Should().Be(IdempotencyBeginStatus.Started);

        await using var waiterContext = _db.CreateContext();
        await using var waiterTx = await waiterContext.Database.BeginTransactionAsync();
        var waiterStore = CreateStore(waiterContext);
        var waiterTask = Task.Run(() => waiterStore.BeginAsync(identity, CancellationToken.None));

        await WaitForBlockedIdempotencyWaiterAsync();

        await holderStore.CompleteAsync(identity, "{\"winner\":\"holder\"}", identity.Operation, CancellationToken.None);
        await holderContext.SaveChangesAsync();
        await holderTx.CommitAsync();

        var waiterBegin = await waiterTask.WaitAsync(TimeSpan.FromSeconds(30));

        waiterBegin.Status.Should().Be(IdempotencyBeginStatus.Completed,
            "after the holder commits, the concurrent waiter must receive the replay");
        System.Text.Json.JsonDocument.Parse(waiterBegin.SerializedResult!)
            .RootElement.GetProperty("winner").GetString().Should().Be("holder");

        await waiterTx.RollbackAsync();
    }

    [Fact]
    public async Task IDEM_DB_008_ConcurrentFirstRollback_WaiterStarts()
    {
        // If the holder's transaction rolls back, the blocked waiter's INSERT must
        // succeed — the waiter becomes the first execution instead of deadlocking.
        var identity = CreateIdentity(
            keyHash: Guid.NewGuid().ToString("N"),
            requestHash: Guid.NewGuid().ToString("N"));

        await using var holderContext = _db.CreateContext();
        await using var holderTx = await holderContext.Database.BeginTransactionAsync();
        var holderStore = CreateStore(holderContext);

        var holderBegin = await holderStore.BeginAsync(identity, CancellationToken.None);
        holderBegin.Status.Should().Be(IdempotencyBeginStatus.Started);

        await using var waiterContext = _db.CreateContext();
        await using var waiterTx = await waiterContext.Database.BeginTransactionAsync();
        var waiterStore = CreateStore(waiterContext);
        var waiterTask = Task.Run(() => waiterStore.BeginAsync(identity, CancellationToken.None));

        await WaitForBlockedIdempotencyWaiterAsync();

        await holderTx.RollbackAsync();

        var waiterBegin = await waiterTask.WaitAsync(TimeSpan.FromSeconds(30));

        waiterBegin.Status.Should().Be(IdempotencyBeginStatus.Started,
            "after the holder rolls back, the waiter must become the first execution");

        await waiterStore.CompleteAsync(identity, "{\"winner\":\"waiter\"}", identity.Operation, CancellationToken.None);
        await waiterContext.SaveChangesAsync();
        await waiterTx.CommitAsync();

        await using var verifyContext = _db.CreateContext();
        var record = await verifyContext.Set<IdempotencyRecord>()
            .SingleAsync(r => r.Scope == identity.Scope && r.Operation == identity.Operation && r.KeyHash == identity.KeyHash);
        record.State.Should().Be("Completed");
    }

    /// <summary>
    /// Bounded wait until a concurrent session blocks on a transactionid lock while
    /// executing an idempotency statement — proof the waiter reached its blocking
    /// INSERT before the holder resolves. Polls real lock state instead of sleeping.
    /// </summary>
    private async Task WaitForBlockedIdempotencyWaiterAsync()
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(15);
        while (DateTimeOffset.UtcNow < deadline)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT count(*)
                FROM pg_locks l
                JOIN pg_stat_activity a ON a.pid = l.pid
                WHERE l.locktype = 'transactionid'
                  AND NOT l.granted
                  AND a.query ILIKE '%idempotency_records%'
                """;

            var blocked = (long)(await cmd.ExecuteScalarAsync())!;
            if (blocked > 0)
            {
                return;
            }

            await Task.Delay(25);
        }

        throw new TimeoutException(
            "The concurrent waiter never blocked on the holder transaction — " +
            "the concurrency scenario did not reproduce.");
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
        var storeA = CreateStore(contextA);
        var resultA = await storeA.BeginAsync(identityA, CancellationToken.None);
        resultA.Status.Should().Be(IdempotencyBeginStatus.Started);
        await storeA.CompleteAsync(identityA, "{\"tenant\":\"A\"}", identityA.Operation, CancellationToken.None);
        await contextA.SaveChangesAsync();
        await txA.CommitAsync();

        // Tenant B with same key should also get Started (independent partition)
        await using var contextB = _db.CreateContext();
        await using var txB = await contextB.Database.BeginTransactionAsync();
        var storeB = CreateStore(contextB);
        var resultB = await storeB.BeginAsync(identityB, CancellationToken.None);

        resultB.Status.Should().Be(IdempotencyBeginStatus.Started,
            "different tenant partitions must be independent");
    }

    [Fact]
    public async Task IDEM_DB_011_NoTransaction_Throws()
    {
        await using var context = _db.CreateContext();
        // No transaction started

        var store = CreateStore(context);
        var identity = CreateIdentity();

        var act = () => store.BeginAsync(identity, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*requires a current request transaction*");
    }

    [Fact]
    public async Task IDEM_DB_012_CommittedActiveProcessing_IsNeverReturnedAsCompleted()
    {
        // FZ-IDEM-03 (spec 3.8): a committed active Processing row is corrupt/legacy
        // state. BeginAsync must never map it to Completed — it must surface the typed
        // IdempotencyIncompleteStateException so the caller rolls back and the API can
        // answer 503 + Retry-After.
        var identity = CreateIdentity(
            keyHash: Guid.NewGuid().ToString("N"),
            requestHash: Guid.NewGuid().ToString("N"));

        // Simulate a committed Processing row left behind by a crashed/partial writer.
        await using (var seedContext = _db.CreateContext())
        {
            var now = DateTimeOffset.UtcNow;
            var record = IdempotencyRecord.CreateProcessing(
                identity.Scope, identity.Operation, identity.KeyHash, identity.RequestHash,
                now, now.AddMinutes(5));
            seedContext.Set<IdempotencyRecord>().Add(record);
            await seedContext.SaveChangesAsync();
        }

        await using var context = _db.CreateContext();
        await using var tx = await context.Database.BeginTransactionAsync();
        var store = CreateStore(context);

        var act = () => store.BeginAsync(identity, CancellationToken.None);

        await act.Should().ThrowAsync<IdempotencyIncompleteStateException>(
            "an active committed Processing row must never be replayed as Completed");
    }

    [Fact]
    public async Task IDEM_DB_014_ExpiredProcessing_IsReplacedAndStarts()
    {
        // Spec 3.8: expired Processing rows may be replaced atomically.
        var identity = CreateIdentity(
            keyHash: Guid.NewGuid().ToString("N"),
            requestHash: Guid.NewGuid().ToString("N"));

        await using (var seedContext = _db.CreateContext())
        {
            var now = DateTimeOffset.UtcNow;
            var record = IdempotencyRecord.CreateProcessing(
                identity.Scope, identity.Operation, identity.KeyHash, identity.RequestHash,
                now.AddMinutes(-10), now.AddMinutes(-5));
            seedContext.Set<IdempotencyRecord>().Add(record);
            await seedContext.SaveChangesAsync();
        }

        await using var context = _db.CreateContext();
        await using var tx = await context.Database.BeginTransactionAsync();
        var store = CreateStore(context);

        var begin = await store.BeginAsync(identity, CancellationToken.None);

        begin.Status.Should().Be(IdempotencyBeginStatus.Started,
            "an expired Processing row must be replaced by a fresh execution");

        await store.CompleteAsync(identity, "{\"retry\":true}", identity.Operation, CancellationToken.None);
        await context.SaveChangesAsync();
        await tx.CommitAsync();

        await using var verifyContext = _db.CreateContext();
        var persisted = await verifyContext.Set<IdempotencyRecord>()
            .SingleAsync(r => r.Scope == identity.Scope && r.Operation == identity.Operation && r.KeyHash == identity.KeyHash);
        persisted.State.Should().Be("Completed");
    }

    [Fact]
    public async Task IDEM_DB_015_ExpiredCompleted_IsReplacedAndStarts()
    {
        // Spec 3.8: expired Completed rows may be replaced atomically.
        var identity = CreateIdentity(
            keyHash: Guid.NewGuid().ToString("N"),
            requestHash: Guid.NewGuid().ToString("N"));

        await using (var seedContext = _db.CreateContext())
        {
            var now = DateTimeOffset.UtcNow;
            var record = IdempotencyRecord.CreateProcessing(
                identity.Scope, identity.Operation, identity.KeyHash, identity.RequestHash,
                now.AddDays(-2), now.AddDays(-1));
            record.MarkCompleted("{\"old\":true}", identity.Operation, now.AddDays(-2), now.AddDays(-1));
            seedContext.Set<IdempotencyRecord>().Add(record);
            await seedContext.SaveChangesAsync();
        }

        await using var context = _db.CreateContext();
        await using var tx = await context.Database.BeginTransactionAsync();
        var store = CreateStore(context);

        var begin = await store.BeginAsync(identity, CancellationToken.None);

        begin.Status.Should().Be(IdempotencyBeginStatus.Started,
            "an expired Completed row must be replaced by a fresh execution");
    }

    [Fact]
    public async Task IDEM_DB_016_ProcessingExpiry_ComesFromOptions()
    {
        // Spec 3.6: the store owns Processing expiry through TimeProvider + options.
        var options = new IdempotencyOptions { ProcessingExpiry = TimeSpan.FromMinutes(7) };
        var identity = CreateIdentity(
            keyHash: Guid.NewGuid().ToString("N"),
            requestHash: Guid.NewGuid().ToString("N"));

        var before = DateTimeOffset.UtcNow;

        await using var context = _db.CreateContext();
        await using var tx = await context.Database.BeginTransactionAsync();
        var store = CreateStore(context, options);

        var begin = await store.BeginAsync(identity, CancellationToken.None);
        begin.Status.Should().Be(IdempotencyBeginStatus.Started);

        // The Processing row is uncommitted; read it inside the same transaction.
        await using var cmd = context.Database.GetDbConnection().CreateCommand();
        cmd.Transaction = context.Database.CurrentTransaction!.GetDbTransaction();
        cmd.CommandText = """
            SELECT expires_at FROM ops.idempotency_records
            WHERE scope = @scope AND operation = @operation AND key_hash = @keyHash
            """;
        AddParameter(cmd, "scope", identity.Scope);
        AddParameter(cmd, "operation", identity.Operation);
        AddParameter(cmd, "keyHash", identity.KeyHash);

        var expiresAtScalar = await cmd.ExecuteScalarAsync();
        var expiresAt = expiresAtScalar is DateTimeOffset dto
            ? dto
            : new DateTimeOffset(((DateTime)expiresAtScalar!).ToUniversalTime());
        var after = DateTimeOffset.UtcNow;

        expiresAt.Should().BeOnOrAfter(before.Add(options.ProcessingExpiry));
        expiresAt.Should().BeOnOrBefore(after.Add(options.ProcessingExpiry).AddSeconds(1));

        await tx.RollbackAsync();
    }

    [Fact]
    public async Task IDEM_SCHEMA_003_CheckConstraints_EnforceStateContract()
    {
        // Spec 3.8 + FZ-IDEM-03 + FZ-INF-IDEM-SCHEMA-01: the database enforces
        // the idempotency state/payload contract — only the Processing-empty and
        // Completed-populated row shapes are accepted.
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        // 1. Bogus state with null payload must be rejected by the state enum
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                INSERT INTO ops.idempotency_records
                    (id, scope, operation, key_hash, request_hash, state, created_at, expires_at)
                VALUES (@id, 'account:00000000000000000000000000000098', 'test.schema.bogus.v1', @keyHash, 'r', 'Bogus', now(), now() + interval '1 hour')
                """;
            AddParameter(cmd, "id", Guid.NewGuid());
            AddParameter(cmd, "keyHash", Guid.NewGuid().ToString("N"));

            var act = () => cmd.ExecuteNonQueryAsync();
            await act.Should().ThrowAsync<PostgresException>(
                    "state must be restricted to Processing/Completed by a CHECK constraint")
                .Where(e => e.SqlState == "23514", "a state violation must surface as a check-constraint violation");
        }

        // 2. Processing with all payload columns null is a valid row shape
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                INSERT INTO ops.idempotency_records
                    (id, scope, operation, key_hash, request_hash, state, created_at, expires_at)
                VALUES (@id, 'account:00000000000000000000000000000098', 'test.schema.processing-empty.v1', @keyHash, 'r', 'Processing', now(), now() + interval '1 hour')
                """;
            AddParameter(cmd, "id", Guid.NewGuid());
            AddParameter(cmd, "keyHash", Guid.NewGuid().ToString("N"));

            await cmd.ExecuteNonQueryAsync();
        }

        // 3. Processing carrying payload columns must be rejected
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                INSERT INTO ops.idempotency_records
                    (id, scope, operation, key_hash, request_hash, state, result_json, result_contract, completed_at, created_at, expires_at)
                VALUES (@id, 'account:00000000000000000000000000000098', 'test.schema.processing-populated.v1', @keyHash, 'r', 'Processing', '{}', 'op', now(), now(), now() + interval '1 hour')
                """;
            AddParameter(cmd, "id", Guid.NewGuid());
            AddParameter(cmd, "keyHash", Guid.NewGuid().ToString("N"));

            var act = () => cmd.ExecuteNonQueryAsync();
            await act.Should().ThrowAsync<PostgresException>(
                    "a Processing row with result_json/result_contract/completed_at must violate a CHECK constraint")
                .Where(e => e.SqlState == "23514", "a state violation must surface as a check-constraint violation");
        }

        // 4. Completed without payload columns must be rejected
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                INSERT INTO ops.idempotency_records
                    (id, scope, operation, key_hash, request_hash, state, created_at, expires_at)
                VALUES (@id, 'account:00000000000000000000000000000098', 'test.schema.completed-empty.v1', @keyHash, 'r', 'Completed', now(), now() + interval '1 hour')
                """;
            AddParameter(cmd, "id", Guid.NewGuid());
            AddParameter(cmd, "keyHash", Guid.NewGuid().ToString("N"));

            var act = () => cmd.ExecuteNonQueryAsync();
            await act.Should().ThrowAsync<PostgresException>(
                    "a Completed row without result_json/result_contract/completed_at must violate a CHECK constraint")
                .Where(e => e.SqlState == "23514", "a state violation must surface as a check-constraint violation");
        }

        // 5. Completed with all payload columns populated is a valid row shape
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                INSERT INTO ops.idempotency_records
                    (id, scope, operation, key_hash, request_hash, state, result_json, result_contract, completed_at, created_at, expires_at)
                VALUES (@id, 'account:00000000000000000000000000000098', 'test.schema.completed-populated.v1', @keyHash, 'r', 'Completed', '{}', 'op', now(), now(), now() + interval '1 hour')
                """;
            AddParameter(cmd, "id", Guid.NewGuid());
            AddParameter(cmd, "keyHash", Guid.NewGuid().ToString("N"));

            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static void AddParameter(System.Data.Common.DbCommand cmd, string name, object? value)
    {
        var param = cmd.CreateParameter();
        param.ParameterName = name;
        param.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(param);
    }

    [Fact]
    public async Task IDEM_DB_013_CompleteAsync_ExpiryOwnedByStoreOptions()
    {
        // Spec 3.6: the store owns expiry calculation through TimeProvider + options.
        // CompleteAsync takes no expiry argument.
        var options = new IdempotencyOptions { ResultExpiry = TimeSpan.FromHours(2) };
        var identity = CreateIdentity(
            keyHash: Guid.NewGuid().ToString("N"),
            requestHash: Guid.NewGuid().ToString("N"));

        var before = DateTimeOffset.UtcNow;

        await using var context = _db.CreateContext();
        await using var tx = await context.Database.BeginTransactionAsync();
        var store = CreateStore(context, options);

        var begin = await store.BeginAsync(identity, CancellationToken.None);
        begin.Status.Should().Be(IdempotencyBeginStatus.Started);

        await store.CompleteAsync(identity, "{\"ok\":true}", identity.Operation, CancellationToken.None);
        await context.SaveChangesAsync();
        await tx.CommitAsync();

        var after = DateTimeOffset.UtcNow;

        await using var verifyContext = _db.CreateContext();
        var record = await verifyContext.Set<IdempotencyRecord>()
            .FirstAsync(r => r.Scope == identity.Scope && r.Operation == identity.Operation && r.KeyHash == identity.KeyHash);

        record.ExpiresAt.Should().BeOnOrAfter(before.Add(options.ResultExpiry));
        record.ExpiresAt.Should().BeOnOrBefore(after.Add(options.ResultExpiry).AddSeconds(1));
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
