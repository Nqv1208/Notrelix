using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Billing.Payments.Events;

namespace Notrelix.Domain.Billing.Payments;

public class Invoice : AggregateRoot, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public Guid SubscriptionId { get; private set; }
    public string Number { get; private set; } = null!;
    public Money Amount { get; private set; } = null!;
    public InvoiceStatus Status { get; private set; }
    public DateTimeOffset DueAt { get; private set; }

    private Invoice() : base() { }

    public static Invoice Create(Guid workspaceId, Guid subscriptionId, string number, Money amount, DateTimeOffset dueAt, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNullOrWhiteSpace(number);
        Guard.NotNull(amount);

        var invoice = new Invoice
        {
            WorkspaceId = workspaceId,
            SubscriptionId = subscriptionId,
            Number = number,
            Amount = amount,
            Status = InvoiceStatus.Draft,
            DueAt = dueAt
        };

        invoice.SetAuditOnCreate(null, createdAt);
        invoice.AddDomainEvent(new InvoiceCreatedDomainEvent(invoice.Id, workspaceId, amount, dueAt, createdAt));
        return invoice;
    }

    public void Issue(DateTimeOffset issuedAt)
    {
        if (Status != InvoiceStatus.Draft)
            throw new BusinessRuleException("Only draft invoices can be issued.");
        Status = InvoiceStatus.Open;
        SetAuditOnUpdate(null, issuedAt);
        AddDomainEvent(new InvoiceIssuedDomainEvent(Id, WorkspaceId, Amount, issuedAt));
    }

    public void MarkPaid(DateTimeOffset paidAt)
    {
        if (Status == InvoiceStatus.Void)
            throw new BusinessRuleException("Cannot mark a void invoice as paid.");
        if (Status == InvoiceStatus.Paid) return;

        Status = InvoiceStatus.Paid;
        SetAuditOnUpdate(null, paidAt);
        AddDomainEvent(new InvoicePaidDomainEvent(Id, WorkspaceId, paidAt));
    }

    public void MarkFailed(string reason, DateTimeOffset failedAt)
    {
        if (Status == InvoiceStatus.Paid)
            throw new BusinessRuleException("Cannot fail a paid invoice.");
        if (Status == InvoiceStatus.Void)
            throw new BusinessRuleException("Cannot fail a void invoice.");

        Status = InvoiceStatus.Uncollectible;
        SetAuditOnUpdate(null, failedAt);
        AddDomainEvent(new InvoiceFailedDomainEvent(Id, WorkspaceId, reason, failedAt));
    }

    public void Void(DateTimeOffset voidedAt)
    {
        if (Status == InvoiceStatus.Paid)
            throw new BusinessRuleException("Cannot void a paid invoice.");
        if (Status == InvoiceStatus.Void) return;

        Status = InvoiceStatus.Void;
        SetAuditOnUpdate(null, voidedAt);
        IncrementVersion();
        AddDomainEvent(new InvoiceVoidedDomainEvent(Id, WorkspaceId, voidedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        IncrementVersion();
        AddDomainEvent(new InvoiceSoftDeletedDomainEvent(WorkspaceId, Id, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        base.Restore(restoredBy, restoredAt);
        IncrementVersion();
        AddDomainEvent(new InvoiceRestoredDomainEvent(WorkspaceId, Id, restoredBy, restoredAt));
    }
}
