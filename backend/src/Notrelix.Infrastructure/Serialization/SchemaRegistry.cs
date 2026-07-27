namespace Notrelix.Infrastructure.Serialization;

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

public interface ISchemaRegistry
{
    SchemaDefinition? GetSchema(string eventName, int version);
    bool Validate(string eventName, int version, string payload);
    IReadOnlyList<SchemaDefinition> GetAll();
}

public sealed class JsonSchemaRegistry : ISchemaRegistry
{
    private readonly Dictionary<string, SchemaDefinition> _schemas = new(StringComparer.OrdinalIgnoreCase);

    public JsonSchemaRegistry(IEnumerable<SchemaDefinition> schemas)
    {
        foreach (var schema in schemas)
        {
            _schemas[$"{schema.EventName}:v{schema.Version}"] = schema;
        }
    }

    public SchemaDefinition? GetSchema(string eventName, int version)
    {
        return _schemas.TryGetValue($"{eventName}:v{version}", out var schema) ? schema : null;
    }

    public bool Validate(string eventName, int version, string payload)
    {
        return _schemas.ContainsKey($"{eventName}:v{version}");
    }

    public IReadOnlyList<SchemaDefinition> GetAll() => _schemas.Values.ToList();
}
