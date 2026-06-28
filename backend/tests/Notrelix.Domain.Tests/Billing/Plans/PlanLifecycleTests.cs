using FluentAssertions;
using Notrelix.Domain.Billing.Plans;

namespace Notrelix.Domain.Tests.Billing.Plans;

public class PlanLifecycleTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Plan_AddLimit_ShouldRaiseEvent()
    {
        var plan = Plan.Create("Pro", Money.Create(29, "USD"), BillingPeriod.Monthly, Now);
        plan.ClearDomainEvents();
        var version = plan.Version;

        plan.AddLimit(FeatureCode.Create("seats"), 10, Now);

        plan.Version.Should().Be(version + 1);
        plan.DomainEvents.Should().ContainSingle(e => e is PlanLimitAddedDomainEvent);
        var evt = (PlanLimitAddedDomainEvent)plan.DomainEvents.Single(e => e is PlanLimitAddedDomainEvent);
        evt.PlanId.Should().Be(plan.Id);
        evt.Limit.Should().Be(10);
    }

    [Fact]
    public void Plan_UpdateDescription_ShouldRaiseEvent()
    {
        var plan = Plan.Create("Pro", Money.Create(29, "USD"), BillingPeriod.Monthly, Now);
        plan.ClearDomainEvents();
        var version = plan.Version;

        plan.UpdateDescription("New desc", Now);

        plan.Version.Should().Be(version + 1);
        plan.DomainEvents.Should().ContainSingle(e => e is PlanDescriptionUpdatedDomainEvent);
        var evt = (PlanDescriptionUpdatedDomainEvent)plan.DomainEvents.Single(e => e is PlanDescriptionUpdatedDomainEvent);
        evt.PlanId.Should().Be(plan.Id);
        evt.Description.Should().Be("New desc");
    }

    [Fact]
    public void Plan_Archive_ShouldRaiseEvent()
    {
        var plan = Plan.Create("Pro", Money.Create(29, "USD"), BillingPeriod.Monthly, Now);
        plan.ClearDomainEvents();
        var version = plan.Version;

        plan.Archive(Now);

        plan.Version.Should().Be(version + 1);
        plan.DomainEvents.Should().ContainSingle(e => e is PlanArchivedDomainEvent);
    }

    [Fact]
    public void Plan_Archive_WhenAlreadyArchived_ShouldNotRaiseEvent()
    {
        var plan = Plan.Create("Pro", Money.Create(29, "USD"), BillingPeriod.Monthly, Now);
        plan.Archive(Now);
        plan.ClearDomainEvents();
        var version = plan.Version;

        plan.Archive(Now);

        plan.Version.Should().Be(version);
        plan.DomainEvents.Should().NotContain(e => e is PlanArchivedDomainEvent);
    }

    [Fact]
    public void Plan_Deprecate_ShouldRaiseEvent()
    {
        var plan = Plan.Create("Pro", Money.Create(29, "USD"), BillingPeriod.Monthly, Now);
        plan.ClearDomainEvents();
        var version = plan.Version;

        plan.Deprecate(Now);

        plan.Version.Should().Be(version + 1);
        plan.DomainEvents.Should().ContainSingle(e => e is PlanDeprecatedDomainEvent);
    }

    [Fact]
    public void Plan_Deprecate_WhenAlreadyDeprecated_ShouldNotRaiseEvent()
    {
        var plan = Plan.Create("Pro", Money.Create(29, "USD"), BillingPeriod.Monthly, Now);
        plan.Deprecate(Now);
        plan.ClearDomainEvents();
        var version = plan.Version;

        plan.Deprecate(Now);

        plan.Version.Should().Be(version);
        plan.DomainEvents.Should().NotContain(e => e is PlanDeprecatedDomainEvent);
    }

    [Fact]
    public void Plan_SoftDelete_ShouldRaiseEvent()
    {
        var plan = Plan.Create("Pro", Money.Create(29, "USD"), BillingPeriod.Monthly, Now);
        var version = plan.Version;

        plan.SoftDelete(Actor, Now);

        plan.IsDeleted.Should().BeTrue();
        plan.Version.Should().Be(version + 1);
        plan.DomainEvents.Should().ContainSingle(e => e is PlanSoftDeletedDomainEvent);
        var evt = (PlanSoftDeletedDomainEvent)plan.DomainEvents.Single(e => e is PlanSoftDeletedDomainEvent);
        evt.PlanId.Should().Be(plan.Id);
        evt.DeletedBy.Should().Be(Actor);
    }

    [Fact]
    public void Plan_Restore_ShouldRaiseEvent()
    {
        var plan = Plan.Create("Pro", Money.Create(29, "USD"), BillingPeriod.Monthly, Now);
        plan.SoftDelete(Actor, Now);
        plan.ClearDomainEvents();
        var version = plan.Version;

        plan.Restore(Actor, Now);

        plan.IsDeleted.Should().BeFalse();
        plan.Version.Should().Be(version + 1);
        plan.DomainEvents.Should().ContainSingle(e => e is PlanRestoredDomainEvent);
        var evt = (PlanRestoredDomainEvent)plan.DomainEvents.Single(e => e is PlanRestoredDomainEvent);
        evt.PlanId.Should().Be(plan.Id);
        evt.RestoredBy.Should().Be(Actor);
    }

    [Fact]
    public void Plan_SoftDelete_WhenAlreadyDeleted_ShouldNotRaiseEvent()
    {
        var plan = Plan.Create("Pro", Money.Create(29, "USD"), BillingPeriod.Monthly, Now);
        plan.SoftDelete(Actor, Now);
        plan.ClearDomainEvents();
        var version = plan.Version;

        plan.SoftDelete(Actor, Now);

        plan.Version.Should().Be(version);
        plan.DomainEvents.Should().NotContain(e => e is PlanSoftDeletedDomainEvent);
    }

    [Fact]
    public void Plan_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var plan = Plan.Create("Pro", Money.Create(29, "USD"), BillingPeriod.Monthly, Now);
        plan.ClearDomainEvents();
        var version = plan.Version;

        plan.Restore(Actor, Now);

        plan.Version.Should().Be(version);
        plan.DomainEvents.Should().NotContain(e => e is PlanRestoredDomainEvent);
    }
}
