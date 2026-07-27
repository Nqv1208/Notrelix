using FluentAssertions;
using Notrelix.Domain.Identity.OAuth;

namespace Notrelix.Domain.Tests.Identity;

public class OAuthProfileSnapshotTests
{
    [Fact]
    public void Create_ValidSnapshot_ShouldSucceed()
    {
        var data = JsonValue.Create("{\"sub\":\"123\",\"name\":\"Test\"}");

        var snapshot = OAuthProfileSnapshot.Create(OAuthProvider.Google, 1, data);

        snapshot.Provider.Should().Be(OAuthProvider.Google);
        snapshot.SchemaVersion.Should().Be(1);
        snapshot.Data.Should().Be(data);
    }

    [Fact]
    public void Create_NullData_ShouldThrow()
    {
        var act = () => OAuthProfileSnapshot.Create(OAuthProvider.Google, 1, null!);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_ZeroSchemaVersion_ShouldThrow()
    {
        var data = JsonValue.EmptyObject();

        var act = () => OAuthProfileSnapshot.Create(OAuthProvider.Google, 0, data);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*schema version must be positive*");
    }

    [Fact]
    public void Create_NegativeSchemaVersion_ShouldThrow()
    {
        var data = JsonValue.EmptyObject();

        var act = () => OAuthProfileSnapshot.Create(OAuthProvider.Google, -1, data);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*schema version must be positive*");
    }

    [Fact]
    public void Create_NonObjectJson_ShouldThrow()
    {
        var data = JsonValue.Create("[1,2,3]");

        var act = () => OAuthProfileSnapshot.Create(OAuthProvider.Google, 1, data);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*must be a JSON object*");
    }

    [Fact]
    public void Create_StringJson_ShouldThrow()
    {
        var data = JsonValue.Create("\"hello\"");

        var act = () => OAuthProfileSnapshot.Create(OAuthProvider.Google, 1, data);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*must be a JSON object*");
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        var data = JsonValue.Create("{\"sub\":\"123\"}");
        var s1 = OAuthProfileSnapshot.Create(OAuthProvider.Google, 1, data);
        var s2 = OAuthProfileSnapshot.Create(OAuthProvider.Google, 1, JsonValue.Create("{\"sub\":\"123\"}"));

        s1.Should().Be(s2);
        s1.GetHashCode().Should().Be(s2.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentProvider_ShouldNotBeEqual()
    {
        var data = JsonValue.EmptyObject();
        var s1 = OAuthProfileSnapshot.Create(OAuthProvider.Google, 1, data);
        var s2 = OAuthProfileSnapshot.Create(OAuthProvider.GitHub, 1, data);

        s1.Should().NotBe(s2);
    }

    [Fact]
    public void Equality_DifferentVersion_ShouldNotBeEqual()
    {
        var data = JsonValue.EmptyObject();
        var s1 = OAuthProfileSnapshot.Create(OAuthProvider.Google, 1, data);
        var s2 = OAuthProfileSnapshot.Create(OAuthProvider.Google, 2, data);

        s1.Should().NotBe(s2);
    }

    [Fact]
    public void Equality_DifferentData_ShouldNotBeEqual()
    {
        var s1 = OAuthProfileSnapshot.Create(OAuthProvider.Google, 1, JsonValue.Create("{\"a\":1}"));
        var s2 = OAuthProfileSnapshot.Create(OAuthProvider.Google, 1, JsonValue.Create("{\"a\":2}"));

        s1.Should().NotBe(s2);
    }
}
