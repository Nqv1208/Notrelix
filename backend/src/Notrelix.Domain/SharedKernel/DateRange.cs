using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.SharedKernel;

public sealed class DateRange : ValueObject
{
    public DateTimeOffset Start { get; }
    public DateTimeOffset? End { get; }

    private DateRange() { }
    private DateRange(DateTimeOffset start, DateTimeOffset? end)
    {
        Start = start;
        End = end;
    }

    public static DateRange Create(DateTimeOffset start, DateTimeOffset? end = null)
    {
        if (start == default || start == DateTimeOffset.MinValue)
            throw new BusinessRuleException(BusinessRuleCodes.SharedKernel_DateRange_StartAfterEnd, "Start date must be a valid date.");

        if (end.HasValue)
        {
            if (start > end.Value)
                throw new BusinessRuleException(BusinessRuleCodes.SharedKernel_DateRange_StartAfterEnd, "Start date must be before or equal to the end date.");
        }

        return new DateRange(start, end);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }
}
