namespace Notrelix.Domain.Identity.Tokens;

public sealed class TokenHash : ValueObject
{
    public string Value { get; } = null!;

    private TokenHash() { }
    private TokenHash(string value)
    {
        Value = value;
    }

    public static TokenHash Create(string value)
    {
        Guard.NotNullOrWhiteSpace(value);
        return new TokenHash(value.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
