using System.Text.Json;
using System.Text.Json.Serialization;

namespace Notrelix.Domain.SharedKernel;

/// <summary>
/// Serializes <see cref="ResourceKind"/> as its canonical string so persisted
/// JSON (jsonb columns, domain/integration event payloads) carries the kind
/// directly instead of an object wrapper.
/// </summary>
public sealed class ResourceKindJsonConverter : JsonConverter<ResourceKind>
{
    public override ResourceKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
            throw new JsonException("ResourceKind must be serialized as a non-empty canonical kind string.");

        return ResourceKind.Create(value);
    }

    public override void Write(Utf8JsonWriter writer, ResourceKind value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}
