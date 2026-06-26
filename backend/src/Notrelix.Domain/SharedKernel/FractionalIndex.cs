namespace Notrelix.Domain.SharedKernel;

public sealed class FractionalIndex : ValueObject, IComparable<FractionalIndex>
{
    public string Value { get; }

    private FractionalIndex() { }
    private FractionalIndex(string value)
    {
        Guard.NotNullOrWhiteSpace(value);
        Value = value;
    }

    public static FractionalIndex Create(string value) => new(value);
    public static FractionalIndex Initial() => new("a0");

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public int CompareTo(FractionalIndex? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;
        return string.CompareOrdinal(Value, other.Value);
    }

    public override string ToString() => Value;
}
