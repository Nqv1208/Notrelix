using Notrelix.Domain.Workspaces;
using FluentAssertions;

namespace Notrelix.Domain.Tests.Workspaces;

public class WorkspaceMemberJobTitleTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 23, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid Actor = Guid.NewGuid();

    private static WorkspaceMember NewMember(WorkspaceRole role = WorkspaceRole.Member)
        => WorkspaceMember.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), role, Actor, T0);

    [Fact]
    public void ChangeJobTitle_TrimsAndCollapsesWhitespace_AndAdvancesTheVersionOnce()
    {
        var member = NewMember();
        var version = member.Version;

        var changed = member.ChangeJobTitle("  Senior \t  product   designer ".Replace('\t', ' '), Actor, T0.AddMinutes(1));

        changed.Should().BeTrue();
        member.JobTitle.Should().Be("Senior product designer");
        member.Version.Should().Be(version + 1);
        member.UpdatedBy.Should().Be(Actor);
        member.UpdatedAt.Should().Be(T0.AddMinutes(1));
    }

    [Fact]
    public void ChangeJobTitle_SameValueAfterNormalization_IsASemanticNoOp()
    {
        var member = NewMember();
        member.ChangeJobTitle("Designer", Actor, T0.AddMinutes(1));
        var version = member.Version;

        var changed = member.ChangeJobTitle("  Designer  ", Actor, T0.AddMinutes(2));

        changed.Should().BeFalse();
        member.Version.Should().Be(version);
        member.UpdatedAt.Should().Be(T0.AddMinutes(1), "a no-op writes no audit");
    }

    [Fact]
    public void ChangeJobTitle_BlankClearsTheTitle_AndClearingNothingIsANoOp()
    {
        var member = NewMember();
        member.ChangeJobTitle("Designer", Actor, T0.AddMinutes(1));

        member.ChangeJobTitle("   ", Actor, T0.AddMinutes(2)).Should().BeTrue();
        member.JobTitle.Should().BeNull();
        member.ChangeJobTitle(null, Actor, T0.AddMinutes(3)).Should().BeFalse();
    }

    [Fact]
    public void ChangeJobTitle_AtTheLimitIsAccepted_OverTheLimitIsRejectedWithoutPartialChange()
    {
        var member = NewMember();
        member.ChangeJobTitle(new string('a', WorkspaceMember.MaxJobTitleLength), Actor, T0.AddMinutes(1));
        var version = member.Version;

        var act = () => member.ChangeJobTitle(new string('b', WorkspaceMember.MaxJobTitleLength + 1), Actor, T0.AddMinutes(2));

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_JobTitleTooLong);
        member.JobTitle.Should().Be(new string('a', WorkspaceMember.MaxJobTitleLength));
        member.Version.Should().Be(version);
    }

    [Theory]
    [InlineData("two\nlines")]
    [InlineData("tab\there")]
    [InlineData("nul\0char")]
    public void ChangeJobTitle_ControlCharacters_AreRejected(string title)
    {
        var member = NewMember();

        var act = () => member.ChangeJobTitle(title, Actor, T0.AddMinutes(1));

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_InvalidJobTitle);
        member.JobTitle.Should().BeNull();
    }

    [Fact]
    public void ChangeJobTitle_OnARemovedMember_IsRejected_ButASuspendedMemberCanBeEdited()
    {
        var suspended = NewMember();
        suspended.Suspend(Actor, T0.AddMinutes(1), activeOwnerCount: 2);
        suspended.ChangeJobTitle("Contractor", Actor, T0.AddMinutes(2)).Should().BeTrue();

        var removed = NewMember();
        removed.Remove(activeOwnerCount: 2, Actor, T0.AddMinutes(1));
        var act = () => removed.ChangeJobTitle("x", Actor, T0.AddMinutes(2));

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_CannotEditRemovedProfile);
    }

    [Fact]
    public void ChangeJobTitle_RaisesNoDomainEvent_BeyondTheCreationEvent()
    {
        var member = NewMember();
        ((IHasDomainEvents)member).ClearDomainEvents();

        member.ChangeJobTitle("Designer", Actor, T0.AddMinutes(1));

        member.DomainEvents.Should().BeEmpty("a job title is a profile fact no consumer reacts to");
    }
}
