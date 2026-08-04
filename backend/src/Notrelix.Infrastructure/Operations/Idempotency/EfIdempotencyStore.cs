using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Operations.Idempotency;

/// <summary>
/// Atomic PostgreSQL idempotency store using INSERT ... ON CONFLICT DO NOTHING.
/// Participates in the current request transaction — never starts its own or calls SaveChanges.
///
/// State machine (spec 3.8):
/// - normal Processing is uncommitted in the current request transaction;
/// - a committed active Processing row is corrupt/legacy state and is never mapped
///   to Completed — the typed <see cref="IdempotencyIncompleteStateException"/> is thrown;
/// - expired Processing/Completed rows are replaced atomically (FOR UPDATE, delete,
///   retry insert, verify one row inserted);
/// - the store owns all expiry calculations through TimeProvider + IdempotencyOptions.
/// </summary>
public sealed class EfIdempotencyStore : IIdempotencyStore
{
    private const string ProcessingState = "Processing";
    private const string CompletedState = "Completed";

    // Bounded retry budget for expired-row replacement races: after deleting an
    // expired row, another transaction may win the re-insert; re-read and retry.
    private const int MaxAcquisitionAttempts = 3;

    private readonly ApplicationDbContext _context;
    private readonly TimeProvider _timeProvider;
    private readonly IdempotencyOptions _options;

    public EfIdempotencyStore(
        ApplicationDbContext context,
        TimeProvider timeProvider,
        IOptions<IdempotencyOptions> options)
    {
        _context = context;
        _timeProvider = timeProvider;
        _options = options.Value;
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

        for (var attempt = 0; attempt < MaxAcquisitionAttempts; attempt++)
        {
            // Atomic insert — the PostgreSQL unique constraint (scope, operation, key_hash)
            // serializes concurrent first executions. The Processing row expires according
            // to IdempotencyOptions.ProcessingExpiry.
            var inserted = await InsertProcessingRecordAsync(
                connection, transaction, identity, now, _options.ProcessingExpiry, cancellationToken);

            if (inserted)
            {
                return new IdempotencyBeginResult(IdempotencyBeginStatus.Started, null, null);
            }

            // Conflict — read the existing row FOR UPDATE. It may have vanished if the
            // conflicting transaction rolled back while we were waiting on the conflict.
            var existing = await SelectForUpdateAsync(connection, transaction, identity, cancellationToken);

            if (existing is null)
            {
                continue;
            }

            if (existing.Value.ExpiresAt <= now)
            {
                // Expired Processing or Completed row — replace atomically.
                await DeleteRecordAsync(connection, transaction, identity, cancellationToken);
                continue;
            }

            if (existing.Value.State == CompletedState)
            {
                if (existing.Value.RequestHash != identity.RequestHash)
                {
                    return new IdempotencyBeginResult(IdempotencyBeginStatus.PayloadMismatch, null, null);
                }

                if (existing.Value.ResultJson is null || existing.Value.ResultContract is null)
                {
                    throw new IdempotencyIncompleteStateException(identity.Operation);
                }

                return new IdempotencyBeginResult(
                    IdempotencyBeginStatus.Completed,
                    existing.Value.ResultJson,
                    existing.Value.ResultContract);
            }

            // Active committed Processing row: corrupt/legacy state. Never replay it
            // as Completed — surface the typed incomplete-state failure instead.
            throw new IdempotencyIncompleteStateException(identity.Operation);
        }

        throw new InvalidOperationException(
            $"Idempotency acquisition for operation '{identity.Operation}' failed after " +
            $"{MaxAcquisitionAttempts} attempts due to concurrent replacement activity.");
    }

    public async Task CompleteAsync(
        IdempotencyIdentity identity,
        string serializedResult,
        string resultContract,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var expiresAt = now.Add(_options.ResultExpiry);
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
        TimeSpan processingExpiry,
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
        AddParameter(cmd, "expiresAt", now.Add(processingExpiry));

        var rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    private static async Task<(string State, string RequestHash, string? ResultJson, string? ResultContract, DateTimeOffset ExpiresAt)?>
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
            // The conflicting row vanished (its transaction rolled back).
            return null;
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
