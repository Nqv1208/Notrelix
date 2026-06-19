namespace Notrelix.Infrastructure.Data.Outbox;

public sealed class ProcessedEvent
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public string ConsumerName { get; private set; } = null!;
    public string MessageName { get; private set; } = null!;
    public int SchemaVersion { get; private set; }
    public Guid? SourceEventId { get; private set; }
    public Guid? WorkspaceId { get; private set; }
    public DateTimeOffset ProcessedAt { get; private set; }

    private ProcessedEvent() { }

    public static ProcessedEvent Create(
        Guid eventId,
        string consumerName,
        string messageName,
        int schemaVersion,
        Guid? sourceEventId,
        Guid? workspaceId,
        DateTimeOffset processedAt)
    {
        return new ProcessedEvent
        {
            Id = Guid.CreateVersion7(),
            EventId = eventId,
            ConsumerName = consumerName,
            MessageName = messageName,
            SchemaVersion = schemaVersion,
            SourceEventId = sourceEventId,
            WorkspaceId = workspaceId,
            ProcessedAt = processedAt,
        };
    }
}
