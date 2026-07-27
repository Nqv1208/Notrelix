namespace Notrelix.Platform.Messaging.Contracts;

public enum SchemaFormat
{
    JsonSchema,
    Avro,
    Protobuf,
}

public sealed record SchemaDefinition
{
    public required string EventName { get; init; }
    public int Version { get; init; }
    public SchemaFormat Format { get; init; } = SchemaFormat.JsonSchema;
    public required string Schema { get; init; }
    public string? Description { get; init; }
}
