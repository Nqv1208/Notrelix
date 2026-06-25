using FluentAssertions;

namespace Notrelix.Domain.Tests.Identity.Tokens;

public class ApiTokenScopesTests
{
    [Fact]
    public void FromJson_ShouldCreateEmpty_WithEmptyArrayJson()
    {
        var scopes = ApiTokenScopes.FromJson("[]");

        scopes.HasAny().Should().BeFalse();
        scopes.Allows("anything").Should().BeFalse();
    }

    [Fact]
    public void FromJson_ShouldParseScopeArray()
    {
        var scopes = ApiTokenScopes.FromJson("[\"read\",\"write\"]");

        scopes.HasAny().Should().BeTrue();
        scopes.Allows("read").Should().BeTrue();
        scopes.Allows("write").Should().BeTrue();
    }

    [Fact]
    public void Allows_ShouldReturnFalse_ForUnlistedScope()
    {
        var scopes = ApiTokenScopes.FromJson("[\"read\"]");

        scopes.Allows("admin").Should().BeFalse();
    }

    [Fact]
    public void ToJson_ShouldRoundtrip()
    {
        var json = "[\"read\",\"write\",\"admin\"]";
        var scopes = ApiTokenScopes.FromJson(json);

        scopes.ToJson().Should().Be(json);
    }

    [Fact]
    public void Equals_ShouldCompareByScopeValues()
    {
        var scopes1 = ApiTokenScopes.FromJson("[\"read\",\"write\"]");
        var scopes2 = ApiTokenScopes.FromJson("[\"read\",\"write\"]");

        scopes1.Equals(scopes2).Should().BeTrue();
        (scopes1 == scopes2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_ForDifferentScopes()
    {
        var scopes1 = ApiTokenScopes.FromJson("[\"read\"]");
        var scopes2 = ApiTokenScopes.FromJson("[\"read\",\"write\"]");

        scopes1.Equals(scopes2).Should().BeFalse();
        (scopes1 != scopes2).Should().BeTrue();
    }

    [Fact]
    public void FromJson_ShouldThrow_ForInvalidJson()
    {
        Action act = () => ApiTokenScopes.FromJson("not-json");

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void ScopeOrder_ShouldNotAffectEquality()
    {
        var scopes1 = ApiTokenScopes.FromJson("[\"read\",\"write\"]");
        var scopes2 = ApiTokenScopes.FromJson("[\"write\",\"read\"]");

        scopes1.Equals(scopes2).Should().BeTrue();
    }

    [Fact]
    public void HasAny_ShouldReturnTrue_WhenNonEmpty()
    {
        var scopes = ApiTokenScopes.FromJson("[\"read\"]");
        scopes.HasAny().Should().BeTrue();
    }
}
