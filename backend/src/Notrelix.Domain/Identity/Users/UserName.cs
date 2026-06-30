namespace Notrelix.Domain.Identity.Users;

public sealed class UserName : ValueObject
{
    public string Value { get; private set; } = null!;

    private UserName() { }
    private UserName(string value)
    {
        Value = value;
    }

    public static UserName Create(string value)
    {
        Guard.NotNullOrWhiteSpace(value);
        return new UserName(value.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
