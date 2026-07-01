namespace Notrelix.Domain.Billing.Payments;

public class InvoiceLineItem : Entity
{
    public Guid InvoiceId { get; private set; }
    public string Description { get; private set; } = null!;
    public decimal Quantity { get; private set; }
    public decimal UnitAmount { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = null!;

    private InvoiceLineItem() { }

    public static InvoiceLineItem Create(Guid invoiceId, string description, decimal quantity, decimal unitAmount, string currency)
    {
        Guard.NotEmpty(invoiceId);
        Guard.NotNullOrWhiteSpace(description);
        Guard.NotNegative((double)quantity);
        Guard.NotNegative((double)unitAmount);
        Guard.NotNullOrWhiteSpace(currency);

        return new InvoiceLineItem
        {
            InvoiceId = invoiceId,
            Description = description,
            Quantity = quantity,
            UnitAmount = unitAmount,
            Amount = quantity * unitAmount,
            Currency = currency.ToUpperInvariant()
        };
    }
}
