using FluentAssertions;
using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Workspaces.Invitations;
using Xunit;

namespace Notrelix.Domain.Tests.Workspaces;

public class InvitationTokenHashTests
{
    [Fact]
    public void Create_WithValidHash_ShouldSucceed()
    {
        var hash = InvitationTokenHash.Create("abc123");
        hash.Value.Should().Be("abc123");
    }

    [Fact]
    public void Create_ShouldTrimValue()
    {
        var hash = InvitationTokenHash.Create("  abc123  ");
        hash.Value.Should().Be("abc123");
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
    public void Equality_SameValue_ShouldBeEqual()
    {
        var h1 = InvitationTokenHash.Create("abc123");
        var h2 = InvitationTokenHash.Create("abc123");

        h1.Should().Be(h2);
        (h1 == h2).Should().BeTrue();
        h1.GetHashCode().Should().Be(h2.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentValue_ShouldNotBeEqual()
    {
        var h1 = InvitationTokenHash.Create("abc123");
        var h2 = InvitationTokenHash.Create("xyz789");

        h1.Should().NotBe(h2);
        (h1 != h2).Should().BeTrue();
    }
}
