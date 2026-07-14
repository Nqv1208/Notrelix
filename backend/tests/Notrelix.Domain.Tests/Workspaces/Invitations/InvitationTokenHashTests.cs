using FluentAssertions;
using Notrelix.Domain.Workspaces.Invitations;

namespace Notrelix.Domain.Tests.Workspaces;

public class InvitationTokenHashTests
{
    private const string ValidHash64 = "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2";
    private const string ValidHash64Alt = "b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3";

    [Fact]
    public void Create_WithValidHash_ShouldSucceed()
    {
        var hash = InvitationTokenHash.Create(ValidHash64);
        hash.Value.Should().Be(ValidHash64);
    }

    [Fact]
    public void Create_ShouldTrimValue()
    {
        var hash = InvitationTokenHash.Create($"  {ValidHash64}  ");
        hash.Value.Should().Be(ValidHash64);
    }

    [Fact]
    public void Create_WithNull_ShouldThrow()
    {
        var act = () => InvitationTokenHash.Create(null!);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyString_ShouldThrow()
    {
        var act = () => InvitationTokenHash.Create("");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithNonHex_ShouldThrow()
    {
        var act = () => InvitationTokenHash.Create("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithWrongLength_ShouldThrow()
    {
        var act = () => InvitationTokenHash.Create("abc123");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Equality_SameValue_ShouldBeEqual()
    {
        var h1 = InvitationTokenHash.Create(ValidHash64);
        var h2 = InvitationTokenHash.Create(ValidHash64);

        h1.Should().Be(h2);
        (h1 == h2).Should().BeTrue();
        h1.GetHashCode().Should().Be(h2.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentValue_ShouldNotBeEqual()
    {
        var h1 = InvitationTokenHash.Create(ValidHash64);
        var h2 = InvitationTokenHash.Create(ValidHash64Alt);

        h1.Should().NotBe(h2);
        (h1 != h2).Should().BeTrue();
    }
}
