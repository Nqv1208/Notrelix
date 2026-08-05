using FluentAssertions;
using Notrelix.Domain.Analytics.Widgets;

namespace Notrelix.Domain.Tests.Analytics.Dashboards;

public class WidgetConfigImmutabilityTests
{
    [Fact]
    public void Create_ShouldSetData()
    {
        var data = JsonValue.Create("""{"content":"hello"}""");

        var config = WidgetConfig.Create(data);

        config.Data.Should().Be(data);
    }

    [Fact]
    public void Create_WithNullData_ShouldThrow()
    {
        var act = () => WidgetConfig.Create(null!);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Config_ShouldBeImmutableValueObject()
    {
        var config = WidgetConfig.Create(JsonValue.Create("""{"content":"hello"}"""));

        config.Should().BeOfType<WidgetConfig>();
        config.Data.Should().NotBeNull();
    }

    [Fact]
    public void EqualConfigs_ShouldBeEqual()
    {
        var data = JsonValue.Create("""{"content":"hello"}""");

        var a = WidgetConfig.Create(data);
        var b = WidgetConfig.Create(data);

        a.Should().Be(b);
    }

    [Fact]
    public void DifferentData_ShouldNotBeEqual()
    {
        var a = WidgetConfig.Create(JsonValue.Create("""{"content":"hello"}"""));
        var b = WidgetConfig.Create(JsonValue.Create("""{"content":"world"}"""));

        a.Should().NotBe(b);
    }
}
