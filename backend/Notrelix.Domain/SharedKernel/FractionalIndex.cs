using Notrelix.Domain.Common;

namespace Notrelix.Domain.SharedKernel;

public sealed class FractionalIndex : ValueObject, IComparable<FractionalIndex>
{
    public double Value { get; }

    private FractionalIndex(double value)
    {
        Value = value;
    }

    public static FractionalIndex Create(double value)
    {
        return new FractionalIndex(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public int CompareTo(FractionalIndex? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;
        return Value.CompareTo(other.Value);
    }

    public override string ToString() => Value.ToString("G");
}
