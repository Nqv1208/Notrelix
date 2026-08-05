using FluentAssertions;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Domain.Tests.Governance.Permissions;

public class PermissionRuleLifecycleTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void PermissionRule_Create_ShouldSucceed()
    {
        var rule = PermissionRule.Create(Guid.NewGuid(), WsA, PermissionScopeType.Workspace, ResourceKind.Create("work-management.board"), null, PermissionSubjectType.User, Actor, null, PermissionAction.UpdateItem, PermissionEffect.Allow, Actor, Now);
        rule.Should().NotBeNull();
        rule.WorkspaceId.Should().Be(WsA);
        rule.Status.Should().Be(PermissionRuleStatus.Active);
        rule.DomainEvents.Should().ContainSingle(e => e is PermissionRuleCreatedDomainEvent);
    }

    [Fact]
    public void PermissionRule_Disable_ShouldUpdateStatus()
    {
        var rule = PermissionRule.Create(Guid.NewGuid(), WsA, PermissionScopeType.Workspace, ResourceKind.Create("work-management.board"), null, PermissionSubjectType.User, Actor, null, PermissionAction.UpdateItem, PermissionEffect.Allow, Actor, Now);
        ((IHasDomainEvents)rule).ClearDomainEvents();

        rule.Disable(Actor, Now);

        rule.Status.Should().Be(PermissionRuleStatus.Disabled);
        rule.DomainEvents.Should().ContainSingle(e => e is PermissionRuleDisabledDomainEvent);
    }

    [Fact]
    public void PermissionRule_IsActive_WhenActive_ShouldReturnTrue()
    {
        var rule = PermissionRule.Create(Guid.NewGuid(), WsA, PermissionScopeType.Workspace, ResourceKind.Create("work-management.board"), null, PermissionSubjectType.User, Actor, null, PermissionAction.UpdateItem, PermissionEffect.Allow, Actor, Now);
        rule.IsActive(Now).Should().BeTrue();
    }

    [Fact]
    public void PermissionRule_IsActive_WhenDisabled_ShouldReturnFalse()
    {
        var rule = PermissionRule.Create(Guid.NewGuid(), WsA, PermissionScopeType.Workspace, ResourceKind.Create("work-management.board"), null, PermissionSubjectType.User, Actor, null, PermissionAction.UpdateItem, PermissionEffect.Allow, Actor, Now);
        rule.Disable(Actor, Now);
        rule.IsActive(Now).Should().BeFalse();
    }

    [Fact]
    public void PermissionRule_IsActive_WhenExpired_ShouldReturnFalse()
    {
        var rule = PermissionRule.Create(Guid.NewGuid(), WsA, PermissionScopeType.Workspace, ResourceKind.Create("work-management.board"), null, PermissionSubjectType.User, Actor, null, PermissionAction.UpdateItem, PermissionEffect.Allow, Actor, Now, expiresAt: Now.AddDays(-1));
        rule.IsActive(Now).Should().BeFalse();
    }

    [Fact]
    public void PermissionRule_IsActive_WhenNotYetStarted_ShouldReturnFalse()
    {
        var rule = PermissionRule.Create(Guid.NewGuid(), WsA, PermissionScopeType.Workspace, ResourceKind.Create("work-management.board"), null, PermissionSubjectType.User, Actor, null, PermissionAction.UpdateItem, PermissionEffect.Allow, Actor, Now, startsAt: Now.AddDays(1));
        rule.IsActive(Now).Should().BeFalse();
    }

    [Fact]
    public void PermissionRule_Delete_ShouldRaiseEvent()
    {
        var rule = PermissionRule.Create(Guid.NewGuid(), WsA, PermissionScopeType.Workspace, ResourceKind.Create("work-management.board"), null, PermissionSubjectType.User, Actor, null, PermissionAction.UpdateItem, PermissionEffect.Allow, Actor, Now);
        ((IHasDomainEvents)rule).ClearDomainEvents();
        var version = rule.Version;

        rule.Delete(Actor, Now);

        rule.IsDeleted.Should().BeTrue();
        rule.Version.Should().Be(version + 1);
        rule.DomainEvents.Should().ContainSingle(e => e is PermissionRuleDeletedDomainEvent);
        var evt = (PermissionRuleDeletedDomainEvent)rule.DomainEvents.Single(e => e is PermissionRuleDeletedDomainEvent);
        evt.RuleId.Should().Be(rule.Id);
    }

    [Fact]
    public void PermissionRule_Restore_ShouldRaiseEvent()
    {
        var rule = PermissionRule.Create(Guid.NewGuid(), WsA, PermissionScopeType.Workspace, ResourceKind.Create("work-management.board"), null, PermissionSubjectType.User, Actor, null, PermissionAction.UpdateItem, PermissionEffect.Allow, Actor, Now);
        rule.Delete(Actor, Now);
        ((IHasDomainEvents)rule).ClearDomainEvents();
        var version = rule.Version;

        rule.Restore(Actor, Now);

        rule.IsDeleted.Should().BeFalse();
        rule.Version.Should().Be(version + 1);
        rule.DomainEvents.Should().ContainSingle(e => e is PermissionRuleRestoredDomainEvent);
        var evt = (PermissionRuleRestoredDomainEvent)rule.DomainEvents.Single(e => e is PermissionRuleRestoredDomainEvent);
        evt.RuleId.Should().Be(rule.Id);
    }

    [Fact]
    public void PermissionRule_Delete_WhenAlreadyDeleted_ShouldNotRaiseEvent()
    {
        var rule = PermissionRule.Create(Guid.NewGuid(), WsA, PermissionScopeType.Workspace, ResourceKind.Create("work-management.board"), null, PermissionSubjectType.User, Actor, null, PermissionAction.UpdateItem, PermissionEffect.Allow, Actor, Now);
        rule.Delete(Actor, Now);
        ((IHasDomainEvents)rule).ClearDomainEvents();
        var version = rule.Version;

        rule.Delete(Actor, Now);

        rule.Version.Should().Be(version);
        rule.DomainEvents.Should().NotContain(e => e is PermissionRuleDeletedDomainEvent);
    }

    [Fact]
    public void PermissionRule_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var rule = PermissionRule.Create(Guid.NewGuid(), WsA, PermissionScopeType.Workspace, ResourceKind.Create("work-management.board"), null, PermissionSubjectType.User, Actor, null, PermissionAction.UpdateItem, PermissionEffect.Allow, Actor, Now);
        ((IHasDomainEvents)rule).ClearDomainEvents();
        var version = rule.Version;

        rule.Restore(Actor, Now);

        rule.Version.Should().Be(version);
        rule.DomainEvents.Should().NotContain(e => e is PermissionRuleRestoredDomainEvent);
    }
}
