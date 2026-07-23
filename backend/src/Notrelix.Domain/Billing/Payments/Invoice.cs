namespace Notrelix.Domain.Billing.Payments;

public class Invoice : AggregateRoot, IAccountScoped
{
    public Guid AccountId { get; private set; }
    public Guid? WorkspaceId { get; private set; }
    public Guid SubscriptionId { get; private set; }
    public string Number { get; private set; } = null!;
    public Money Amount { get; private set; } = null!;
    public InvoiceStatus Status { get; private set; }
    public DateTimeOffset DueAt { get; private set; }

    private Invoice() : base() { }

    public static Invoice Create(Guid accountId, Guid subscriptionId, string number, Money amount, DateTimeOffset dueAt, DateTimeOffset createdAt, Guid? workspaceId = null)
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

        invoice.SetAuditOnCreate(null, createdAt);
        invoice.RaiseDomainEvent(new InvoiceCreatedDomainEvent(accountId, invoice.Id, workspaceId, amount, dueAt, createdAt));
        return invoice;
    }

    public void Issue(DateTimeOffset issuedAt)
    {
        if (Status != InvoiceStatus.Draft)
            throw new BusinessRuleException(BusinessRuleCodes.Billing_Invoice_CannotIssueUnlessDraft, "Only draft invoices can be issued.");
        Status = InvoiceStatus.Open;
        SetAuditOnUpdate(null, issuedAt);
        RaiseDomainEvent(new InvoiceIssuedDomainEvent(AccountId, Id, WorkspaceId, Amount, issuedAt));
    }

    public void MarkPaid(DateTimeOffset paidAt)
    {
        if (Status == InvoiceStatus.Void)
            throw new BusinessRuleException(BusinessRuleCodes.Billing_Invoice_CannotMarkVoidAsPaid, "Cannot mark a void invoice as paid.");
        if (Status == InvoiceStatus.Paid) return;

        Status = InvoiceStatus.Paid;
        SetAuditOnUpdate(null, paidAt);
        RaiseDomainEvent(new InvoicePaidDomainEvent(AccountId, Id, WorkspaceId, paidAt));
    }

    public void MarkFailed(string reason, DateTimeOffset failedAt)
    {
        if (Status == InvoiceStatus.Paid)
            throw new BusinessRuleException(BusinessRuleCodes.Billing_Invoice_CannotFailPaid, "Cannot fail a paid invoice.");
        if (Status == InvoiceStatus.Void)
            throw new BusinessRuleException(BusinessRuleCodes.Billing_Invoice_CannotFailVoid, "Cannot fail a void invoice.");

        Status = InvoiceStatus.Uncollectible;
        SetAuditOnUpdate(null, failedAt);
        RaiseDomainEvent(new InvoiceFailedDomainEvent(AccountId, Id, WorkspaceId, reason, failedAt));
    }

    public void Void(DateTimeOffset voidedAt)
    {
        if (Status == InvoiceStatus.Paid)
            throw new BusinessRuleException(BusinessRuleCodes.Billing_Invoice_CannotVoidPaid, "Cannot void a paid invoice.");
        if (Status == InvoiceStatus.Void) return;

        Status = InvoiceStatus.Void;
        SetAuditOnUpdate(null, voidedAt);
        IncrementVersion();
        RaiseDomainEvent(new InvoiceVoidedDomainEvent(AccountId, Id, WorkspaceId, voidedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        IncrementVersion();
        RaiseDomainEvent(new InvoiceSoftDeletedDomainEvent(AccountId, WorkspaceId, Id, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        base.Restore(restoredBy, restoredAt);
        IncrementVersion();
        RaiseDomainEvent(new InvoiceRestoredDomainEvent(AccountId, WorkspaceId, Id, restoredBy, restoredAt));
    }
}
