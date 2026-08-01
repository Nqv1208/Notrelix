using FluentAssertions;
using Notrelix.Domain.Billing.Payments;

namespace Notrelix.Domain.Tests.Billing;

public class InvoiceTests
{
    private static readonly Money SampleAmount = Money.Create(99.99m, "USD");

    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var invoice = Invoice.Create(Guid.NewGuid(), Guid.NewGuid(), "INV-001", SampleAmount, DateTimeOffset.UtcNow.AddDays(30), DateTimeOffset.UtcNow);

        invoice.Status.Should().Be(InvoiceStatus.Draft);
        invoice.Number.Should().Be("INV-001");
        invoice.DomainEvents.Should().ContainSingle(e => e is InvoiceCreatedDomainEvent);
    }

    [Fact]
    public void Issue_ShouldTransition_AndRaiseEvent()
    {
        var invoice = CreateDraftInvoice();
        ((IHasDomainEvents)invoice).ClearDomainEvents();

        invoice.Issue(DateTimeOffset.UtcNow);

        invoice.Status.Should().Be(InvoiceStatus.Open);
        invoice.DomainEvents.Should().ContainSingle(e => e is InvoiceIssuedDomainEvent);
    }

    [Fact]
    public void Issue_WhenNotDraft_ShouldThrow()
    {
        var invoice = CreateDraftInvoice();
        invoice.Issue(DateTimeOffset.UtcNow);

        var act = () => invoice.Issue(DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*draft*");
    }

    [Fact]
    public void MarkPaid_ShouldTransition_AndRaiseEvent()
    {
        var invoice = CreateDraftInvoice();
        invoice.Issue(DateTimeOffset.UtcNow);
        ((IHasDomainEvents)invoice).ClearDomainEvents();

        invoice.MarkPaid(DateTimeOffset.UtcNow);

        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.DomainEvents.Should().ContainSingle(e => e is InvoicePaidDomainEvent);
    }

    [Fact]
    public void MarkPaid_WhenAlreadyPaid_ShouldBeNoOp()
    {
        var invoice = CreateDraftInvoice();
        invoice.Issue(DateTimeOffset.UtcNow);
        invoice.MarkPaid(DateTimeOffset.UtcNow);
        ((IHasDomainEvents)invoice).ClearDomainEvents();

        invoice.MarkPaid(DateTimeOffset.UtcNow);

        invoice.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void MarkPaid_WhenVoid_ShouldThrow()
    {
        var invoice = CreateDraftInvoice();
        invoice.Void(DateTimeOffset.UtcNow);

        var act = () => invoice.MarkPaid(DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*void*");
    }

    [Fact]
    public void MarkFailed_ShouldTransition_AndRaiseEvent()
    {
        var invoice = CreateDraftInvoice();
        invoice.Issue(DateTimeOffset.UtcNow);
        ((IHasDomainEvents)invoice).ClearDomainEvents();

        invoice.MarkFailed("Payment declined", DateTimeOffset.UtcNow);

        invoice.Status.Should().Be(InvoiceStatus.Uncollectible);
        invoice.DomainEvents.Should().ContainSingle(e => e is InvoiceFailedDomainEvent);
    }

    [Fact]
    public void MarkFailed_WhenPaid_ShouldThrow()
    {
        var invoice = CreateDraftInvoice();
        invoice.Issue(DateTimeOffset.UtcNow);
        invoice.MarkPaid(DateTimeOffset.UtcNow);

        var act = () => invoice.MarkFailed("Error", DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*paid*");
    }

    [Fact]
    public void MarkFailed_WhenVoid_ShouldThrow()
    {
        var invoice = CreateDraftInvoice();
        invoice.Void(DateTimeOffset.UtcNow);

        var act = () => invoice.MarkFailed("Error", DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*void*");
    }

    [Fact]
    public void Void_ShouldTransition_AndRaiseEvent()
    {
        var invoice = CreateDraftInvoice();
        invoice.Issue(DateTimeOffset.UtcNow);
        ((IHasDomainEvents)invoice).ClearDomainEvents();

        invoice.Void(DateTimeOffset.UtcNow);

        invoice.Status.Should().Be(InvoiceStatus.Void);
        invoice.DomainEvents.Should().ContainSingle(e => e is InvoiceVoidedDomainEvent);
    }

    [Fact]
    public void Void_WhenAlreadyVoid_ShouldBeNoOp()
    {
        var invoice = CreateDraftInvoice();
        invoice.Void(DateTimeOffset.UtcNow);
        ((IHasDomainEvents)invoice).ClearDomainEvents();

        invoice.Void(DateTimeOffset.UtcNow);

        invoice.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Void_WhenPaid_ShouldThrow()
    {
        var invoice = CreateDraftInvoice();
        invoice.Issue(DateTimeOffset.UtcNow);
        invoice.MarkPaid(DateTimeOffset.UtcNow);

        var act = () => invoice.Void(DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*paid*");
    }

    [Fact]
    public void MarkFailed_WhenDraft_ShouldSucceed()
    {
        var invoice = CreateDraftInvoice();
        ((IHasDomainEvents)invoice).ClearDomainEvents();

        invoice.MarkFailed("Cancelled by customer", DateTimeOffset.UtcNow);

        invoice.Status.Should().Be(InvoiceStatus.Uncollectible);
        invoice.DomainEvents.Should().ContainSingle(e => e is InvoiceFailedDomainEvent);
    }

    [Fact]
    public void Void_WhenOpen_ShouldSucceed()
    {
        var invoice = CreateDraftInvoice();
        invoice.Issue(DateTimeOffset.UtcNow);
        ((IHasDomainEvents)invoice).ClearDomainEvents();

        invoice.Void(DateTimeOffset.UtcNow);

        invoice.Status.Should().Be(InvoiceStatus.Void);
    }

    [Fact]
    public void MarkPaid_WhenUncollectible_ShouldSucceed()
    {
        var invoice = CreateDraftInvoice();
        invoice.MarkFailed("Retry", DateTimeOffset.UtcNow);
        ((IHasDomainEvents)invoice).ClearDomainEvents();

        invoice.MarkPaid(DateTimeOffset.UtcNow);

        invoice.Status.Should().Be(InvoiceStatus.Paid);
    }

    private static Invoice CreateDraftInvoice()
    {
        return Invoice.Create(Guid.NewGuid(), Guid.NewGuid(), "INV-001", SampleAmount, DateTimeOffset.UtcNow.AddDays(30), DateTimeOffset.UtcNow);
    }
}
