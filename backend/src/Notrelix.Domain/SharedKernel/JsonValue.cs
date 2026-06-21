using System.Text.Json;
using Notrelix.Domain.Common;

namespace Notrelix.Domain.SharedKernel;

public sealed class JsonValue : ValueObject
{
    public string Value { get; }

    private JsonValue() { }    private JsonValue(string value)
    {
        Value = value;
    }

    public static JsonValue Create(string jsonString)
    {
        Guard.NotNullOrWhiteSpace(jsonString);

        try
        {
            using var document = JsonDocument.Parse(jsonString);
            return new JsonValue(jsonString);
        }
        catch (JsonException ex)
        {
            throw new BusinessRuleException($"Invalid JSON format: {ex.Message}");
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
