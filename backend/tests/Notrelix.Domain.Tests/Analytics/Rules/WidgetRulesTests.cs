using FluentAssertions;
using Notrelix.Domain.Analytics.Rules;
using Notrelix.Domain.Analytics.Widgets;

namespace Notrelix.Domain.Tests.Analytics.Rules;

public class WidgetRulesTests
{
    [Fact]
    public void ValidateTitle_WhenValid_ShouldNotThrow()
    {
        Action act = () => WidgetRules.ValidateTitle("My Widget");

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateTitle_WhenNull_ShouldThrow()
    {
        Action act = () => WidgetRules.ValidateTitle(null!);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void ValidateTitle_WhenEmpty_ShouldThrow()
    {
        Action act = () => WidgetRules.ValidateTitle("");

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void ValidateTitle_WhenWhitespace_ShouldThrow()
    {
        Action act = () => WidgetRules.ValidateTitle("   ");

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void ValidatePosition_WhenValid_ShouldNotThrow()
    {
        var position = WidgetPosition.Create(0, 0, 4, 3);

        Action act = () => WidgetRules.ValidatePosition(position);

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidatePosition_WhenNegativeX_ShouldThrow()
    {
        var position = WidgetPosition.Create(-1, 0, 4, 3);

        Action act = () => WidgetRules.ValidatePosition(position);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*non-negative*");
    }

    [Fact]
    public void ValidatePosition_WhenNegativeY_ShouldThrow()
    {
        var position = WidgetPosition.Create(0, -1, 4, 3);

        Action act = () => WidgetRules.ValidatePosition(position);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*non-negative*");
    }

    [Fact]
    public void ValidatePosition_WhenZeroWidth_ShouldThrow()
    {
        var position = WidgetPosition.Create(0, 0, 0, 3);

        Action act = () => WidgetRules.ValidatePosition(position);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*positive*");
    }

    [Fact]
    public void ValidatePosition_WhenZeroHeight_ShouldThrow()
    {
        var position = WidgetPosition.Create(0, 0, 4, 0);

        Action act = () => WidgetRules.ValidatePosition(position);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*positive*");
    }

    [Fact]
    public void ValidatePosition_WhenNull_ShouldThrow()
    {
        Action act = () => WidgetRules.ValidatePosition(null!);

        act.Should().Throw<BusinessRuleException>();
    }
}
