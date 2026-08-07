using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Notrelix.Application.Common.Idempotency;

/// <summary>
/// Deterministic request fingerprint using canonical JSON serialization.
/// Algorithm:
/// 1. Serialize request to JsonNode with shared deterministic options.
/// 2. Remove properties decorated with [IdempotencyFingerprintIgnore].
/// 3. Recursively sort object property names (ordinal).
/// 4. Preserve array order.
/// 5. Serialize compact UTF-8.
/// 6. SHA-256 full 64 uppercase hexadecimal characters.
/// </summary>
public sealed class JsonIdempotencyRequestFingerprint : IIdempotencyRequestFingerprint
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public string Compute(IIdempotentRequest request, Type requestType)
    {
        var node = JsonSerializer.SerializeToNode(request, requestType, SerializerOptions)
            ?? throw new InvalidOperationException("Request serialized to null JsonNode.");

        if (node is not JsonObject root)
            throw new InvalidOperationException("Request must serialize to a JSON object.");

        RemoveIgnoredProperties(root, requestType);
        SortPropertiesRecursively(root);

        var compact = root.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(compact));
        return Convert.ToHexString(bytes);
    }

    private static void RemoveIgnoredProperties(JsonObject root, Type requestType)
    {
        var properties = requestType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            if (prop.GetCustomAttribute<IdempotencyFingerprintIgnoreAttribute>() is null)
                continue;

            var jsonName = SerializerOptions.PropertyNamingPolicy?.ConvertName(prop.Name) ?? prop.Name;
            root.Remove(jsonName);
        }
    }

    private static void SortPropertiesRecursively(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                var keys = obj.Select(kvp => kvp.Key).OrderBy(k => k, StringComparer.Ordinal).ToList();
                var values = keys.Select(k => obj[k]?.DeepClone()).ToList();

                foreach (var key in keys)
                    obj.Remove(key);

                for (var i = 0; i < keys.Count; i++)
                {
                    obj.Add(keys[i], values[i]);
                    SortPropertiesRecursively(values[i]);
                }
                break;

            case JsonArray arr:
                foreach (var item in arr)
                    SortPropertiesRecursively(item);
                break;
        }
    }
}
