using FluentAssertions;

namespace Notrelix.Domain.Tests.Identity;

public class RefreshTokenHashTests
{
    [Fact]
    public void Create_ShouldProduceHash()
    {
        var hash = RefreshTokenHash.Create("my-raw-token");

        hash.Hash.Should().NotBeNullOrWhiteSpace();
        hash.Hash.Should().NotBe("my-raw-token");
    }

    [Fact]
    public void Create_SameInput_ShouldProduceSameHash()
    {
        var hash1 = RefreshTokenHash.Create("same-token");
        var hash2 = RefreshTokenHash.Create("same-token");

        hash1.Hash.Should().Be(hash2.Hash);
    }

    [Fact]
    public void Create_DifferentInput_ShouldProduceDifferentHash()
    {
        var hash1 = RefreshTokenHash.Create("token-a");
        var hash2 = RefreshTokenHash.Create("token-b");

        hash1.Hash.Should().NotBe(hash2.Hash);
    }

    [Fact]
    public void Create_WithNull_ShouldThrow()
    {
        var act = () => RefreshTokenHash.Create(null!);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmpty_ShouldThrow()
    {
        var act = () => RefreshTokenHash.Create("");

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Equality_ShouldBeValueBased()
    {
        var hash1 = RefreshTokenHash.Create("same-token");
        var hash2 = RefreshTokenHash.Create("same-token");

        hash1.Should().Be(hash2);
        (hash1 == hash2).Should().BeTrue();
        hash1.GetHashCode().Should().Be(hash2.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentTokens_ShouldNotBeEqual()
    {
        var hash1 = RefreshTokenHash.Create("token-a");
        var hash2 = RefreshTokenHash.Create("token-b");

        hash1.Should().NotBe(hash2);
        (hash1 != hash2).Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldUseSHA256()
    {
        var hash = RefreshTokenHash.Create("test-token");

        hash.Hash.Should().HaveLength(44);
    }
}
