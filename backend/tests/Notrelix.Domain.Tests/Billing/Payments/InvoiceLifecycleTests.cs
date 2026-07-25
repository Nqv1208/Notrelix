using FluentAssertions;
using Notrelix.Domain.Billing.Payments;

namespace Notrelix.Domain.Tests.Billing.Payments;

public class InvoiceLifecycleTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Invoice_Create_ShouldRaiseEvent()
    {
        var invoice = Invoice.Create(Guid.NewGuid(), Guid.NewGuid(), "INV-001", Money.Create(100, "USD"), Now.AddDays(30), Now, WsA);

        invoice.DomainEvents.Should().ContainSingle(e => e is InvoiceCreatedDomainEvent);
        var evt = (InvoiceCreatedDomainEvent)invoice.DomainEvents.Single(e => e is InvoiceCreatedDomainEvent);
        evt.InvoiceId.Should().Be(invoice.Id);
        evt.WorkspaceId.Should().Be(WsA);
        evt.Amount.Should().Be(Money.Create(100, "USD"));
    }

    [Fact]
    public void Invoice_Void_ShouldRaiseEvent()
    {
        var invoice = Invoice.Create(Guid.NewGuid(), Guid.NewGuid(), "INV-001", Money.Create(100, "USD"), Now.AddDays(30), Now, WsA);
        ((IHasDomainEvents)invoice).ClearDomainEvents();
        var version = invoice.Version;

        invoice.Void(Now);

        invoice.Version.Should().Be(version + 1);
        invoice.DomainEvents.Should().ContainSingle(e => e is InvoiceVoidedDomainEvent);
        var evt = (InvoiceVoidedDomainEvent)invoice.DomainEvents.Single(e => e is InvoiceVoidedDomainEvent);
        evt.InvoiceId.Should().Be(invoice.Id);
        evt.WorkspaceId.Should().Be(WsA);
    }

    [Fact]
    public void Invoice_Void_WhenAlreadyVoided_ShouldNotRaiseEvent()
    {
        var invoice = Invoice.Create(Guid.NewGuid(), Guid.NewGuid(), "INV-001", Money.Create(100, "USD"), Now.AddDays(30), Now, WsA);
        invoice.Void(Now);
        ((IHasDomainEvents)invoice).ClearDomainEvents();
        var version = invoice.Version;

        invoice.Void(Now);

        invoice.Version.Should().Be(version);
        invoice.DomainEvents.Should().NotContain(e => e is InvoiceVoidedDomainEvent);
    }

    [Fact]
    public void Invoice_SoftDelete_ShouldRaiseEvent()
    {
        var invoice = Invoice.Create(Guid.NewGuid(), Guid.NewGuid(), "INV-001", Money.Create(100, "USD"), Now.AddDays(30), Now, WsA);
        ((IHasDomainEvents)invoice).ClearDomainEvents();
        var version = invoice.Version;

        invoice.SoftDelete(Actor, Now);

        invoice.IsDeleted.Should().BeTrue();
        invoice.Version.Should().Be(version + 1);
        invoice.DomainEvents.Should().ContainSingle(e => e is InvoiceSoftDeletedDomainEvent);
        var evt = (InvoiceSoftDeletedDomainEvent)invoice.DomainEvents.Single(e => e is InvoiceSoftDeletedDomainEvent);
        evt.WorkspaceId.Should().Be(WsA);
        evt.InvoiceId.Should().Be(invoice.Id);
        evt.DeletedBy.Should().Be(Actor);
    }

    [Fact]
    public void Invoice_Restore_ShouldRaiseEvent()
    {
        var invoice = Invoice.Create(Guid.NewGuid(), Guid.NewGuid(), "INV-001", Money.Create(100, "USD"), Now.AddDays(30), Now, WsA);
        invoice.SoftDelete(Actor, Now);
        ((IHasDomainEvents)invoice).ClearDomainEvents();
        var version = invoice.Version;

        invoice.Restore(Actor, Now);

        invoice.IsDeleted.Should().BeFalse();
        invoice.Version.Should().Be(version + 1);
        invoice.DomainEvents.Should().ContainSingle(e => e is InvoiceRestoredDomainEvent);
        var evt = (InvoiceRestoredDomainEvent)invoice.DomainEvents.Single(e => e is InvoiceRestoredDomainEvent);
        evt.WorkspaceId.Should().Be(WsA);
        evt.InvoiceId.Should().Be(invoice.Id);
        evt.RestoredBy.Should().Be(Actor);
    }

    [Fact]
    public void Invoice_SoftDelete_WhenAlreadyDeleted_ShouldNotRaiseEvent()
    {
        var invoice = Invoice.Create(Guid.NewGuid(), Guid.NewGuid(), "INV-001", Money.Create(100, "USD"), Now.AddDays(30), Now, WsA);
        invoice.SoftDelete(Actor, Now);
        ((IHasDomainEvents)invoice).ClearDomainEvents();
        var version = invoice.Version;

        invoice.SoftDelete(Actor, Now);

        invoice.Version.Should().Be(version);
        invoice.DomainEvents.Should().NotContain(e => e is InvoiceSoftDeletedDomainEvent);
    }

    [Fact]
    public void Invoice_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var invoice = Invoice.Create(Guid.NewGuid(), Guid.NewGuid(), "INV-001", Money.Create(100, "USD"), Now.AddDays(30), Now, WsA);
        ((IHasDomainEvents)invoice).ClearDomainEvents();
        var version = invoice.Version;

        invoice.Restore(Actor, Now);

        invoice.Version.Should().Be(version);
        invoice.DomainEvents.Should().NotContain(e => e is InvoiceRestoredDomainEvent);
    }
}
