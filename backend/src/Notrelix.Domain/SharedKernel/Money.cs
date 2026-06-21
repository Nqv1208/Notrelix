using Notrelix.Domain.Common;

namespace Notrelix.Domain.SharedKernel;

public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money() { }    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency)
    {
        Guard.NotNullOrWhiteSpace(currency);
        Guard.Assert(currency.Length == 3, "Currency must be a 3-letter ISO code.");
        
        return new Money(amount, currency.ToUpperInvariant());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount} {Currency}";
}
