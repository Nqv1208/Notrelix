using FluentAssertions;
using Notrelix.Domain.Collaboration.Activity;

namespace Notrelix.Domain.Tests.Collaboration;

public class ActivityMetadataTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var data = JsonValue.Create("{\"key\":\"value\"}");
        var metadata = ActivityMetadata.Create(data);

        metadata.Data.Should().Be(data);
    }

    [Fact]
    public void Create_WithNull_ShouldThrow()
    {
        var act = () => ActivityMetadata.Create(null!);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Empty_ShouldReturnEmptyObject()
    {
        var metadata = ActivityMetadata.Empty();

        metadata.Data.Should().Be(JsonValue.EmptyObject());
    }

    [Fact]
    public void Equality_SameData_ShouldBeEqual()
    {
        var m1 = ActivityMetadata.Create(JsonValue.Create("{\"x\":1}"));
        var m2 = ActivityMetadata.Create(JsonValue.Create("{\"x\":1}"));

        m1.Should().Be(m2);
    }

    [Fact]
    public void Equality_DifferentData_ShouldNotBeEqual()
    {
        var m1 = ActivityMetadata.Create(JsonValue.Create("{\"x\":1}"));
        var m2 = ActivityMetadata.Create(JsonValue.Create("{\"x\":2}"));

        m1.Should().NotBe(m2);
    }
}
