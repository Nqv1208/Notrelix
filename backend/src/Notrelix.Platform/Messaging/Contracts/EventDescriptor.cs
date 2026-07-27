using Notrelix.Application.Common.Events;

namespace Notrelix.Platform.Messaging.Contracts;

public sealed record EventDescriptor
{
    public required string Name { get; init; }
    public int Version { get; init; }
    public required Type EventType { get; init; }
    public SchemaDefinition? Schema { get; init; }
    public IEventSerializer? Serializer { get; init; }
    public EventClassification Classification { get; init; }
    public bool Deprecated { get; init; }
    public DateOnly? DeprecationDate { get; init; }
    public string? ReplacementEventName { get; init; }
}
