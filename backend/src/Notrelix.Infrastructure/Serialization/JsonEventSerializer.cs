using System.Text.Json;

namespace Notrelix.Infrastructure.Serialization;

public sealed class JsonEventSerializer : IEventSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public ReadOnlyMemory<byte> Serialize<T>(T @event) where T : class
    {
        return JsonSerializer.SerializeToUtf8Bytes(@event, Options);
    }

    public T? Deserialize<T>(ReadOnlyMemory<byte> data) where T : class
    {
        return JsonSerializer.Deserialize<T>(data.Span, Options);
    }

    public object? Deserialize(ReadOnlyMemory<byte> data, Type targetType)
    {
        return JsonSerializer.Deserialize(data.Span, targetType, Options);
    }
}
