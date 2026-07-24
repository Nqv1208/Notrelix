using System.Text.RegularExpressions;

namespace Notrelix.Domain.SharedKernel;

public sealed class Money : ValueObject
{
    private static readonly Regex CurrencyRegex = new(
        @"^[A-Z]{3}$",
        RegexOptions.Compiled);

    public decimal Amount { get; }
    public string Currency { get; } = null!;

    private Money() { }
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency)
    {
        Guard.NotNullOrWhiteSpace(currency);
        currency = currency.Trim().ToUpperInvariant();
        if (!CurrencyRegex.IsMatch(currency))
            throw new BusinessRuleException(BusinessRuleCodes.SharedKernel_Money_InvalidCurrency, "Currency must be a 3-letter uppercase ISO code.");

        return new Money(amount, currency);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount} {Currency}";
}
