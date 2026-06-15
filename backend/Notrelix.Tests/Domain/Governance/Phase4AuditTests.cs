using FluentAssertions;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Governance.Permissions.Events;
using Notrelix.Domain.Governance.Roles;
using Notrelix.Domain.Governance.Roles.Events;
using Notrelix.Domain.Governance.ShareLinks;
using Notrelix.Domain.Governance.ShareLinks.Events;

namespace Notrelix.Domain.Tests.Governance;

public class Phase4AuditTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    #region Phase 4a — PermissionRule source of truth

    [Fact]
    public void PermissionRule_Create_ShouldSucceed()
    {
        var rule = PermissionRule.Create(WsA, PermissionScopeType.Workspace, ResourceType.Board, null, PermissionSubjectType.User, Actor, null, PermissionAction.UpdateItem, PermissionEffect.Allow, Actor, Now);
        rule.Should().NotBeNull();
        rule.WorkspaceId.Should().Be(WsA);
        rule.Status.Should().Be(PermissionRuleStatus.Active);
        rule.DomainEvents.Should().ContainSingle(e => e is PermissionRuleCreatedDomainEvent);
    }

    [Fact]
    public void PermissionRule_Disable_ShouldUpdateStatus()
    {
        var rule = PermissionRule.Create(WsA, PermissionScopeType.Workspace, ResourceType.Board, null, PermissionSubjectType.User, Actor, null, PermissionAction.UpdateItem, PermissionEffect.Allow, Actor, Now);
        rule.ClearDomainEvents();

        rule.Disable(Actor, Now);

        rule.Status.Should().Be(PermissionRuleStatus.Disabled);
        rule.DomainEvents.Should().ContainSingle(e => e is PermissionRuleDisabledDomainEvent);
    }

    [Fact]
    public void PermissionRule_IsActive_WhenActive_ShouldReturnTrue()
    {
        var rule = PermissionRule.Create(WsA, PermissionScopeType.Workspace, ResourceType.Board, null, PermissionSubjectType.User, Actor, null, PermissionAction.UpdateItem, PermissionEffect.Allow, Actor, Now);
        rule.IsActive(Now).Should().BeTrue();
    }

    [Fact]
    public void PermissionRule_IsActive_WhenDisabled_ShouldReturnFalse()
    {
        var rule = PermissionRule.Create(WsA, PermissionScopeType.Workspace, ResourceType.Board, null, PermissionSubjectType.User, Actor, null, PermissionAction.UpdateItem, PermissionEffect.Allow, Actor, Now);
        rule.Disable(Actor, Now);
        rule.IsActive(Now).Should().BeFalse();
    }

    [Fact]
    public void PermissionRule_IsActive_WhenExpired_ShouldReturnFalse()
    {
        var rule = PermissionRule.Create(WsA, PermissionScopeType.Workspace, ResourceType.Board, null, PermissionSubjectType.User, Actor, null, PermissionAction.UpdateItem, PermissionEffect.Allow, Actor, Now, expiresAt: Now.AddDays(-1));
        rule.IsActive(Now).Should().BeFalse();
    }

    [Fact]
    public void PermissionRule_IsActive_WhenNotYetStarted_ShouldReturnFalse()
    {
        var rule = PermissionRule.Create(WsA, PermissionScopeType.Workspace, ResourceType.Board, null, PermissionSubjectType.User, Actor, null, PermissionAction.UpdateItem, PermissionEffect.Allow, Actor, Now, startsAt: Now.AddDays(1));
        rule.IsActive(Now).Should().BeFalse();
    }

    #endregion

    #region Phase 4c — ShareLink hardening

    [Fact]
    public void ShareLink_Create_WithPublicAccessAndNoExpiry_ShouldThrow()
    {
        var tokenHash = ShareLinkTokenHash.Create("test-hash");
        var act = () => ShareLink.Create(WsA, ResourceType.Board, Guid.NewGuid(), tokenHash, ShareLinkAccessMode.Public, Actor, Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*expiration*");
    }

    [Fact]
    public void ShareLink_Create_WithPublicAccessAndExpiry_ShouldSucceed()
    {
        var tokenHash = ShareLinkTokenHash.Create("test-hash");
        var link = ShareLink.Create(WsA, ResourceType.Board, Guid.NewGuid(), tokenHash, ShareLinkAccessMode.Public, Actor, Now, Now.AddDays(7));
        link.Should().NotBeNull();
        link.AccessMode.Should().Be(ShareLinkAccessMode.Public);
        link.ExpiresAt.Should().Be(Now.AddDays(7));
    }

    [Fact]
    public void ShareLink_Create_WithWorkspaceOnlyAccess_ShouldAllowNoExpiry()
    {
        var tokenHash = ShareLinkTokenHash.Create("test-hash");
        var link = ShareLink.Create(WsA, ResourceType.Board, Guid.NewGuid(), tokenHash, ShareLinkAccessMode.WorkspaceOnly, Actor, Now);
        link.Should().NotBeNull();
        link.ExpiresAt.Should().BeNull();
    }

    [Fact]
    public void ShareLink_Expire_ShouldUseNullActor()
    {
        var tokenHash = ShareLinkTokenHash.Create("test-hash");
        var link = ShareLink.Create(WsA, ResourceType.Board, Guid.NewGuid(), tokenHash, ShareLinkAccessMode.WorkspaceOnly, Actor, Now);
        link.Expire(Now);
        link.Status.Should().Be(ShareLinkStatus.Expired);
    }

    [Fact]
    public void ShareLink_IsExpired_WhenExpired_ShouldReturnTrue()
    {
        var tokenHash = ShareLinkTokenHash.Create("test-hash");
        var link = ShareLink.Create(WsA, ResourceType.Board, Guid.NewGuid(), tokenHash, ShareLinkAccessMode.WorkspaceOnly, Actor, Now);
        link.Expire(Now);
        link.IsExpired(Now).Should().BeTrue();
    }

    [Fact]
    public void ShareLink_IsExpired_WhenPastExpiry_ShouldReturnTrue()
    {
        var tokenHash = ShareLinkTokenHash.Create("test-hash");
        var link = ShareLink.Create(WsA, ResourceType.Board, Guid.NewGuid(), tokenHash, ShareLinkAccessMode.WorkspaceOnly, Actor, Now, Now.AddDays(-1));
        link.IsExpired(Now).Should().BeTrue();
    }

    #endregion

    #region Task 24 — PermissionRule SoftDelete/Restore

    [Fact]
    public void PermissionRule_SoftDelete_ShouldRaiseEvent()
    {
        var rule = PermissionRule.Create(WsA, PermissionScopeType.Workspace, ResourceType.Board, null, PermissionSubjectType.User, Actor, null, PermissionAction.UpdateItem, PermissionEffect.Allow, Actor, Now);
        rule.ClearDomainEvents();
        var version = rule.Version;

        rule.SoftDelete(Actor, Now);

        rule.IsDeleted.Should().BeTrue();
        rule.Version.Should().Be(version + 1);
        rule.DomainEvents.Should().ContainSingle(e => e is PermissionRuleSoftDeletedDomainEvent);
        var evt = (PermissionRuleSoftDeletedDomainEvent)rule.DomainEvents.Single(e => e is PermissionRuleSoftDeletedDomainEvent);
        evt.RuleId.Should().Be(rule.Id);
    }

    [Fact]
    public void PermissionRule_Restore_ShouldRaiseEvent()
    {
        var rule = PermissionRule.Create(WsA, PermissionScopeType.Workspace, ResourceType.Board, null, PermissionSubjectType.User, Actor, null, PermissionAction.UpdateItem, PermissionEffect.Allow, Actor, Now);
        rule.SoftDelete(Actor, Now);
        rule.ClearDomainEvents();
        var version = rule.Version;

        rule.Restore(Actor, Now);

        rule.IsDeleted.Should().BeFalse();
        rule.Version.Should().Be(version + 1);
        rule.DomainEvents.Should().ContainSingle(e => e is PermissionRuleRestoredDomainEvent);
        var evt = (PermissionRuleRestoredDomainEvent)rule.DomainEvents.Single(e => e is PermissionRuleRestoredDomainEvent);
        evt.RuleId.Should().Be(rule.Id);
    }

    [Fact]
    public void PermissionRule_SoftDelete_WhenAlreadyDeleted_ShouldNotRaiseEvent()
    {
        var rule = PermissionRule.Create(WsA, PermissionScopeType.Workspace, ResourceType.Board, null, PermissionSubjectType.User, Actor, null, PermissionAction.UpdateItem, PermissionEffect.Allow, Actor, Now);
        rule.SoftDelete(Actor, Now);
        rule.ClearDomainEvents();
        var version = rule.Version;

        rule.SoftDelete(Actor, Now);

        rule.Version.Should().Be(version);
        rule.DomainEvents.Should().NotContain(e => e is PermissionRuleSoftDeletedDomainEvent);
    }

    [Fact]
    public void PermissionRule_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var rule = PermissionRule.Create(WsA, PermissionScopeType.Workspace, ResourceType.Board, null, PermissionSubjectType.User, Actor, null, PermissionAction.UpdateItem, PermissionEffect.Allow, Actor, Now);
        rule.ClearDomainEvents();
        var version = rule.Version;

        rule.Restore(Actor, Now);

        rule.Version.Should().Be(version);
        rule.DomainEvents.Should().NotContain(e => e is PermissionRuleRestoredDomainEvent);
    }

    #endregion

    #region Task 25 — ResourcePermission SoftDelete/Restore

    [Fact]
    public void ResourcePermission_SoftDelete_ShouldRaiseEvent()
    {
        var permission = ResourcePermission.Grant(WsA, ResourceType.Board, Guid.NewGuid(), PermissionSubjectType.User, Actor, PermissionLevel.Editor, Actor, Now);
        permission.ClearDomainEvents();
        var version = permission.Version;

        permission.SoftDelete(Actor, Now);

        permission.IsDeleted.Should().BeTrue();
        permission.Version.Should().Be(version + 1);
        permission.DomainEvents.Should().ContainSingle(e => e is ResourcePermissionSoftDeletedEvent);
        var evt = (ResourcePermissionSoftDeletedEvent)permission.DomainEvents.Single(e => e is ResourcePermissionSoftDeletedEvent);
        evt.PermissionId.Should().Be(permission.Id);
        evt.DeletedBy.Should().Be(Actor);
    }

    [Fact]
    public void ResourcePermission_Restore_ShouldRaiseEvent()
    {
        var permission = ResourcePermission.Grant(WsA, ResourceType.Board, Guid.NewGuid(), PermissionSubjectType.User, Actor, PermissionLevel.Editor, Actor, Now);
        permission.SoftDelete(Actor, Now);
        permission.ClearDomainEvents();
        var version = permission.Version;

        permission.Restore(Actor, Now);

        permission.IsDeleted.Should().BeFalse();
        permission.Version.Should().Be(version + 1);
        permission.DomainEvents.Should().ContainSingle(e => e is ResourcePermissionRestoredEvent);
        var evt = (ResourcePermissionRestoredEvent)permission.DomainEvents.Single(e => e is ResourcePermissionRestoredEvent);
        evt.PermissionId.Should().Be(permission.Id);
        evt.RestoredBy.Should().Be(Actor);
    }

    [Fact]
    public void ResourcePermission_SoftDelete_WhenAlreadyDeleted_ShouldNotRaiseEvent()
    {
        var permission = ResourcePermission.Grant(WsA, ResourceType.Board, Guid.NewGuid(), PermissionSubjectType.User, Actor, PermissionLevel.Editor, Actor, Now);
        permission.SoftDelete(Actor, Now);
        permission.ClearDomainEvents();
        var version = permission.Version;

        permission.SoftDelete(Actor, Now);

        permission.Version.Should().Be(version);
        permission.DomainEvents.Should().NotContain(e => e is ResourcePermissionSoftDeletedEvent);
    }

    [Fact]
    public void ResourcePermission_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var permission = ResourcePermission.Grant(WsA, ResourceType.Board, Guid.NewGuid(), PermissionSubjectType.User, Actor, PermissionLevel.Editor, Actor, Now);
        permission.ClearDomainEvents();
        var version = permission.Version;

        permission.Restore(Actor, Now);

        permission.Version.Should().Be(version);
        permission.DomainEvents.Should().NotContain(e => e is ResourcePermissionRestoredEvent);
    }

    [Fact]
    public void ResourcePermission_Revoke_ShouldEmitBothEvents()
    {
        var permission = ResourcePermission.Grant(WsA, ResourceType.Board, Guid.NewGuid(), PermissionSubjectType.User, Actor, PermissionLevel.Editor, Actor, Now);
        permission.ClearDomainEvents();
        var version = permission.Version;

        permission.Revoke(Actor, Now);

        permission.IsDeleted.Should().BeTrue();
        permission.DomainEvents.Should().Contain(e => e is ResourcePermissionSoftDeletedEvent);
        permission.DomainEvents.Should().Contain(e => e is ResourcePermissionRevokedEvent);
    }

    #endregion

    #region Task 26 — ShareLink SoftDelete/Restore

    [Fact]
    public void ShareLink_SoftDelete_ShouldRaiseEvent()
    {
        var tokenHash = ShareLinkTokenHash.Create("test-hash");
        var link = ShareLink.Create(WsA, ResourceType.Board, Guid.NewGuid(), tokenHash, ShareLinkAccessMode.WorkspaceOnly, Actor, Now);
        link.ClearDomainEvents();
        var version = link.Version;

        link.SoftDelete(Actor, Now);

        link.IsDeleted.Should().BeTrue();
        link.Version.Should().Be(version + 1);
        link.DomainEvents.Should().ContainSingle(e => e is ShareLinkSoftDeletedEvent);
        var evt = (ShareLinkSoftDeletedEvent)link.DomainEvents.Single(e => e is ShareLinkSoftDeletedEvent);
        evt.LinkId.Should().Be(link.Id);
        evt.DeletedBy.Should().Be(Actor);
    }

    [Fact]
    public void ShareLink_Restore_ShouldRaiseEvent()
    {
        var tokenHash = ShareLinkTokenHash.Create("test-hash");
        var link = ShareLink.Create(WsA, ResourceType.Board, Guid.NewGuid(), tokenHash, ShareLinkAccessMode.WorkspaceOnly, Actor, Now);
        link.SoftDelete(Actor, Now);
        link.ClearDomainEvents();
        var version = link.Version;

        link.Restore(Actor, Now);

        link.IsDeleted.Should().BeFalse();
        link.Version.Should().Be(version + 1);
        link.DomainEvents.Should().ContainSingle(e => e is ShareLinkRestoredEvent);
        var evt = (ShareLinkRestoredEvent)link.DomainEvents.Single(e => e is ShareLinkRestoredEvent);
        evt.LinkId.Should().Be(link.Id);
        evt.RestoredBy.Should().Be(Actor);
    }

    [Fact]
    public void ShareLink_SoftDelete_WhenAlreadyDeleted_ShouldNotRaiseEvent()
    {
        var tokenHash = ShareLinkTokenHash.Create("test-hash");
        var link = ShareLink.Create(WsA, ResourceType.Board, Guid.NewGuid(), tokenHash, ShareLinkAccessMode.WorkspaceOnly, Actor, Now);
        link.SoftDelete(Actor, Now);
        link.ClearDomainEvents();
        var version = link.Version;

        link.SoftDelete(Actor, Now);

        link.Version.Should().Be(version);
        link.DomainEvents.Should().NotContain(e => e is ShareLinkSoftDeletedEvent);
    }

    [Fact]
    public void ShareLink_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var tokenHash = ShareLinkTokenHash.Create("test-hash");
        var link = ShareLink.Create(WsA, ResourceType.Board, Guid.NewGuid(), tokenHash, ShareLinkAccessMode.WorkspaceOnly, Actor, Now);
        link.ClearDomainEvents();
        var version = link.Version;

        link.Restore(Actor, Now);

        link.Version.Should().Be(version);
        link.DomainEvents.Should().NotContain(e => e is ShareLinkRestoredEvent);
    }

    #endregion

    #region Task 27 — CustomRole Archive/Activate + dedicated SoftDelete/Restore events

    [Fact]
    public void CustomRole_Archive_ShouldRaiseEvent()
    {
        var role = CustomRole.Create(WsA, "Admin", null, Actor, Now);
        role.ClearDomainEvents();
        var version = role.Version;

        role.Archive(Actor, Now);

        role.Status.Should().Be(CustomRoleStatus.Archived);
        role.Version.Should().Be(version + 1);
        role.DomainEvents.Should().ContainSingle(e => e is CustomRoleArchivedEvent);
        var evt = (CustomRoleArchivedEvent)role.DomainEvents.Single(e => e is CustomRoleArchivedEvent);
        evt.RoleId.Should().Be(role.Id);
        evt.ArchivedBy.Should().Be(Actor);
    }

    [Fact]
    public void CustomRole_Archive_WhenAlreadyArchived_ShouldNotRaiseEvent()
    {
        var role = CustomRole.Create(WsA, "Admin", null, Actor, Now);
        role.Archive(Actor, Now);
        role.ClearDomainEvents();
        var version = role.Version;

        role.Archive(Actor, Now);

        role.Version.Should().Be(version);
        role.DomainEvents.Should().NotContain(e => e is CustomRoleArchivedEvent);
    }

    [Fact]
    public void CustomRole_Activate_ShouldRaiseEvent()
    {
        var role = CustomRole.Create(WsA, "Admin", null, Actor, Now);
        role.Archive(Actor, Now);
        role.ClearDomainEvents();
        var version = role.Version;

        role.Activate(Actor, Now);

        role.Status.Should().Be(CustomRoleStatus.Active);
        role.Version.Should().Be(version + 1);
        role.DomainEvents.Should().ContainSingle(e => e is CustomRoleActivatedEvent);
        var evt = (CustomRoleActivatedEvent)role.DomainEvents.Single(e => e is CustomRoleActivatedEvent);
        evt.RoleId.Should().Be(role.Id);
        evt.ActivatedBy.Should().Be(Actor);
    }

    [Fact]
    public void CustomRole_Activate_WhenNotArchived_ShouldNotRaiseEvent()
    {
        var role = CustomRole.Create(WsA, "Admin", null, Actor, Now);
        role.ClearDomainEvents();
        var version = role.Version;

        role.Activate(Actor, Now);

        role.Version.Should().Be(version);
        role.DomainEvents.Should().NotContain(e => e is CustomRoleActivatedEvent);
    }

    [Fact]
    public void CustomRole_SoftDelete_ShouldRaiseDedicatedEvent()
    {
        var role = CustomRole.Create(WsA, "Admin", null, Actor, Now);
        role.ClearDomainEvents();
        var version = role.Version;

        role.SoftDelete(Actor, Now);

        role.IsDeleted.Should().BeTrue();
        role.Version.Should().Be(version + 1);
        role.DomainEvents.Should().ContainSingle(e => e is CustomRoleSoftDeletedEvent);
        role.DomainEvents.Should().NotContain(e => e is CustomRoleUpdatedEvent);
        var evt = (CustomRoleSoftDeletedEvent)role.DomainEvents.Single(e => e is CustomRoleSoftDeletedEvent);
        evt.RoleId.Should().Be(role.Id);
        evt.DeletedBy.Should().Be(Actor);
    }

    [Fact]
    public void CustomRole_Restore_ShouldRaiseDedicatedEvent()
    {
        var role = CustomRole.Create(WsA, "Admin", null, Actor, Now);
        role.SoftDelete(Actor, Now);
        role.ClearDomainEvents();
        var version = role.Version;

        role.Restore(Actor, Now);

        role.IsDeleted.Should().BeFalse();
        role.Version.Should().Be(version + 1);
        role.DomainEvents.Should().ContainSingle(e => e is CustomRoleRestoredEvent);
        role.DomainEvents.Should().NotContain(e => e is CustomRoleUpdatedEvent);
        var evt = (CustomRoleRestoredEvent)role.DomainEvents.Single(e => e is CustomRoleRestoredEvent);
        evt.RoleId.Should().Be(role.Id);
        evt.RestoredBy.Should().Be(Actor);
    }

    [Fact]
    public void CustomRole_SoftDelete_WhenAlreadyDeleted_ShouldNotRaiseEvent()
    {
        var role = CustomRole.Create(WsA, "Admin", null, Actor, Now);
        role.SoftDelete(Actor, Now);
        role.ClearDomainEvents();
        var version = role.Version;

        role.SoftDelete(Actor, Now);

        role.Version.Should().Be(version);
        role.DomainEvents.Should().NotContain(e => e is CustomRoleSoftDeletedEvent);
    }

    [Fact]
    public void CustomRole_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var role = CustomRole.Create(WsA, "Admin", null, Actor, Now);
        role.ClearDomainEvents();
        var version = role.Version;

        role.Restore(Actor, Now);

        role.Version.Should().Be(version);
        role.DomainEvents.Should().NotContain(e => e is CustomRoleRestoredEvent);
    }

    #endregion
}
