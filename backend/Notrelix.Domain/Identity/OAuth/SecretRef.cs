using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.OAuth;

public sealed class SecretRef : ValueObject
{
    public string Value { get; }

    private SecretRef(string value)
    {
        Value = value;
    }

    public static SecretRef Create(string value)
    {
        Guard.NotNullOrWhiteSpace(value);
        return new SecretRef(value.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
