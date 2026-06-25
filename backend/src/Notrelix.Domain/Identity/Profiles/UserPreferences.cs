namespace Notrelix.Domain.Identity.Profiles;

public sealed class UserPreferences : ValueObject
{
    public JsonValue Data { get; }

    private UserPreferences() { }
    private UserPreferences(JsonValue data)
    {
        Data = data;
    }

    public static UserPreferences Create(JsonValue data)
    {
        Guard.NotNull(data);
        return new UserPreferences(data);
    }

    public static UserPreferences Default() => new(JsonValue.EmptyObject());

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Data;
    }
}
