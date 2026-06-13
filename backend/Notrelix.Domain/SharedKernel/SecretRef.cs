using Notrelix.Domain.Common;

namespace Notrelix.Domain.SharedKernel;

public sealed class SecretRef : ValueObject
{
    public string Value { get; }

    private SecretRef() { }    private SecretRef(string value)
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

    public override string ToString() => Value;
}
