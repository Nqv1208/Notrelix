using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.BillingEvents;

public sealed class ProviderEventId : ValueObject
{
    public string Value { get; }

    private ProviderEventId() { }    private ProviderEventId(string value)
    {
        Value = value;
    }

    public static ProviderEventId Create(string value)
    {
        Guard.NotNullOrWhiteSpace(value);
        return new ProviderEventId(value.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
