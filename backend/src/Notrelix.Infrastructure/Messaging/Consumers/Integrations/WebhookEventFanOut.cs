using System.Text.Json;
using Notrelix.Application.Events.Integrations;
using Notrelix.Domain.Integrations.Connections;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Messaging.Consumers.Integrations;

/// <summary>
/// Turns one Work or comment fact into one signed delivery per active outgoing webhook connection of the
/// Workspace. The delivery identity is derived from (source event id, connection id), so a redelivery of the
/// source fact asks for the same deliveries again and the receiver (and the dispatch consumer's own dedup)
/// sees a repeat, never a second delivery. A paused, revoked or deleted connection is skipped. The reads run
/// under the Workspace tenant the receive pipeline restored; the intents commit through the outbox.
/// </summary>
public sealed class WebhookEventFanOut
{
    private readonly ApplicationDbContext _db;
    private readonly IIntegrationEventCollector _events;
    private readonly IDateTimeProvider _clock;

    public WebhookEventFanOut(ApplicationDbContext db, IIntegrationEventCollector events, IDateTimeProvider clock)
    {
        _db = db;
        _events = events;
        _clock = clock;
    }

    public async Task FanOutAsync(
        Guid accountId,
        Guid workspaceId,
        Guid sourceEventId,
        string deliveryType,
        object data,
        Guid? actorUserId,
        Guid correlationId,
        DateTimeOffset occurredAt,
        CancellationToken ct)
    {
        var connectionIds = await _db.IntegrationConnections
            .AsNoTracking()
            .Where(c => c.WorkspaceId == workspaceId
                && c.Provider == IntegrationProvider.Webhook
                && c.Status == IntegrationConnectionStatus.Active
                && c.TargetUrl != null)
            .Select(c => c.Id)
            .ToListAsync(ct);
        if (connectionIds.Count == 0) return;

        var payload = JsonSerializer.Serialize(data);
        foreach (var connectionId in connectionIds)
        {
            _events.Add(new IntegrationWebhookDispatchRequestedV1(
                EventId: Derive(sourceEventId, connectionId, "event"),
                DeliveryId: Derive(sourceEventId, connectionId, deliveryType),
                ConnectionId: connectionId,
                DeliveryType: deliveryType,
                AccountIdValue: accountId,
                WorkspaceIdValue: workspaceId,
                OccurredAt: occurredAt == default ? _clock.UtcNow : occurredAt,
                CorrelationId: correlationId,
                ActorUserIdValue: actorUserId,
                SourceEventId: sourceEventId,
                CausationId: sourceEventId,
                PayloadJson: payload));
        }

        await _db.SaveChangesAsync(ct);
    }

    private static Guid Derive(Guid sourceEventId, Guid connectionId, string purpose)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"webhook-fanout:{purpose}:{sourceEventId:N}:{connectionId:N}"));
        return new Guid(bytes.AsSpan(0, 16));
    }
}
