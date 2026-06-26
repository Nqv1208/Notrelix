using FluentAssertions;

namespace Notrelix.Domain.Tests.Identity;

public class UserNameTests
{
    [Fact]
    public void Create_WithValidValue_ShouldSucceed()
    {
        var name = UserName.Create("John Doe");

        name.Value.Should().Be("John Doe");
    }

    [Fact]
    public void Create_ShouldTrimValue()
    {
        var name = UserName.Create("  John Doe  ");

        name.Value.Should().Be("John Doe");
    }

    [Fact]
    public void Create_WithNull_ShouldThrow()
    {
        var act = () => UserName.Create(null!);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmpty_ShouldThrow()
    {
        var act = () => UserName.Create("");

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithWhitespace_ShouldThrow()
    {
        var act = () => UserName.Create("   ");

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Equality_ShouldBeValueBased()
    {
        var name1 = UserName.Create("John Doe");
        var name2 = UserName.Create("John Doe");

        name1.Should().Be(name2);
        (name1 == name2).Should().BeTrue();
        name1.GetHashCode().Should().Be(name2.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentValues_ShouldNotBeEqual()
    {
        var name1 = UserName.Create("John Doe");
        var name2 = UserName.Create("Jane Doe");

        name1.Should().NotBe(name2);
        (name1 != name2).Should().BeTrue();
    }
}
