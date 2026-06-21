using Notrelix.Domain.Common;

namespace Notrelix.Domain.Collaboration.Activity;

public sealed class ActivityMetadata : ValueObject
{
    public JsonValue Data { get; }

    private ActivityMetadata() { }    private ActivityMetadata(JsonValue data)
    {
        Data = data;
    }

    public static ActivityMetadata Create(JsonValue data)
    {
        Guard.NotNull(data);
        return new ActivityMetadata(data);
    }

    public static ActivityMetadata Empty() => new(JsonValue.EmptyObject());

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Data;
    }
}
