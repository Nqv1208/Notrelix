using System.Text.Json;

namespace Notrelix.Domain.SharedKernel;

public sealed class JsonValue : ValueObject
{
    public string Value { get; } = null!;

    private JsonValue() { }
    private JsonValue(string value)
    {
        Value = value;
    }

    public static JsonValue Create(string jsonString)
    {
        Guard.NotNullOrWhiteSpace(jsonString);

        try
        {
            using var document = JsonDocument.Parse(jsonString);
            // Store compact form for deterministic equality.
            var compact = JsonSerializer.Serialize(document.RootElement);
            return new JsonValue(compact);
        }
        catch (JsonException)
        {
            throw new BusinessRuleException(
                BusinessRuleCodes.SharedKernel_Json_InvalidFormat,
                "Invalid JSON format.");
        }
    }

    public static JsonValue EmptyObject() => new("{}");
    public static JsonValue EmptyArray() => new("[]");
    public static JsonValue Null() => new("null");

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
