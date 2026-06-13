using FluentAssertions;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Governance.ShareLinks;

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
        var rule = PermissionRule.Create(WsA, "Workspace", "Board", null, "User", Actor, null, "edit", PermissionEffect.Allow, Actor, Now);
        rule.Should().NotBeNull();
        rule.WorkspaceId.Should().Be(WsA);
        rule.Status.Should().Be("Active");
        rule.DomainEvents.Should().ContainSingle(e => e is PermissionRuleCreatedDomainEvent);
    }

    [Fact]
    public void PermissionRule_Disable_ShouldUpdateStatus()
    {
        var rule = PermissionRule.Create(WsA, "Workspace", "Board", null, "User", Actor, null, "edit", PermissionEffect.Allow, Actor, Now);
        rule.ClearDomainEvents();

        rule.Disable(Actor, Now);

        rule.Status.Should().Be("Disabled");
        rule.DomainEvents.Should().ContainSingle(e => e is PermissionRuleDisabledDomainEvent);
    }

    [Fact]
    public void PermissionRule_IsActive_WhenActive_ShouldReturnTrue()
    {
        var rule = PermissionRule.Create(WsA, "Workspace", "Board", null, "User", Actor, null, "edit", PermissionEffect.Allow, Actor, Now);
        rule.IsActive(Now).Should().BeTrue();
    }

    [Fact]
    public void PermissionRule_IsActive_WhenDisabled_ShouldReturnFalse()
    {
        var rule = PermissionRule.Create(WsA, "Workspace", "Board", null, "User", Actor, null, "edit", PermissionEffect.Allow, Actor, Now);
        rule.Disable(Actor, Now);
        rule.IsActive(Now).Should().BeFalse();
    }

    [Fact]
    public void PermissionRule_IsActive_WhenExpired_ShouldReturnFalse()
    {
        var rule = PermissionRule.Create(WsA, "Workspace", "Board", null, "User", Actor, null, "edit", PermissionEffect.Allow, Actor, Now, expiresAt: Now.AddDays(-1));
        rule.IsActive(Now).Should().BeFalse();
    }

    [Fact]
    public void PermissionRule_IsActive_WhenNotYetStarted_ShouldReturnFalse()
    {
        var rule = PermissionRule.Create(WsA, "Workspace", "Board", null, "User", Actor, null, "edit", PermissionEffect.Allow, Actor, Now, startsAt: Now.AddDays(1));
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
}
