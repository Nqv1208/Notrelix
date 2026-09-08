using System.Text.Json;

namespace Notrelix.Application.Common.Realtime;

[IntegrationEventTenantScope(IntegrationEventTenantScope.None)]
[EventName("realtime.resource-changed", Version = 1)]
public sealed record RealtimeResourceChangedV1 : IntegrationEvent
{
    public string TopicNamespace { get; }
    public string ResourceKind { get; }
    public Guid ResourceId { get; }
    public string StreamKey { get; }
    public long StreamVersion { get; }
    public string ChangeKind { get; }
    public string PayloadContract { get; }
    public JsonElement Payload { get; }

    public RealtimeResourceChangedV1(
        Guid eventId,
        Guid? accountId,
        Guid? workspaceId,
        Guid? actorUserId,
        Guid correlationId,
        Guid? causationId,
        DateTimeOffset occurredAt,
        string topicNamespace,
        string resourceKind,
        Guid resourceId,
        string streamKey,
        long streamVersion,
        string changeKind,
        string payloadContract,
        JsonElement payload)
        : base(
            eventId,
            "realtime.resource-changed",
            1,
            correlationId,
            accountId: accountId,
            workspaceId: workspaceId,
            actorUserId: actorUserId,
            causationId: causationId,
            occurredAt: occurredAt)
    {
        TopicNamespace = topicNamespace;
        ResourceKind = resourceKind;
        ResourceId = resourceId;
        StreamKey = streamKey;
        StreamVersion = streamVersion;
        ChangeKind = changeKind;
        PayloadContract = payloadContract;
        Payload = payload;
    }
}
