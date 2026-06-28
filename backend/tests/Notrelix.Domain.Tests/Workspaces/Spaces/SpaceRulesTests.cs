using FluentAssertions;
using Notrelix.Domain.Workspaces.Rules;

namespace Notrelix.Domain.Tests.Workspaces;

public class SpaceRulesTests
{
    [Fact]
    public void ValidateName_WithValidName_ShouldNotThrow()
    {
        var act = () => SpaceRules.ValidateName("Marketing");
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateName_WithNull_ShouldThrow()
    {
        var act = () => SpaceRules.ValidateName(null!);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void ValidateName_WithEmptyString_ShouldThrow()
    {
        var act = () => SpaceRules.ValidateName("");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void ValidateName_WithWhiteSpace_ShouldThrow()
    {
        var act = () => SpaceRules.ValidateName("   ");
        act.Should().Throw<BusinessRuleException>();
    }
}
