using static Notrelix.Domain.Billing.BillingRuleCodes;

namespace Notrelix.Domain.Billing.Usage;

public sealed class UsagePeriod : ValueObject
{
    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; }

    private UsagePeriod() { }
    private UsagePeriod(DateTimeOffset start, DateTimeOffset end)
    {
        Start = start;
        End = end;
    }

    public static UsagePeriod Create(DateTimeOffset start, DateTimeOffset end)
    {
        if (start >= end) throw new BusinessRuleException(Billing_Usage_StartMustBeBeforeEnd, "Usage period start must be before end.");
        return new UsagePeriod(start, end);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }
}
