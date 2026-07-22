using System.Text.Json;

namespace Notrelix.Platform.Messaging.Contracts;

public sealed class JsonCanonicalizer : ICanonicalizer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public ReadOnlyMemory<byte> Canonicalize(ReadOnlyMemory<byte> data)
    {
        using var doc = JsonDocument.Parse(data);
        return JsonSerializer.SerializeToUtf8Bytes(doc.RootElement, Options);
    }
}
