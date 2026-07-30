using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.Billing.Payments;

namespace Notrelix.Domain.Tests.Billing;

[CoversAggregate(typeof(Invoice))]
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

    [CoversMutation(typeof(Invoice), "Issue(System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void Issue_ShouldTransition_AndRaiseEvent()
    {
        var invoice = CreateDraftInvoice();
        ((IHasDomainEvents)invoice).ClearDomainEvents();

        invoice.Issue(DateTimeOffset.UtcNow);

        invoice.Status.Should().Be(InvoiceStatus.Open);
        invoice.DomainEvents.Should().ContainSingle(e => e is InvoiceIssuedDomainEvent);
    }

    [CoversMutation(typeof(Invoice), "Issue(System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void Issue_WhenNotDraft_ShouldThrow()
    {
        var invoice = CreateDraftInvoice();
        invoice.Issue(DateTimeOffset.UtcNow);

        var act = () => invoice.Issue(DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*draft*");
    }

    [CoversMutation(typeof(Invoice), "MarkFailed(System.String,System.DateTimeOffset)", MutationScenario.Event)]
    [CoversMutation(typeof(Invoice), "MarkPaid(System.DateTimeOffset)", MutationScenario.Event)]
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

    [CoversMutation(typeof(Invoice), "MarkFailed(System.String,System.DateTimeOffset)", MutationScenario.NoOp)]
    [CoversMutation(typeof(Invoice), "MarkPaid(System.DateTimeOffset)", MutationScenario.NoOp)]
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

    [CoversMutation(typeof(Invoice), "Void(System.DateTimeOffset)", MutationScenario.Invalid)]
    [CoversMutation(typeof(Invoice), "MarkFailed(System.String,System.DateTimeOffset)", MutationScenario.Invalid)]
    [CoversMutation(typeof(Invoice), "MarkPaid(System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void MarkPaid_WhenVoid_ShouldThrow()
    {
        var invoice = CreateDraftInvoice();
        invoice.Void(DateTimeOffset.UtcNow);

        var act = () => invoice.MarkPaid(DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*void*");
    }

    [CoversMutation(typeof(Invoice), "MarkFailed(System.String,System.DateTimeOffset)", MutationScenario.Event)]
    [CoversMutation(typeof(Invoice), "MarkPaid(System.DateTimeOffset)", MutationScenario.Event)]
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

    [CoversMutation(typeof(Invoice), "MarkFailed(System.String,System.DateTimeOffset)", MutationScenario.Invalid)]
    [CoversMutation(typeof(Invoice), "MarkPaid(System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void MarkFailed_WhenPaid_ShouldThrow()
    {
        var invoice = CreateDraftInvoice();
        invoice.Issue(DateTimeOffset.UtcNow);
        invoice.MarkPaid(DateTimeOffset.UtcNow);

        var act = () => invoice.MarkFailed("Error", DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*paid*");
    }

    [CoversMutation(typeof(Invoice), "Void(System.DateTimeOffset)", MutationScenario.Invalid)]
    [CoversMutation(typeof(Invoice), "MarkFailed(System.String,System.DateTimeOffset)", MutationScenario.Invalid)]
    [CoversMutation(typeof(Invoice), "MarkPaid(System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void MarkFailed_WhenVoid_ShouldThrow()
    {
        var invoice = CreateDraftInvoice();
        invoice.Void(DateTimeOffset.UtcNow);

        var act = () => invoice.MarkFailed("Error", DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*void*");
    }

    [CoversMutation(typeof(Invoice), "Void(System.DateTimeOffset)", MutationScenario.Event)]
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

    [CoversMutation(typeof(Invoice), "Void(System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void Void_WhenAlreadyVoid_ShouldBeNoOp()
    {
        var invoice = CreateDraftInvoice();
        invoice.Void(DateTimeOffset.UtcNow);
        ((IHasDomainEvents)invoice).ClearDomainEvents();

        invoice.Void(DateTimeOffset.UtcNow);

        invoice.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(Invoice), "Void(System.DateTimeOffset)", MutationScenario.Invalid)]
    [CoversMutation(typeof(Invoice), "MarkPaid(System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void Void_WhenPaid_ShouldThrow()
    {
        var invoice = CreateDraftInvoice();
        invoice.Issue(DateTimeOffset.UtcNow);
        invoice.MarkPaid(DateTimeOffset.UtcNow);

        var act = () => invoice.Void(DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*paid*");
    }

    [CoversMutation(typeof(Invoice), "MarkFailed(System.String,System.DateTimeOffset)", MutationScenario.Valid)]
    [CoversMutation(typeof(Invoice), "MarkPaid(System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void MarkFailed_WhenDraft_ShouldSucceed()
    {
        var invoice = CreateDraftInvoice();
        ((IHasDomainEvents)invoice).ClearDomainEvents();

        invoice.MarkFailed("Cancelled by customer", DateTimeOffset.UtcNow);

        invoice.Status.Should().Be(InvoiceStatus.Uncollectible);
        invoice.DomainEvents.Should().ContainSingle(e => e is InvoiceFailedDomainEvent);
    }

    [CoversMutation(typeof(Invoice), "Void(System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void Void_WhenOpen_ShouldSucceed()
    {
        var invoice = CreateDraftInvoice();
        invoice.Issue(DateTimeOffset.UtcNow);
        ((IHasDomainEvents)invoice).ClearDomainEvents();

        invoice.Void(DateTimeOffset.UtcNow);

        invoice.Status.Should().Be(InvoiceStatus.Void);
    }

    [CoversMutation(typeof(Invoice), "MarkFailed(System.String,System.DateTimeOffset)", MutationScenario.Valid)]
    [CoversMutation(typeof(Invoice), "MarkPaid(System.DateTimeOffset)", MutationScenario.Valid)]
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
