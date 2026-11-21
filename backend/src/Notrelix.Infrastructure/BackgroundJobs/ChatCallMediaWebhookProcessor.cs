using Notrelix.Application.Common.SystemOperations;
using Notrelix.Application.Features.Chat.Calls;
using Notrelix.Application.Features.Chat.Calls.Commands.HandleCallMediaWebhook;
using Notrelix.Application.Features.Chat.Calls.Services;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.BackgroundJobs;

/// <summary>
/// Runs a verified media server event under the worker login: the call is found from the signed room name across Workspaces, so the
/// request cannot use the caller's own tenant, and the worker scope (RLS worker policies, no bypass) is what the call sweep already uses
/// for the same cross-tenant reads. Each event is its own scope.
/// </summary>
internal sealed class ChatCallMediaWebhookProcessor(IServiceScopeFactory scopes) : IChatCallMediaWebhookProcessor
{
    public async Task ProcessAsync(CallMediaWebhookEvent mediaEvent, string rawBody, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateWorkerAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ChatCallMediaWebhookRunner>().RunAsync(mediaEvent, rawBody, cancellationToken);
    }
}

/// <summary>
/// One event, one transaction. The event id is claimed first in the shared idempotency table (system scope): a replay finds it done and
/// changes nothing. Then a transaction advisory lock on the room serializes two events of one call, the call changes through the
/// aggregate, the claim is completed, and everything commits together. An exception rolls back the claim too, so the media server's retry
/// is applied once and only once.
/// </summary>
internal sealed class ChatCallMediaWebhookRunner(
    ApplicationDbContext context,
    ICurrentTenantContext tenant,
    IRlsSessionContext rls,
    IIdempotencyStore idempotency,
    ChatCallMediaEventApplier applier,
    IDateTimeProvider clock,
    ILogger<ChatCallMediaWebhookRunner> logger)
{
    public const string Operation = "chat.call.media-webhook.v1";

    private static readonly SystemOperationReason Reason =
        new("Chat", "Apply a signed media server event (a person left the room, the room finished) to the call that owns the room");

    /// <returns>True when the event was new and applied; false for a replay.</returns>
    public async Task<bool> RunAsync(CallMediaWebhookEvent mediaEvent, string rawBody, CancellationToken cancellationToken)
    {
        using var system = new SystemContextScope(tenant, logger, "chat-calls-webhook", Reason);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await rls.ApplyAsync(cancellationToken);

        var identity = new IdempotencyIdentity(Operation, "system", Hex(mediaEvent.EventId), Hex(rawBody));
        if ((await idempotency.BeginAsync(identity, cancellationToken)).Status != IdempotencyBeginStatus.Started)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var lockKey = System.Buffers.Binary.BinaryPrimitives.ReadInt64BigEndian(SHA256.HashData(Encoding.UTF8.GetBytes(mediaEvent.RoomKey))[..8]);
        await context.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({lockKey})", cancellationToken);

        await applier.ApplyAsync(mediaEvent, clock.UtcNow, cancellationToken);
        await idempotency.CompleteAsync(identity, "{}", Operation, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private static string Hex(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
