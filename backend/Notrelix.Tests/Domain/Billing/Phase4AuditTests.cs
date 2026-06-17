using FluentAssertions;
using Notrelix.Domain.Billing.Entitlements;
using Notrelix.Domain.Billing.Entitlements.Events;
using Notrelix.Domain.Billing.Payments;
using Notrelix.Domain.Billing.Payments.Events;
using Notrelix.Domain.Billing.Plans;
using Notrelix.Domain.Billing.Plans.Events;
using Notrelix.Domain.Billing.Subscriptions;
using Notrelix.Domain.Billing.Subscriptions.Events;
using Notrelix.Domain.Billing.Usage;
using Notrelix.Domain.Billing.Usage.Events;
using Notrelix.Domain.SharedKernel;
using Xunit;

namespace Notrelix.Domain.Tests.Billing;

public class Phase4AuditTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    #region Task 17 — Plan events

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
        plan.DomainEvents.Should().ContainSingle(e => e is PlanDescriptionUpdatedEvent);
        var evt = (PlanDescriptionUpdatedEvent)plan.DomainEvents.Single(e => e is PlanDescriptionUpdatedEvent);
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
        plan.DomainEvents.Should().ContainSingle(e => e is PlanArchivedEvent);
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
        plan.DomainEvents.Should().NotContain(e => e is PlanArchivedEvent);
    }

    [Fact]
    public void Plan_Deprecate_ShouldRaiseEvent()
    {
        var plan = Plan.Create("Pro", Money.Create(29, "USD"), BillingPeriod.Monthly, Now);
        plan.ClearDomainEvents();
        var version = plan.Version;

        plan.Deprecate(Now);

        plan.Version.Should().Be(version + 1);
        plan.DomainEvents.Should().ContainSingle(e => e is PlanDeprecatedEvent);
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
        plan.DomainEvents.Should().NotContain(e => e is PlanDeprecatedEvent);
    }

    [Fact]
    public void Plan_SoftDelete_ShouldRaiseEvent()
    {
        var plan = Plan.Create("Pro", Money.Create(29, "USD"), BillingPeriod.Monthly, Now);
        var version = plan.Version;

        plan.SoftDelete(Actor, Now);

        plan.IsDeleted.Should().BeTrue();
        plan.Version.Should().Be(version + 1);
        plan.DomainEvents.Should().ContainSingle(e => e is PlanSoftDeletedEvent);
        var evt = (PlanSoftDeletedEvent)plan.DomainEvents.Single(e => e is PlanSoftDeletedEvent);
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
        plan.DomainEvents.Should().ContainSingle(e => e is PlanRestoredEvent);
        var evt = (PlanRestoredEvent)plan.DomainEvents.Single(e => e is PlanRestoredEvent);
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
        plan.DomainEvents.Should().NotContain(e => e is PlanSoftDeletedEvent);
    }

    [Fact]
    public void Plan_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var plan = Plan.Create("Pro", Money.Create(29, "USD"), BillingPeriod.Monthly, Now);
        plan.ClearDomainEvents();
        var version = plan.Version;

        plan.Restore(Actor, Now);

        plan.Version.Should().Be(version);
        plan.DomainEvents.Should().NotContain(e => e is PlanRestoredEvent);
    }

    #endregion

    #region Task 18 — Subscription events

    [Fact]
    public void Subscription_ScheduleCancellation_ShouldRaiseEvent()
    {
        var sub = Subscription.Create(WsA, Guid.NewGuid(), SubscriptionTier.Pro, Now, Now.AddDays(30), Actor, Now);
        sub.ClearDomainEvents();
        var version = sub.Version;

        sub.ScheduleCancellation(Actor, Now);

        sub.Version.Should().Be(version + 1);
        sub.DomainEvents.Should().ContainSingle(e => e is SubscriptionCancellationScheduledEvent);
        var evt = (SubscriptionCancellationScheduledEvent)sub.DomainEvents.Single(e => e is SubscriptionCancellationScheduledEvent);
        evt.WorkspaceId.Should().Be(WsA);
        evt.SubscriptionId.Should().Be(sub.Id);
        evt.UpdatedBy.Should().Be(Actor);
    }

    [Fact]
    public void Subscription_ScheduleCancellation_WhenAlreadyScheduled_ShouldNotRaiseEvent()
    {
        var sub = Subscription.Create(WsA, Guid.NewGuid(), SubscriptionTier.Pro, Now, Now.AddDays(30), Actor, Now);
        sub.ScheduleCancellation(Actor, Now);
        sub.ClearDomainEvents();
        var version = sub.Version;

        sub.ScheduleCancellation(Actor, Now);

        sub.Version.Should().Be(version);
        sub.DomainEvents.Should().NotContain(e => e is SubscriptionCancellationScheduledEvent);
    }

    [Fact]
    public void Subscription_SoftDelete_ShouldRaiseEvent()
    {
        var sub = Subscription.Create(WsA, Guid.NewGuid(), SubscriptionTier.Pro, Now, Now.AddDays(30), Actor, Now);
        var version = sub.Version;

        sub.SoftDelete(Actor, Now);

        sub.IsDeleted.Should().BeTrue();
        sub.Version.Should().Be(version + 1);
        sub.DomainEvents.Should().ContainSingle(e => e is SubscriptionSoftDeletedEvent);
        var evt = (SubscriptionSoftDeletedEvent)sub.DomainEvents.Single(e => e is SubscriptionSoftDeletedEvent);
        evt.WorkspaceId.Should().Be(WsA);
        evt.SubscriptionId.Should().Be(sub.Id);
        evt.DeletedBy.Should().Be(Actor);
    }

    [Fact]
    public void Subscription_Restore_ShouldRaiseEvent()
    {
        var sub = Subscription.Create(WsA, Guid.NewGuid(), SubscriptionTier.Pro, Now, Now.AddDays(30), Actor, Now);
        sub.SoftDelete(Actor, Now);
        sub.ClearDomainEvents();
        var version = sub.Version;

        sub.Restore(Actor, Now);

        sub.IsDeleted.Should().BeFalse();
        sub.Version.Should().Be(version + 1);
        sub.DomainEvents.Should().ContainSingle(e => e is SubscriptionRestoredEvent);
        var evt = (SubscriptionRestoredEvent)sub.DomainEvents.Single(e => e is SubscriptionRestoredEvent);
        evt.WorkspaceId.Should().Be(WsA);
        evt.RestoredBy.Should().Be(Actor);
    }

    [Fact]
    public void Subscription_SoftDelete_WhenAlreadyDeleted_ShouldNotRaiseEvent()
    {
        var sub = Subscription.Create(WsA, Guid.NewGuid(), SubscriptionTier.Pro, Now, Now.AddDays(30), Actor, Now);
        sub.SoftDelete(Actor, Now);
        sub.ClearDomainEvents();
        var version = sub.Version;

        sub.SoftDelete(Actor, Now);

        sub.Version.Should().Be(version);
        sub.DomainEvents.Should().NotContain(e => e is SubscriptionSoftDeletedEvent);
    }

    [Fact]
    public void Subscription_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var sub = Subscription.Create(WsA, Guid.NewGuid(), SubscriptionTier.Pro, Now, Now.AddDays(30), Actor, Now);
        sub.ClearDomainEvents();
        var version = sub.Version;

        sub.Restore(Actor, Now);

        sub.Version.Should().Be(version);
        sub.DomainEvents.Should().NotContain(e => e is SubscriptionRestoredEvent);
    }

    #endregion

    #region Task 19 — Entitlement events

    [Fact]
    public void Entitlement_Create_ShouldRaiseEvent()
    {
        var feature = FeatureCode.Create("boards");
        var entitlement = Entitlement.Create(WsA, feature, 10, EntitlementSource.Subscription, Now);

        entitlement.DomainEvents.Should().ContainSingle(e => e is EntitlementGrantedDomainEvent);
        var evt = (EntitlementGrantedDomainEvent)entitlement.DomainEvents.Single(e => e is EntitlementGrantedDomainEvent);
        evt.EntitlementId.Should().Be(entitlement.Id);
        evt.FeatureCode.Should().Be("BOARDS");
        evt.Limit.Should().Be(10);
    }

    [Fact]
    public void Entitlement_SoftDelete_ShouldRaiseEvent()
    {
        var feature = FeatureCode.Create("boards");
        var entitlement = Entitlement.Create(WsA, feature, 10, EntitlementSource.Subscription, Now);
        entitlement.ClearDomainEvents();
        var version = entitlement.Version;

        entitlement.SoftDelete(Actor, Now);

        entitlement.IsDeleted.Should().BeTrue();
        entitlement.Version.Should().Be(version + 1);
        entitlement.DomainEvents.Should().ContainSingle(e => e is EntitlementSoftDeletedEvent);
        var evt = (EntitlementSoftDeletedEvent)entitlement.DomainEvents.Single(e => e is EntitlementSoftDeletedEvent);
        evt.WorkspaceId.Should().Be(WsA);
        evt.EntitlementId.Should().Be(entitlement.Id);
        evt.FeatureCode.Should().Be("BOARDS");
    }

    [Fact]
    public void Entitlement_Restore_ShouldRaiseEvent()
    {
        var feature = FeatureCode.Create("boards");
        var entitlement = Entitlement.Create(WsA, feature, 10, EntitlementSource.Subscription, Now);
        entitlement.SoftDelete(Actor, Now);
        entitlement.ClearDomainEvents();
        var version = entitlement.Version;

        entitlement.Restore(Actor, Now);

        entitlement.IsDeleted.Should().BeFalse();
        entitlement.Version.Should().Be(version + 1);
        entitlement.DomainEvents.Should().ContainSingle(e => e is EntitlementRestoredEvent);
        var evt = (EntitlementRestoredEvent)entitlement.DomainEvents.Single(e => e is EntitlementRestoredEvent);
        evt.WorkspaceId.Should().Be(WsA);
        evt.EntitlementId.Should().Be(entitlement.Id);
    }

    [Fact]
    public void Entitlement_SoftDelete_WhenAlreadyDeleted_ShouldNotRaiseEvent()
    {
        var feature = FeatureCode.Create("boards");
        var entitlement = Entitlement.Create(WsA, feature, 10, EntitlementSource.Subscription, Now);
        entitlement.SoftDelete(Actor, Now);
        entitlement.ClearDomainEvents();
        var version = entitlement.Version;

        entitlement.SoftDelete(Actor, Now);

        entitlement.Version.Should().Be(version);
        entitlement.DomainEvents.Should().NotContain(e => e is EntitlementSoftDeletedEvent);
    }

    [Fact]
    public void Entitlement_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var feature = FeatureCode.Create("boards");
        var entitlement = Entitlement.Create(WsA, feature, 10, EntitlementSource.Subscription, Now);
        entitlement.ClearDomainEvents();
        var version = entitlement.Version;

        entitlement.Restore(Actor, Now);

        entitlement.Version.Should().Be(version);
        entitlement.DomainEvents.Should().NotContain(e => e is EntitlementRestoredEvent);
    }

    #endregion

    #region Task 20 — WorkspaceFeatureUsage events

    [Fact]
    public void WorkspaceFeatureUsage_Create_ShouldRaiseEvent()
    {
        var feature = FeatureCode.Create("storage");
        var usage = WorkspaceFeatureUsage.Create(WsA, feature, 0, 100, null, Now);

        usage.DomainEvents.Should().ContainSingle(e => e is WorkspaceFeatureUsageInitializedEvent);
        var evt = (WorkspaceFeatureUsageInitializedEvent)usage.DomainEvents.Single(e => e is WorkspaceFeatureUsageInitializedEvent);
        evt.WorkspaceId.Should().Be(WsA);
        evt.CurrentUsage.Should().Be(0);
        evt.HardLimit.Should().Be(100);
    }

    [Fact]
    public void WorkspaceFeatureUsage_Reset_ShouldRaiseEvent()
    {
        var feature = FeatureCode.Create("storage");
        var usage = WorkspaceFeatureUsage.Create(WsA, feature, 50, 100, null, Now);
        usage.ClearDomainEvents();
        var version = usage.Version;

        usage.Reset(Now, Actor);

        usage.Version.Should().Be(version + 1);
        usage.DomainEvents.Should().ContainSingle(e => e is WorkspaceFeatureUsageResetEvent);
        var evt = (WorkspaceFeatureUsageResetEvent)usage.DomainEvents.Single(e => e is WorkspaceFeatureUsageResetEvent);
        evt.WorkspaceId.Should().Be(WsA);
    }

    [Fact]
    public void WorkspaceFeatureUsage_SoftDelete_ShouldRaiseEvent()
    {
        var feature = FeatureCode.Create("storage");
        var usage = WorkspaceFeatureUsage.Create(WsA, feature, 0, 100, null, Now);
        usage.ClearDomainEvents();
        var version = usage.Version;

        usage.SoftDelete(Actor, Now);

        usage.IsDeleted.Should().BeTrue();
        usage.Version.Should().Be(version + 1);
        usage.DomainEvents.Should().ContainSingle(e => e is WorkspaceFeatureUsageSoftDeletedEvent);
        var evt = (WorkspaceFeatureUsageSoftDeletedEvent)usage.DomainEvents.Single(e => e is WorkspaceFeatureUsageSoftDeletedEvent);
        evt.WorkspaceId.Should().Be(WsA);
        evt.DeletedBy.Should().Be(Actor);
    }

    [Fact]
    public void WorkspaceFeatureUsage_Restore_ShouldRaiseEvent()
    {
        var feature = FeatureCode.Create("storage");
        var usage = WorkspaceFeatureUsage.Create(WsA, feature, 0, 100, null, Now);
        usage.SoftDelete(Actor, Now);
        usage.ClearDomainEvents();
        var version = usage.Version;

        usage.Restore(Actor, Now);

        usage.IsDeleted.Should().BeFalse();
        usage.Version.Should().Be(version + 1);
        usage.DomainEvents.Should().ContainSingle(e => e is WorkspaceFeatureUsageRestoredEvent);
        var evt = (WorkspaceFeatureUsageRestoredEvent)usage.DomainEvents.Single(e => e is WorkspaceFeatureUsageRestoredEvent);
        evt.WorkspaceId.Should().Be(WsA);
        evt.RestoredBy.Should().Be(Actor);
    }

    [Fact]
    public void WorkspaceFeatureUsage_SoftDelete_WhenAlreadyDeleted_ShouldNotRaiseEvent()
    {
        var feature = FeatureCode.Create("storage");
        var usage = WorkspaceFeatureUsage.Create(WsA, feature, 0, 100, null, Now);
        usage.SoftDelete(Actor, Now);
        usage.ClearDomainEvents();
        var version = usage.Version;

        usage.SoftDelete(Actor, Now);

        usage.Version.Should().Be(version);
        usage.DomainEvents.Should().NotContain(e => e is WorkspaceFeatureUsageSoftDeletedEvent);
    }

    [Fact]
    public void WorkspaceFeatureUsage_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var feature = FeatureCode.Create("storage");
        var usage = WorkspaceFeatureUsage.Create(WsA, feature, 0, 100, null, Now);
        usage.ClearDomainEvents();
        var version = usage.Version;

        usage.Restore(Actor, Now);

        usage.Version.Should().Be(version);
        usage.DomainEvents.Should().NotContain(e => e is WorkspaceFeatureUsageRestoredEvent);
    }

    #endregion

    #region Task 21 — UsageMetric events

    [Fact]
    public void UsageMetric_Create_ShouldRaiseEvent()
    {
        var metric = UsageMetric.Create(WsA, UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(Now, Now.AddDays(30)), Now);

        metric.DomainEvents.Should().ContainSingle(e => e is UsageMetricCreatedEvent);
        var evt = (UsageMetricCreatedEvent)metric.DomainEvents.Single(e => e is UsageMetricCreatedEvent);
        evt.WorkspaceId.Should().Be(WsA);
        evt.Key.Should().Be(metric.Key);
    }

    [Fact]
    public void UsageMetric_Decrease_ShouldRaiseEvent()
    {
        var metric = UsageMetric.Create(WsA, UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(Now, Now.AddDays(30)), Now);
        metric.ClearDomainEvents();
        metric.Increase(5, 10, isHardLimit: true, Now);
        metric.ClearDomainEvents();
        var version = metric.Version;

        metric.Decrease(2, Now);

        metric.Version.Should().Be(version + 1);
        metric.DomainEvents.Should().ContainSingle(e => e is UsageMetricDecreasedEvent);
        var evt = (UsageMetricDecreasedEvent)metric.DomainEvents.Single(e => e is UsageMetricDecreasedEvent);
        evt.WorkspaceId.Should().Be(WsA);
        evt.Amount.Should().Be(2);
    }

    [Fact]
    public void UsageMetric_Reset_ShouldRaiseEvent()
    {
        var metric = UsageMetric.Create(WsA, UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(Now, Now.AddDays(30)), Now);
        metric.Increase(5, 10, isHardLimit: true, Now);
        metric.ClearDomainEvents();
        var version = metric.Version;

        metric.Reset(UsagePeriod.Create(Now.AddDays(30), Now.AddDays(60)), Now);

        metric.Version.Should().Be(version + 1);
        metric.DomainEvents.Should().ContainSingle(e => e is UsageMetricResetEvent);
        var evt = (UsageMetricResetEvent)metric.DomainEvents.Single(e => e is UsageMetricResetEvent);
        evt.WorkspaceId.Should().Be(WsA);
    }

    [Fact]
    public void UsageMetric_SoftDelete_ShouldRaiseEvent()
    {
        var metric = UsageMetric.Create(WsA, UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(Now, Now.AddDays(30)), Now);
        metric.ClearDomainEvents();
        var version = metric.Version;

        metric.SoftDelete(Actor, Now);

        metric.IsDeleted.Should().BeTrue();
        metric.Version.Should().Be(version + 1);
        metric.DomainEvents.Should().ContainSingle(e => e is UsageMetricSoftDeletedEvent);
        var evt = (UsageMetricSoftDeletedEvent)metric.DomainEvents.Single(e => e is UsageMetricSoftDeletedEvent);
        evt.WorkspaceId.Should().Be(WsA);
        evt.DeletedBy.Should().Be(Actor);
    }

    [Fact]
    public void UsageMetric_Restore_ShouldRaiseEvent()
    {
        var metric = UsageMetric.Create(WsA, UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(Now, Now.AddDays(30)), Now);
        metric.SoftDelete(Actor, Now);
        metric.ClearDomainEvents();
        var version = metric.Version;

        metric.Restore(Actor, Now);

        metric.IsDeleted.Should().BeFalse();
        metric.Version.Should().Be(version + 1);
        metric.DomainEvents.Should().ContainSingle(e => e is UsageMetricRestoredEvent);
        var evt = (UsageMetricRestoredEvent)metric.DomainEvents.Single(e => e is UsageMetricRestoredEvent);
        evt.WorkspaceId.Should().Be(WsA);
        evt.RestoredBy.Should().Be(Actor);
    }

    [Fact]
    public void UsageMetric_SoftDelete_WhenAlreadyDeleted_ShouldNotRaiseEvent()
    {
        var metric = UsageMetric.Create(WsA, UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(Now, Now.AddDays(30)), Now);
        metric.SoftDelete(Actor, Now);
        metric.ClearDomainEvents();
        var version = metric.Version;

        metric.SoftDelete(Actor, Now);

        metric.Version.Should().Be(version);
        metric.DomainEvents.Should().NotContain(e => e is UsageMetricSoftDeletedEvent);
    }

    [Fact]
    public void UsageMetric_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var metric = UsageMetric.Create(WsA, UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(Now, Now.AddDays(30)), Now);
        metric.ClearDomainEvents();
        var version = metric.Version;

        metric.Restore(Actor, Now);

        metric.Version.Should().Be(version);
        metric.DomainEvents.Should().NotContain(e => e is UsageMetricRestoredEvent);
    }

    #endregion

    #region Task 22 — Invoice events

    [Fact]
    public void Invoice_Create_ShouldRaiseEvent()
    {
        var invoice = Invoice.Create(WsA, Guid.NewGuid(), "INV-001", Money.Create(100, "USD"), Now.AddDays(30), Now);

        invoice.DomainEvents.Should().ContainSingle(e => e is InvoiceCreatedEvent);
        var evt = (InvoiceCreatedEvent)invoice.DomainEvents.Single(e => e is InvoiceCreatedEvent);
        evt.InvoiceId.Should().Be(invoice.Id);
        evt.WorkspaceId.Should().Be(WsA);
        evt.Amount.Should().Be(Money.Create(100, "USD"));
    }

    [Fact]
    public void Invoice_Void_ShouldRaiseEvent()
    {
        var invoice = Invoice.Create(WsA, Guid.NewGuid(), "INV-001", Money.Create(100, "USD"), Now.AddDays(30), Now);
        invoice.ClearDomainEvents();
        var version = invoice.Version;

        invoice.Void(Now);

        invoice.Version.Should().Be(version + 1);
        invoice.DomainEvents.Should().ContainSingle(e => e is InvoiceVoidedEvent);
        var evt = (InvoiceVoidedEvent)invoice.DomainEvents.Single(e => e is InvoiceVoidedEvent);
        evt.InvoiceId.Should().Be(invoice.Id);
        evt.WorkspaceId.Should().Be(WsA);
    }

    [Fact]
    public void Invoice_Void_WhenAlreadyVoided_ShouldNotRaiseEvent()
    {
        var invoice = Invoice.Create(WsA, Guid.NewGuid(), "INV-001", Money.Create(100, "USD"), Now.AddDays(30), Now);
        invoice.Void(Now);
        invoice.ClearDomainEvents();
        var version = invoice.Version;

        invoice.Void(Now);

        invoice.Version.Should().Be(version);
        invoice.DomainEvents.Should().NotContain(e => e is InvoiceVoidedEvent);
    }

    [Fact]
    public void Invoice_SoftDelete_ShouldRaiseEvent()
    {
        var invoice = Invoice.Create(WsA, Guid.NewGuid(), "INV-001", Money.Create(100, "USD"), Now.AddDays(30), Now);
        invoice.ClearDomainEvents();
        var version = invoice.Version;

        invoice.SoftDelete(Actor, Now);

        invoice.IsDeleted.Should().BeTrue();
        invoice.Version.Should().Be(version + 1);
        invoice.DomainEvents.Should().ContainSingle(e => e is InvoiceSoftDeletedEvent);
        var evt = (InvoiceSoftDeletedEvent)invoice.DomainEvents.Single(e => e is InvoiceSoftDeletedEvent);
        evt.WorkspaceId.Should().Be(WsA);
        evt.InvoiceId.Should().Be(invoice.Id);
        evt.DeletedBy.Should().Be(Actor);
    }

    [Fact]
    public void Invoice_Restore_ShouldRaiseEvent()
    {
        var invoice = Invoice.Create(WsA, Guid.NewGuid(), "INV-001", Money.Create(100, "USD"), Now.AddDays(30), Now);
        invoice.SoftDelete(Actor, Now);
        invoice.ClearDomainEvents();
        var version = invoice.Version;

        invoice.Restore(Actor, Now);

        invoice.IsDeleted.Should().BeFalse();
        invoice.Version.Should().Be(version + 1);
        invoice.DomainEvents.Should().ContainSingle(e => e is InvoiceRestoredEvent);
        var evt = (InvoiceRestoredEvent)invoice.DomainEvents.Single(e => e is InvoiceRestoredEvent);
        evt.WorkspaceId.Should().Be(WsA);
        evt.InvoiceId.Should().Be(invoice.Id);
        evt.RestoredBy.Should().Be(Actor);
    }

    [Fact]
    public void Invoice_SoftDelete_WhenAlreadyDeleted_ShouldNotRaiseEvent()
    {
        var invoice = Invoice.Create(WsA, Guid.NewGuid(), "INV-001", Money.Create(100, "USD"), Now.AddDays(30), Now);
        invoice.SoftDelete(Actor, Now);
        invoice.ClearDomainEvents();
        var version = invoice.Version;

        invoice.SoftDelete(Actor, Now);

        invoice.Version.Should().Be(version);
        invoice.DomainEvents.Should().NotContain(e => e is InvoiceSoftDeletedEvent);
    }

    [Fact]
    public void Invoice_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var invoice = Invoice.Create(WsA, Guid.NewGuid(), "INV-001", Money.Create(100, "USD"), Now.AddDays(30), Now);
        invoice.ClearDomainEvents();
        var version = invoice.Version;

        invoice.Restore(Actor, Now);

        invoice.Version.Should().Be(version);
        invoice.DomainEvents.Should().NotContain(e => e is InvoiceRestoredEvent);
    }

    #endregion

    #region Task 23 — PaymentMethod events

    [Fact]
    public void PaymentMethod_Create_ShouldRaiseEvent()
    {
        var method = PaymentMethod.Create(WsA, PaymentProvider.Stripe, "pm_123", "4242", "Visa", Actor, Now);

        method.DomainEvents.Should().ContainSingle(e => e is PaymentMethodAddedEvent);
        var evt = (PaymentMethodAddedEvent)method.DomainEvents.Single(e => e is PaymentMethodAddedEvent);
        evt.WorkspaceId.Should().Be(WsA);
        evt.PaymentMethodId.Should().Be(method.Id);
        evt.Provider.Should().Be(PaymentProvider.Stripe);
        evt.Last4.Should().Be("4242");
        evt.Brand.Should().Be("Visa");
    }

    [Fact]
    public void PaymentMethod_SoftDelete_ShouldIncrementVersion()
    {
        var method = PaymentMethod.Create(WsA, PaymentProvider.Stripe, "pm_123", "4242", "Visa", Actor, Now);
        method.ClearDomainEvents();
        var version = method.Version;

        method.SoftDelete(Actor, Now);

        method.IsDeleted.Should().BeTrue();
        method.Version.Should().Be(version + 1);
    }

    [Fact]
    public void PaymentMethod_Restore_ShouldIncrementVersion()
    {
        var method = PaymentMethod.Create(WsA, PaymentProvider.Stripe, "pm_123", "4242", "Visa", Actor, Now);
        method.SoftDelete(Actor, Now);
        method.ClearDomainEvents();
        var version = method.Version;

        method.Restore(Actor, Now);

        method.IsDeleted.Should().BeFalse();
        method.Version.Should().Be(version + 1);
    }

    [Fact]
    public void PaymentMethod_SoftDelete_WhenAlreadyDeleted_ShouldNotIncrement()
    {
        var method = PaymentMethod.Create(WsA, PaymentProvider.Stripe, "pm_123", "4242", "Visa", Actor, Now);
        method.SoftDelete(Actor, Now);
        method.ClearDomainEvents();
        var version = method.Version;

        method.SoftDelete(Actor, Now);

        method.Version.Should().Be(version);
    }

    [Fact]
    public void PaymentMethod_Restore_WhenNotDeleted_ShouldNotIncrement()
    {
        var method = PaymentMethod.Create(WsA, PaymentProvider.Stripe, "pm_123", "4242", "Visa", Actor, Now);
        method.ClearDomainEvents();
        var version = method.Version;

        method.Restore(Actor, Now);

        method.Version.Should().Be(version);
    }

    #endregion
}
