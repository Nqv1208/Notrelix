using FluentAssertions;
using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;

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
    public void Assert_ShouldThrow_WhenConditionIsFalse()
    {
        Action act = () => Guard.Assert(1 == 2, "Math is broken.");

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("Math is broken.");
    }
}
