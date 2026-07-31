using FluentAssertions;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Automation.RulesEngine;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Automation.Rules;

/// <summary>
/// Tests for AutomationRule deletion lifecycle.
/// Invariant: Delete/Restore preserve AutomationRule.Status.
/// </summary>
[CoversAggregate(typeof(AutomationRule))]
public class AutomationRuleDeletionLifecycleTests
{
    private static AutomationConfiguration CreateValidConfig()
    {
        var trigger = AutomationTriggerDefinition.Create("ItemCreated");
        var action = AutomationActionDefinition.Create("Webhook", """{"url":"https://example.com/webhook"}""");
        return AutomationConfiguration.Create(trigger, action);
    }

    private static AutomationRule CreateRule(AutomationRuleStatus targetStatus)
    {
        var rule = AutomationRule.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Test Rule",
            CreateValidConfig(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        return targetStatus switch
        {
            AutomationRuleStatus.Draft => rule,
            AutomationRuleStatus.Active => EnableRule(rule),
            AutomationRuleStatus.Disabled => DisableRule(rule),
            _ => rule
        };
    }

    private static AutomationRule EnableRule(AutomationRule rule)
    {
        rule.Enable(Guid.NewGuid(), DateTimeOffset.UtcNow);
        return rule;
    }

    private static AutomationRule DisableRule(AutomationRule rule)
    {
        rule.Enable(Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.Disable(Guid.NewGuid(), DateTimeOffset.UtcNow);
        return rule;
    }

    // ── Delete preserves status ───────────────────────────────────────────

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.Delete), MutationScenario.Valid, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Delete_Draft_PreservesDraft()
    {
        var rule = CreateRule(AutomationRuleStatus.Draft);

        rule.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.Status.Should().Be(AutomationRuleStatus.Draft);
        rule.IsDeleted.Should().BeTrue();
    }

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.Delete), MutationScenario.Valid, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Delete_Active_PreservesActive()
    {
        var rule = CreateRule(AutomationRuleStatus.Active);

        rule.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.Status.Should().Be(AutomationRuleStatus.Active);
        rule.IsDeleted.Should().BeTrue();
    }

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.Delete), MutationScenario.Valid, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Delete_Disabled_PreservesDisabled()
    {
        var rule = CreateRule(AutomationRuleStatus.Disabled);

        rule.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.Status.Should().Be(AutomationRuleStatus.Disabled);
        rule.IsDeleted.Should().BeTrue();
    }

    // ── Restore preserves status ──────────────────────────────────────────

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.Restore), MutationScenario.Valid, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Restore_Draft_PreservesDraft()
    {
        var rule = CreateRule(AutomationRuleStatus.Draft);
        rule.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.Status.Should().Be(AutomationRuleStatus.Draft);
        rule.IsDeleted.Should().BeFalse();
    }

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.Restore), MutationScenario.Valid, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Restore_Active_PreservesActive()
    {
        var rule = CreateRule(AutomationRuleStatus.Active);
        rule.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.Status.Should().Be(AutomationRuleStatus.Active);
        rule.IsDeleted.Should().BeFalse();
    }

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.Restore), MutationScenario.Valid, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Restore_Disabled_PreservesDisabled()
    {
        var rule = CreateRule(AutomationRuleStatus.Disabled);
        rule.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.Status.Should().Be(AutomationRuleStatus.Disabled);
        rule.IsDeleted.Should().BeFalse();
    }

    // ── No-op behavior ────────────────────────────────────────────────────

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.Delete), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Delete_AlreadyDeleted_IsNoOp()
    {
        var rule = CreateRule(AutomationRuleStatus.Draft);
        rule.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)rule).ClearDomainEvents();
        var versionBefore = rule.Version;

        rule.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.Version.Should().Be(versionBefore);
        rule.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.Restore), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Restore_NotDeleted_IsNoOp()
    {
        var rule = CreateRule(AutomationRuleStatus.Draft);
        ((IHasDomainEvents)rule).ClearDomainEvents();
        var versionBefore = rule.Version;

        rule.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.Version.Should().Be(versionBefore);
        rule.DomainEvents.Should().BeEmpty();
    }

    // ── Version increments ────────────────────────────────────────────────

    [Fact]
    public void Delete_IncrementsVersionOnce()
    {
        var rule = CreateRule(AutomationRuleStatus.Draft);
        var versionBefore = rule.Version;

        rule.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.Version.Should().Be(versionBefore + 1);
    }

    [Fact]
    public void Restore_IncrementsVersionOnce()
    {
        var rule = CreateRule(AutomationRuleStatus.Draft);
        rule.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var versionBefore = rule.Version;

        rule.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.Version.Should().Be(versionBefore + 1);
    }

    // ── Event contracts ───────────────────────────────────────────────────

    [Fact]
    public void Delete_RaisesDeletedEventOnly()
    {
        var rule = CreateRule(AutomationRuleStatus.Active);
        ((IHasDomainEvents)rule).ClearDomainEvents();

        rule.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.DomainEvents.Should().ContainSingle(e => e is AutomationRuleDeletedDomainEvent);
        rule.DomainEvents.Should().NotContain(e => e is AutomationRuleDisabledDomainEvent);
    }

    [Fact]
    public void Restore_RaisesRestoredEventOnly()
    {
        var rule = CreateRule(AutomationRuleStatus.Active);
        rule.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)rule).ClearDomainEvents();

        rule.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.DomainEvents.Should().ContainSingle(e => e is AutomationRuleRestoredDomainEvent);
        rule.DomainEvents.Should().NotContain(e => e is AutomationRuleEnabledDomainEvent);
    }

    // ── Failure atomicity ─────────────────────────────────────────────────

    [Fact]
    public void Delete_WhenDeleted_ShouldBeFailureAtomic()
    {
        var rule = CreateRule(AutomationRuleStatus.Active);
        rule.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var statusBefore = rule.Status;
        var isDeletedBefore = rule.IsDeleted;
        var deletedAtBefore = rule.DeletedAt;
        var deletedByBefore = rule.DeletedBy;
        var deleteReasonBefore = rule.DeleteReason;
        var updatedAtBefore = rule.UpdatedAt;
        var updatedByBefore = rule.UpdatedBy;
        var versionBefore = rule.Version;
        var eventsBefore = rule.DomainEvents.Count;

        rule.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.Status.Should().Be(statusBefore);
        rule.IsDeleted.Should().Be(isDeletedBefore);
        rule.DeletedAt.Should().Be(deletedAtBefore);
        rule.DeletedBy.Should().Be(deletedByBefore);
        rule.DeleteReason.Should().Be(deleteReasonBefore);
        rule.UpdatedAt.Should().Be(updatedAtBefore);
        rule.UpdatedBy.Should().Be(updatedByBefore);
        rule.Version.Should().Be(versionBefore);
        rule.DomainEvents.Count.Should().Be(eventsBefore);
    }

    [Fact]
    public void Restore_WhenNotDeleted_ShouldBeFailureAtomic()
    {
        var rule = CreateRule(AutomationRuleStatus.Active);

        var statusBefore = rule.Status;
        var isDeletedBefore = rule.IsDeleted;
        var updatedAtBefore = rule.UpdatedAt;
        var updatedByBefore = rule.UpdatedBy;
        var versionBefore = rule.Version;
        var eventsBefore = rule.DomainEvents.Count;

        rule.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.Status.Should().Be(statusBefore);
        rule.IsDeleted.Should().Be(isDeletedBefore);
        rule.UpdatedAt.Should().Be(updatedAtBefore);
        rule.UpdatedBy.Should().Be(updatedByBefore);
        rule.Version.Should().Be(versionBefore);
        rule.DomainEvents.Count.Should().Be(eventsBefore);
    }
}
