using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Payments;

public class Invoice : AggregateRoot
{
    public Guid WorkspaceId { get; private set; }
    public Guid SubscriptionId { get; private set; }
    public string Number { get; private set; } = null!;
    public Money Amount { get; private set; } = null!;
    public InvoiceStatus Status { get; private set; }
    public DateTimeOffset DueAt { get; private set; }

    private Invoice() : base() { }

    public static Invoice Create(Guid workspaceId, Guid subscriptionId, string number, Money amount, DateTimeOffset dueAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNullOrWhiteSpace(number);
        Guard.NotNull(amount);

        return new Invoice
        {
            WorkspaceId = workspaceId,
            SubscriptionId = subscriptionId,
            Number = number,
            Amount = amount,
            Status = InvoiceStatus.Draft,
            DueAt = dueAt
        };
    }
}
