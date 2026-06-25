using FluentAssertions;

namespace Notrelix.Domain.Tests.Governance;

public class SecurityEventMetadataTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var data = JsonValue.EmptyObject();
        var metadata = SecurityEventMetadata.Create(data);
        metadata.Data.Should().Be(data);
    }

    [Fact]
    public void Create_WithNull_ShouldThrow()
    {
        var act = () => SecurityEventMetadata.Create(null!);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Equality_SameData_ShouldBeEqual()
    {
        var data = JsonValue.EmptyObject();
        var m1 = SecurityEventMetadata.Create(data);
        var m2 = SecurityEventMetadata.Create(data);

        m1.Should().Be(m2);
    }

    [Fact]
    public void Equality_DifferentData_ShouldNotBeEqual()
    {
        var m1 = SecurityEventMetadata.Create(JsonValue.Create("{\"a\":1}"));
        var m2 = SecurityEventMetadata.Create(JsonValue.Create("{\"a\":2}"));

        m1.Should().NotBe(m2);
    }
}
