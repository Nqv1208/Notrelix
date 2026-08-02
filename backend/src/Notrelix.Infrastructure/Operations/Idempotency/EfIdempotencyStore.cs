using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Notrelix.Application.Common.Idempotency;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Operations.Idempotency;

/// <summary>
/// Atomic PostgreSQL idempotency store using INSERT ... ON CONFLICT DO NOTHING.
/// Participates in the current request transaction — never starts its own or calls SaveChanges.
/// </summary>
public sealed class EfIdempotencyStore : IIdempotencyStore
{
    private readonly ApplicationDbContext _context;
    private readonly TimeProvider _timeProvider;

    public EfIdempotencyStore(ApplicationDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task<IdempotencyBeginResult> BeginAsync(
        IdempotencyIdentity identity,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var connection = _context.Database.GetDbConnection();
        var transaction = _context.Database.CurrentTransaction?.GetDbTransaction()
            ?? throw new InvalidOperationException(
                "Idempotency store requires a current request transaction. " +
                "Ensure the request is classified as transactional.");

        // Atomic insert — PostgreSQL unique constraint (scope, operation, key_hash) serializes concurrency
        var inserted = await InsertProcessingRecordAsync(
            connection, transaction, identity, now, cancellationToken);

        if (inserted)
        {
            return new IdempotencyBeginResult(IdempotencyBeginStatus.Started, null, null);
        }

        // Conflict — read the existing row FOR UPDATE
        var existing = await SelectForUpdateAsync(connection, transaction, identity, cancellationToken);

        if (existing.State == "Completed")
        {
            if (existing.ExpiresAt <= now)
            {
                // Expired — delete and retry insert
                await DeleteRecordAsync(connection, transaction, identity, cancellationToken);
                await InsertProcessingRecordAsync(connection, transaction, identity, now, cancellationToken);
                return new IdempotencyBeginResult(IdempotencyBeginStatus.Started, null, null);
            }

            if (existing.RequestHash != identity.RequestHash)
            {
                return new IdempotencyBeginResult(IdempotencyBeginStatus.PayloadMismatch, null, null);
            }

            return new IdempotencyBeginResult(IdempotencyBeginStatus.Completed, existing.ResultJson, existing.ResultContract);
        }

        // State == "Processing" from another uncommitted transaction should not be visible
        // after ON CONFLICT resolution in PostgreSQL. If we reach here, treat as conflict.
        // This can happen if the row was inserted and committed as Processing but never completed
        // (crash scenario). Treat expired Processing as reclaimable.
        if (existing.ExpiresAt <= now)
        {
            await DeleteRecordAsync(connection, transaction, identity, cancellationToken);
            await InsertProcessingRecordAsync(connection, transaction, identity, now, cancellationToken);
            return new IdempotencyBeginResult(IdempotencyBeginStatus.Started, null, null);
        }

        // Active Processing from a crashed transaction — payload mismatch is safest response
        if (existing.RequestHash != identity.RequestHash)
        {
            return new IdempotencyBeginResult(IdempotencyBeginStatus.PayloadMismatch, null, null);
        }

        // Same payload, still processing — replay the completed result if available
        return new IdempotencyBeginResult(IdempotencyBeginStatus.Completed, existing.ResultJson, existing.ResultContract);
    }

    public async Task CompleteAsync(
        IdempotencyIdentity identity,
        string serializedResult,
        string resultContract,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var connection = _context.Database.GetDbConnection();
        var transaction = _context.Database.CurrentTransaction?.GetDbTransaction()
            ?? throw new InvalidOperationException(
                "Idempotency store requires a current request transaction for completion.");

        var rowsAffected = await UpdateToCompletedAsync(
            connection, transaction, identity, serializedResult, resultContract, now, expiresAt, cancellationToken);

        if (rowsAffected == 0)
        {
            throw new InvalidOperationException(
                $"Idempotency completion failed: no Processing record found for operation '{identity.Operation}' " +
                "with matching scope, key hash, and request hash.");
        }
    }

    private static async Task<bool> InsertProcessingRecordAsync(
        System.Data.Common.DbConnection connection,
        System.Data.Common.DbTransaction transaction,
        IdempotencyIdentity identity,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = """
            INSERT INTO ops.idempotency_records (id, scope, operation, key_hash, request_hash, state, created_at, expires_at)
            VALUES (@id, @scope, @operation, @keyHash, @requestHash, 'Processing', @createdAt, @expiresAt)
            ON CONFLICT (scope, operation, key_hash) DO NOTHING
            """;

        AddParameter(cmd, "id", Guid.NewGuid());
        AddParameter(cmd, "scope", identity.Scope);
        AddParameter(cmd, "operation", identity.Operation);
        AddParameter(cmd, "keyHash", identity.KeyHash);
        AddParameter(cmd, "requestHash", identity.RequestHash);
        AddParameter(cmd, "createdAt", now);
        AddParameter(cmd, "expiresAt", now.AddDays(1));

        var rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    private static async Task<(string State, string RequestHash, string? ResultJson, string? ResultContract, DateTimeOffset ExpiresAt)>
        SelectForUpdateAsync(
            System.Data.Common.DbConnection connection,
            System.Data.Common.DbTransaction transaction,
            IdempotencyIdentity identity,
            CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = """
            SELECT state, request_hash, result_json, result_contract, expires_at
            FROM ops.idempotency_records
            WHERE scope = @scope AND operation = @operation AND key_hash = @keyHash
            FOR UPDATE
            """;

        AddParameter(cmd, "scope", identity.Scope);
        AddParameter(cmd, "operation", identity.Operation);
        AddParameter(cmd, "keyHash", identity.KeyHash);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException(
                $"Idempotency record disappeared during conflict resolution for operation '{identity.Operation}'.");
        }

        return (
            reader.GetString(0),
            reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.GetFieldValue<DateTimeOffset>(4));
    }

    private static async Task DeleteRecordAsync(
        System.Data.Common.DbConnection connection,
        System.Data.Common.DbTransaction transaction,
        IdempotencyIdentity identity,
        CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = """
            DELETE FROM ops.idempotency_records
            WHERE scope = @scope AND operation = @operation AND key_hash = @keyHash
            """;

        AddParameter(cmd, "scope", identity.Scope);
        AddParameter(cmd, "operation", identity.Operation);
        AddParameter(cmd, "keyHash", identity.KeyHash);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<int> UpdateToCompletedAsync(
        System.Data.Common.DbConnection connection,
        System.Data.Common.DbTransaction transaction,
        IdempotencyIdentity identity,
        string serializedResult,
        string resultContract,
        DateTimeOffset now,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = """
            UPDATE ops.idempotency_records
            SET state = 'Completed', result_json = @resultJson::jsonb, result_contract = @resultContract,
                completed_at = @completedAt, expires_at = @expiresAt
            WHERE scope = @scope AND operation = @operation AND key_hash = @keyHash
              AND request_hash = @requestHash AND state = 'Processing'
            """;

        AddParameter(cmd, "resultJson", serializedResult);
        AddParameter(cmd, "resultContract", resultContract);
        AddParameter(cmd, "completedAt", now);
        AddParameter(cmd, "expiresAt", expiresAt);
        AddParameter(cmd, "scope", identity.Scope);
        AddParameter(cmd, "operation", identity.Operation);
        AddParameter(cmd, "keyHash", identity.KeyHash);
        AddParameter(cmd, "requestHash", identity.RequestHash);

        return await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameter(System.Data.Common.DbCommand cmd, string name, object? value)
    {
        var param = cmd.CreateParameter();
        param.ParameterName = name;
        param.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(param);
    }
}
