using FluentAssertions;
using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Workspaces.Rules;
using Xunit;

namespace Notrelix.Domain.Tests.Workspaces;

public class WorkspaceRulesTests
{
    [Fact]
    public void ValidateName_WithValidName_ShouldNotThrow()
    {
        var act = () => WorkspaceRules.ValidateName("My Workspace");
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateName_WithMaxLengthName_ShouldNotThrow()
    {
        var name = new string('a', 160);
        var act = () => WorkspaceRules.ValidateName(name);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateName_WithNameExceedingMaxLength_ShouldThrow()
    {
        var name = new string('a', 161);
        var act = () => WorkspaceRules.ValidateName(name);
        act.Should().Throw<BusinessRuleException>().WithMessage("Workspace name is too long.");
    }

    [Fact]
    public void ValidateName_WithNull_ShouldThrow()
    {
        var act = () => WorkspaceRules.ValidateName(null!);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void ValidateName_WithEmptyString_ShouldThrow()
    {
        var act = () => WorkspaceRules.ValidateName("");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void ValidateName_WithWhiteSpace_ShouldThrow()
    {
        var act = () => WorkspaceRules.ValidateName("   ");
        act.Should().Throw<BusinessRuleException>();
    }
}
