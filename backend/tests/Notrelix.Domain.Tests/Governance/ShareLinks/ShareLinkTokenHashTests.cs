using FluentAssertions;
using Notrelix.Domain.Governance.ShareLinks;

namespace Notrelix.Domain.Tests.Governance;

public class ShareLinkTokenHashTests
{
    [Fact]
    public void Create_WithValidToken_ShouldProduceHash()
    {
        var hash = ShareLinkTokenHash.Create("my-secret-token");
        hash.Hash.Should().NotBeNullOrWhiteSpace();
        hash.Hash.Should().NotBe("my-secret-token"); // should be hashed
    }

    [Fact]
    public void Create_SameToken_ShouldProduceSameHash()
    {
        var hash1 = ShareLinkTokenHash.Create("token123");
        var hash2 = ShareLinkTokenHash.Create("token123");

        hash1.Hash.Should().Be(hash2.Hash);
    }

    [Fact]
    public void Create_DifferentTokens_ShouldProduceDifferentHashes()
    {
        var hash1 = ShareLinkTokenHash.Create("token123");
        var hash2 = ShareLinkTokenHash.Create("token456");

        hash1.Hash.Should().NotBe(hash2.Hash);
    }

    [Fact]
    public void Create_WithNull_ShouldThrow()
    {
        var act = () => ShareLinkTokenHash.Create(null!);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyString_ShouldThrow()
    {
        var act = () => ShareLinkTokenHash.Create("");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Equality_SameHash_ShouldBeEqual()
    {
        var h1 = ShareLinkTokenHash.Create("token");
        var h2 = ShareLinkTokenHash.Create("token");

        h1.Should().Be(h2);
    }

    [Fact]
    public void Equality_DifferentHash_ShouldNotBeEqual()
    {
        var h1 = ShareLinkTokenHash.Create("token1");
        var h2 = ShareLinkTokenHash.Create("token2");

        h1.Should().NotBe(h2);
    }
}
