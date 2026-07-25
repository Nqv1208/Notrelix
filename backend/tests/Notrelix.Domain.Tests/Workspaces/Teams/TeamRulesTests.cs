using FluentAssertions;
using Notrelix.Domain.Workspaces.Rules;
using Notrelix.Domain.Workspaces.Teams;

namespace Notrelix.Domain.Tests.Workspaces;

public class TeamRulesTests
{
    [Fact]
    public void ValidateName_WithValidName_ShouldNotThrow()
    {
        var act = () => TeamRules.ValidateName("Dev Team");
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateName_WithNull_ShouldThrow()
    {
        var act = () => TeamRules.ValidateName(null!);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void ValidateName_WithEmptyString_ShouldThrow()
    {
        var act = () => TeamRules.ValidateName("");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void ValidateName_WithWhiteSpace_ShouldThrow()
    {
        var act = () => TeamRules.ValidateName("   ");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void EnsureCanRemoveLead_WithMultipleLeads_ShouldNotThrow()
    {
        var act = () => TeamLeadRules.EnsureCanRemoveLead(2);
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanRemoveLead_WithSingleLead_ShouldThrow()
    {
        var act = () => TeamLeadRules.EnsureCanRemoveLead(1);
        act.Should().Throw<BusinessRuleException>().WithMessage("*last lead*");
    }

    [Fact]
    public void EnsureCanDowngradeLead_WhenMultipleLeads_ShouldNotThrow()
    {
        var act = () => TeamLeadRules.EnsureCanDowngradeLead(TeamMemberRole.Lead, TeamMemberRole.Member, 2);
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanDowngradeLead_WhenLastLead_ShouldThrow()
    {
        var act = () => TeamLeadRules.EnsureCanDowngradeLead(TeamMemberRole.Lead, TeamMemberRole.Member, 1);
        act.Should().Throw<BusinessRuleException>().WithMessage("*last lead*");
    }

    [Fact]
    public void EnsureCanDowngradeLead_WhenNotLead_ShouldNotThrow()
    {
        var act = () => TeamLeadRules.EnsureCanDowngradeLead(TeamMemberRole.Member, TeamMemberRole.Lead, 1);
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanLeaveTeam_WhenLastLead_ShouldThrow()
    {
        var act = () => TeamLeadRules.EnsureCanLeaveTeam(TeamMemberRole.Lead, 1);
        act.Should().Throw<BusinessRuleException>().WithMessage("*last lead cannot leave*");
    }

    [Fact]
    public void EnsureCanLeaveTeam_WhenMember_ShouldNotThrow()
    {
        var act = () => TeamLeadRules.EnsureCanLeaveTeam(TeamMemberRole.Member, 1);
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanLeaveTeam_WhenMultipleLeads_ShouldNotThrow()
    {
        var act = () => TeamLeadRules.EnsureCanLeaveTeam(TeamMemberRole.Lead, 2);
        act.Should().NotThrow();
    }
}
