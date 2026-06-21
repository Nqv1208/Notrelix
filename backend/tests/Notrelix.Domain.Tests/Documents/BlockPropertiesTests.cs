using FluentAssertions;
using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.SharedKernel;
using Xunit;

namespace Notrelix.Domain.Tests.Documents;

public class BlockPropertiesTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var data = JsonValue.Create("{\"align\":\"left\"}");
        var props = BlockProperties.Create(data);
        props.Data.Should().Be(data);
    }

    [Fact]
    public void Create_WithNull_ShouldThrow()
    {
        var act = () => BlockProperties.Create(null!);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Empty_ShouldReturnEmptyObject()
    {
        var props = BlockProperties.Empty();
        props.Data.Value.Should().Be("{}");
    }

    [Fact]
    public void Equality_SameData_ShouldBeEqual()
    {
        var data = JsonValue.EmptyObject();
        var p1 = BlockProperties.Create(data);
        var p2 = BlockProperties.Create(data);

        p1.Should().Be(p2);
    }

    [Fact]
    public void Equality_DifferentData_ShouldNotBeEqual()
    {
        var p1 = BlockProperties.Create(JsonValue.Create("{\"a\":1}"));
        var p2 = BlockProperties.Create(JsonValue.Create("{\"a\":2}"));

        p1.Should().NotBe(p2);
    }
}
