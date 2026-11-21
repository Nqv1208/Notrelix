using FluentAssertions;
using Notrelix.Domain.Identity;

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

    [Fact]
    public void Vocabulary_ShouldBeReadAndWritePerContext_PlusWorkspaceRead()
    {
        ApiTokenScopes.Vocabulary.Should().BeEquivalentTo(
            "work:read", "work:write", "docs:read", "docs:write", "chat:read", "chat:write", "workspace:read");
    }

    [Fact]
    public void Create_ShouldKeepEveryChosenScope_AndAllowOnlyThose()
    {
        var scopes = ApiTokenScopes.Create(["work:read", "docs:write", "work:read"]);

        scopes.Scopes.Should().BeEquivalentTo("work:read", "docs:write");
        scopes.Allows("work:read").Should().BeTrue();
        scopes.Allows("docs:write").Should().BeTrue();
        scopes.Allows("work:write").Should().BeFalse("write does not imply read, nor read write");
        scopes.Allows("docs:read").Should().BeFalse("write does not imply read");
    }

    [Fact]
    public void Create_ShouldRefuseAnEmptyList()
    {
        Action act = () => ApiTokenScopes.Create([]);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode
            .Should().Be(IdentityRuleCodes.Identity_ApiToken_ScopesRequired);
    }

    [Fact]
    public void Create_ShouldRefuseAScopeOutsideTheVocabulary()
    {
        Action act = () => ApiTokenScopes.Create(["work:read", "admin"]);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode
            .Should().Be(IdentityRuleCodes.Identity_ApiToken_UnknownScope);
    }

    [Fact]
    public void All_ShouldAllowEveryScopeOfTheVocabulary()
    {
        var all = ApiTokenScopes.All();

        ApiTokenScopes.Vocabulary.Should().OnlyContain(scope => all.Allows(scope));
    }
}
