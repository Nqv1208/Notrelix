using Notrelix.Domain.Common;

namespace Notrelix.Domain.Automation.Conditions;

public sealed class ConditionConfig : ValueObject
{
    public JsonValue Data { get; }

    private ConditionConfig() { }    private ConditionConfig(JsonValue data)
    {
        Data = data;
    }

    public static ConditionConfig Create(JsonValue data)
    {
        Guard.NotNull(data);
        return new ConditionConfig(data);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Data;
    }
}
