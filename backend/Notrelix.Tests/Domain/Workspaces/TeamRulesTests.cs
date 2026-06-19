using FluentAssertions;
using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Workspaces.Rules;
using Xunit;

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
}
