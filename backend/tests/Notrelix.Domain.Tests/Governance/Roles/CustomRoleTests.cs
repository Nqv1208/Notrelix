using FluentAssertions;
using Notrelix.Domain.Governance;
using Notrelix.Domain.Governance.Roles;

namespace Notrelix.Domain.Tests.Governance;

public class CustomRoleTests
{
    private static readonly string[] None = [];

    private static CustomRole NewRole(string[]? added = null, string[]? removed = null) =>
        CustomRole.Create(
            Guid.NewGuid(), "Project Manager", "Manages projects", CustomRoleBase.Member,
            added ?? ["ViewBilling"], removed ?? ["CreateBoard"], Guid.NewGuid(), DateTimeOffset.UtcNow);

    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var accountId = Guid.NewGuid();

        var role = CustomRole.Create(
            accountId, "  Project Manager  ", "Manages projects", CustomRoleBase.Admin,
            ["ViewBilling"], ["CreateBoard"], Guid.NewGuid(), DateTimeOffset.UtcNow);

        role.Name.Should().Be("Project Manager");
        role.AccountId.Should().Be(accountId);
        role.BaseRole.Should().Be(CustomRoleBase.Admin);
        role.Status.Should().Be(CustomRoleStatus.Active);
        role.AddedActions.Should().Equal("ViewBilling");
        role.RemovedActions.Should().Equal("CreateBoard");
        role.DomainEvents.Should().ContainSingle(e => e is CustomRoleCreatedDomainEvent);
    }

    [Fact]
    public void Create_WithNoActions_IsRefused()
    {
        var act = () => CustomRole.Create(
            Guid.NewGuid(), "Role", null, CustomRoleBase.Member, None, None, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(GovernanceRuleCodes.Governance_Role_NoChanges);
    }

    [Fact]
    public void Create_WithAnActionAddedAndRemoved_IsRefused()
    {
        var act = () => CustomRole.Create(
            Guid.NewGuid(), "Role", null, CustomRoleBase.Member, ["CreateBoard"], ["CreateBoard"], Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(GovernanceRuleCodes.Governance_Role_ActionConflict);
    }

    [Fact]
    public void Create_WithoutAName_IsRefused()
    {
        var act = () => CustomRole.Create(
            Guid.NewGuid(), "  ", null, CustomRoleBase.Member, ["ViewBilling"], None, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(GovernanceRuleCodes.Governance_Role_NameRequired);
    }

    [Fact]
    public void Revise_ChangesActionsAndRaisesEvent()
    {
        var role = NewRole();
        ((IHasDomainEvents)role).ClearDomainEvents();
        var version = role.Version;

        var changed = role.Revise("Project Manager", "Manages projects", ["ViewBilling", "ReadAudit"], ["CreateBoard"], Guid.NewGuid(), DateTimeOffset.UtcNow);

        changed.Should().BeTrue();
        role.AddedActions.Should().Equal("ReadAudit", "ViewBilling");
        role.Version.Should().Be(version + 1);
        role.DomainEvents.Should().ContainSingle(e => e is CustomRoleUpdatedDomainEvent);
    }

    [Fact]
    public void Revise_WithIdenticalContent_IsANoOp()
    {
        var role = NewRole();
        ((IHasDomainEvents)role).ClearDomainEvents();
        var version = role.Version;

        var changed = role.Revise("Project Manager", "Manages projects", ["ViewBilling"], ["CreateBoard"], Guid.NewGuid(), DateTimeOffset.UtcNow);

        changed.Should().BeFalse();
        role.Version.Should().Be(version);
        role.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Revise_WithAConflictingSet_LeavesTheRoleUnchanged()
    {
        var role = NewRole();
        var version = role.Version;

        var act = () => role.Revise("Renamed", null, ["CreateBoard"], ["CreateBoard"], Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
        role.Name.Should().Be("Project Manager");
        role.AddedActions.Should().Equal("ViewBilling");
        role.Version.Should().Be(version);
    }

    [Fact]
    public void Revise_AnArchivedRole_IsRefused()
    {
        var role = NewRole();
        role.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => role.Revise("Other", null, ["ViewBilling"], None, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(GovernanceRuleCodes.Governance_Role_Archived);
    }

    [Fact]
    public void Archive_ShouldSetStatusAndRaiseEvent()
    {
        var role = NewRole();
        ((IHasDomainEvents)role).ClearDomainEvents();

        role.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        role.Status.Should().Be(CustomRoleStatus.Archived);
        role.DomainEvents.Should().ContainSingle(e => e is CustomRoleArchivedDomainEvent);
    }

    [Fact]
    public void Archive_AnArchivedRole_IsANoOp()
    {
        var role = NewRole();
        role.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)role).ClearDomainEvents();
        var version = role.Version;

        role.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        role.Version.Should().Be(version);
        role.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Activate_ShouldRestoreStatusAndRaiseEvent()
    {
        var role = NewRole();
        role.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)role).ClearDomainEvents();

        role.Activate(Guid.NewGuid(), DateTimeOffset.UtcNow);

        role.Status.Should().Be(CustomRoleStatus.Active);
        role.DomainEvents.Should().ContainSingle(e => e is CustomRoleActivatedDomainEvent);
    }
}
