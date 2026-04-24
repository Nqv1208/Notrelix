namespace Notrelix.Domain.ValueObjects;

/// <summary>
/// Value object cho fractional indexing — quản lý position khi chèn/reorder items
/// Cho phép chèn giữa 2 items mà không cần reindex toàn bộ: O(1) thay vì O(n)
/// </summary>
public class FractionalIndex : ValueObject
{
    public double Value { get; private set; }

    private const double DefaultGap = 1.0;
    private const double MinGap = 0.0001;

    private FractionalIndex(double value)
    {
        Value = value;
    }

    public static FractionalIndex Create(double value)
    {
        return new FractionalIndex(value);
    }

    /// <summary>
    /// Tạo position ở giữa 2 items. Nếu before/after null thì tạo ở đầu/cuối.
    /// </summary>
    public static double GenerateBetween(double? before, double? after)
    {
        if (before is null && after is null)
            return DefaultGap;

        if (before is null)
            return after!.Value - DefaultGap;

        if (after is null)
            return before.Value + DefaultGap;

        var mid = (before.Value + after.Value) / 2.0;

        // Nếu khoảng cách quá nhỏ, cần rebalance (application layer xử lý)
        if (Math.Abs(after.Value - before.Value) < MinGap)
            throw new InvalidOperationException(
                "Khoảng cách position quá nhỏ, cần rebalance. " +
                $"Before={before.Value}, After={after.Value}");

        return mid;
    }

    /// <summary>
    /// Tạo position cho item mới ở cuối danh sách
    /// </summary>
    public static double GenerateAfterLast(double? lastPosition)
    {
        return (lastPosition ?? 0) + DefaultGap;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString("F6");

    public static implicit operator double(FractionalIndex index) => index.Value;
}
