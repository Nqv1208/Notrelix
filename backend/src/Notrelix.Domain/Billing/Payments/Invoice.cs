using Notrelix.Domain.Billing.Payments.Events;
using static Notrelix.Domain.Billing.BillingRuleCodes;

namespace Notrelix.Domain.Billing.Payments;

public class Invoice : SoftDeletableAggregateRoot, IAccountScoped
{
    public Guid AccountId { get; private set; }
    public Guid? WorkspaceId { get; private set; }
    public Guid SubscriptionId { get; private set; }
    public string Number { get; private set; } = null!;
    public Money Amount { get; private set; } = null!;
    public InvoiceStatus Status { get; private set; }
    public DateTimeOffset DueAt { get; private set; }

    private Invoice() : base() { }

    public static Invoice Create(Guid accountId, Guid subscriptionId, string number, Money amount, DateTimeOffset dueAt, DateTimeOffset createdAt, Guid? workspaceId = null, Guid? createdBy = null)
    {
        Guard.NotEmpty(accountId);
        Guard.NotNullOrWhiteSpace(number);
        Guard.NotNull(amount);

        var invoice = new Invoice
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            SubscriptionId = subscriptionId,
            Number = number,
            Amount = amount,
            Status = InvoiceStatus.Draft,
            DueAt = dueAt
        };

        invoice.SetAuditOnCreate(createdBy, createdAt);
        invoice.RaiseDomainEvent(new InvoiceCreatedDomainEvent(accountId, invoice.Id, workspaceId, amount, dueAt, createdAt));
        return invoice;
    }

    public void Issue(DateTimeOffset issuedAt)
    {
        EnsureNotDeleted();
        if (Status != InvoiceStatus.Draft)
            throw new BusinessRuleException(Billing_Invoice_CannotIssueUnlessDraft, "Only draft invoices can be issued.");
        var pending = PrepareAuditUpdate(null, issuedAt);
        Status = InvoiceStatus.Open;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new InvoiceIssuedDomainEvent(AccountId, Id, WorkspaceId, Amount, issuedAt));
    }

    public void MarkPaid(DateTimeOffset paidAt)
    {
        EnsureNotDeleted();
        if (Status == InvoiceStatus.Void)
            throw new BusinessRuleException(Billing_Invoice_CannotMarkVoidAsPaid, "Cannot mark a void invoice as paid.");
        if (Status == InvoiceStatus.Paid) return;

        var pending = PrepareAuditUpdate(null, paidAt);
        Status = InvoiceStatus.Paid;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new InvoicePaidDomainEvent(AccountId, Id, WorkspaceId, paidAt));
    }

    public void MarkFailed(string reason, DateTimeOffset failedAt)
    {
        EnsureNotDeleted();
        if (Status == InvoiceStatus.Paid)
            throw new BusinessRuleException(Billing_Invoice_CannotFailPaid, "Cannot fail a paid invoice.");
        if (Status == InvoiceStatus.Void)
            throw new BusinessRuleException(Billing_Invoice_CannotFailVoid, "Cannot fail a void invoice.");

        var pending = PrepareAuditUpdate(null, failedAt);
        Status = InvoiceStatus.Uncollectible;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new InvoiceFailedDomainEvent(AccountId, Id, WorkspaceId, reason, failedAt));
    }

    public void Void(DateTimeOffset voidedAt)
    {
        EnsureNotDeleted();
        if (Status == InvoiceStatus.Paid)
            throw new BusinessRuleException(Billing_Invoice_CannotVoidPaid, "Cannot void a paid invoice.");
        if (Status == InvoiceStatus.Void) return;

        var pending = PrepareAuditUpdate(null, voidedAt);
        Status = InvoiceStatus.Void;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new InvoiceVoidedDomainEvent(AccountId, Id, WorkspaceId, voidedAt));
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;
        var pendingDeletion = PrepareDeletion(deletedBy, deletedAt, reason);
        ApplyDeletion(pendingDeletion);
        IncrementVersion();
        RaiseDomainEvent(new InvoiceSoftDeletedDomainEvent(AccountId, WorkspaceId, Id, deletedBy, deletedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;
        var pendingRestore = PrepareRestore(restoredBy, restoredAt);
        ApplyRestore(pendingRestore);
        IncrementVersion();
        RaiseDomainEvent(new InvoiceRestoredDomainEvent(AccountId, WorkspaceId, Id, restoredBy, restoredAt));
    }
}
