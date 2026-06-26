namespace Notrelix.Domain.Billing.Usage;

public sealed class UsageMetricKey : ValueObject
{
    public string Value { get; }

    private UsageMetricKey() { }
    private UsageMetricKey(string value)
    {
        Value = value;
    }

    public static UsageMetricKey Create(string value)
    {
        Guard.NotNullOrWhiteSpace(value);
        return new UsageMetricKey(value.Trim().ToUpperInvariant());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
