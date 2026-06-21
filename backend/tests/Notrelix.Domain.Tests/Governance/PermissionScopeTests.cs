using FluentAssertions;
using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Governance.Permissions;
using Xunit;

namespace Notrelix.Domain.Tests.Governance;

public class PermissionScopeTests
{
    [Fact]
    public void Create_WithValidValue_ShouldSucceed()
    {
        var scope = PermissionScope.Create("board:123");
        scope.Value.Should().Be("board:123");
    }

    [Fact]
    public void Create_ShouldTrimAndLowercase()
    {
        var scope = PermissionScope.Create("  Board:ABC  ");
        scope.Value.Should().Be("board:abc");
    }

    [Fact]
    public void Create_WithNull_ShouldThrow()
    {
        var act = () => PermissionScope.Create(null!);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyString_ShouldThrow()
    {
        var act = () => PermissionScope.Create("");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void All_ShouldReturnWildcard()
    {
        var scope = PermissionScope.All();
        scope.Value.Should().Be("*");
    }

    [Fact]
    public void Equality_SameValue_ShouldBeEqual()
    {
        var s1 = PermissionScope.Create("board:123");
        var s2 = PermissionScope.Create("board:123");

        s1.Should().Be(s2);
    }

    [Fact]
    public void Equality_DifferentValue_ShouldNotBeEqual()
    {
        var s1 = PermissionScope.Create("board:123");
        var s2 = PermissionScope.Create("board:456");

        s1.Should().NotBe(s2);
    }
}
