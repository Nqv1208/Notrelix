using FluentAssertions;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Identity.Tokens;
using Xunit;

namespace Notrelix.Domain.Tests.Identity.Tokens;

public class TokenHashTests
{
    [Fact]
    public void Create_WithValidValue_ShouldSucceed()
    {
        var hash = TokenHash.Create("hashed-value");

        hash.Value.Should().Be("hashed-value");
    }

    [Fact]
    public void Create_ShouldTrimValue()
    {
        var hash = TokenHash.Create("  hashed-value  ");

        hash.Value.Should().Be("hashed-value");
    }

    [Fact]
    public void Create_WithNull_ShouldThrow()
    {
        var act = () => TokenHash.Create(null!);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmpty_ShouldThrow()
    {
        var act = () => TokenHash.Create("");

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Equality_ShouldBeValueBased()
    {
        var hash1 = TokenHash.Create("same-value");
        var hash2 = TokenHash.Create("same-value");

        hash1.Should().Be(hash2);
        (hash1 == hash2).Should().BeTrue();
        hash1.GetHashCode().Should().Be(hash2.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentValues_ShouldNotBeEqual()
    {
        var hash1 = TokenHash.Create("value-a");
        var hash2 = TokenHash.Create("value-b");

        hash1.Should().NotBe(hash2);
        (hash1 != hash2).Should().BeTrue();
    }
}
