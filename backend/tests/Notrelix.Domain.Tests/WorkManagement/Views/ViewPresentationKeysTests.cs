using FluentAssertions;
using Notrelix.Domain.WorkManagement;
using Notrelix.Domain.WorkManagement.Views;

namespace Notrelix.Domain.Tests.WorkManagement;

public class ViewPresentationKeysTests
{
    private static BoardViewConfig Config(string json) => BoardViewConfig.Create(JsonValue.Create(json));

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"calendarDateField\":null}")]
    [InlineData("{\"calendarDateField\":\"due\",\"timelineGroupBy\":\"status\",\"timelineBarSpan\":\"created-due\"}")]
    [InlineData("{\"calendarDateField\":\"0b2f6c1e-0000-4000-8000-000000000001\"}")]
    public void ValidKeys_AreKept(string json) =>
        Config(json).Data.Value.Should().Be(json);

    [Theory]
    [InlineData("{\"calendarDateField\":\"\"}")]
    [InlineData("{\"calendarDateField\":5}")]
    [InlineData("{\"timelineGroupBy\":\"owner\"}")]
    [InlineData("{\"timelineBarSpan\":\"due-only\"}")]
    public void BadKeys_AreRejected(string json)
    {
        var act = () => Config(json);

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_View_PresentationConfigInvalid);
    }
}
