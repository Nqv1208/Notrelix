using FluentAssertions;

namespace Notrelix.Domain.Tests.Common;

public class GuardTests
{
    [Fact]
    public void NotNull_ShouldThrowBusinessRuleException_WhenNull()
    {
        Action act = () => Guard.NotNull<object>(null);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("Parameter 'null' cannot be null.");
    }

    [Fact]
    public void NotNull_ShouldNotThrow_WhenNotNull()
    {
        Action act = () => Guard.NotNull(new object());

        act.Should().NotThrow();
    }

    [Fact]
    public void NotNullOrWhiteSpace_ShouldThrow_WhenWhiteSpace()
    {
        Action act = () => Guard.NotNullOrWhiteSpace("   ");

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Positive_ShouldThrow_WhenZero()
    {
        Action act = () => Guard.Positive(0);

        var ex = act.Should().Throw<BusinessRuleException>().Which;
        ex.RuleCode.Should().Be(BusinessRuleCodes.Guard_Positive);
    }

    [Fact]
    public void NotEmpty_ShouldThrow_WhenEmpty()
    {
        Action act = () => Guard.NotEmpty(Guid.Empty);

        var ex = act.Should().Throw<BusinessRuleException>().Which;
        ex.RuleCode.Should().Be(BusinessRuleCodes.Guard_Empty);
    }
}
