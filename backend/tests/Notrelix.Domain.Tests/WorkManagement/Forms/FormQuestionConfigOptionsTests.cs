using FluentAssertions;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.WorkManagement.Forms;

namespace Notrelix.Domain.Tests.WorkManagement.Forms;

public class FormQuestionConfigOptionsTests
{
    [Fact]
    public void FromConfig_ReadsOptions_ForChoiceQuestions()
    {
        var config = FormQuestionConfig.FromConfig(FormQuestionType.Select, "{\"options\":[{\"key\":\"a\",\"label\":\"A\"},{\"key\":\"b\",\"label\":\"B\"}]}");

        config.Options.Select(o => o.Key).Should().Equal("a", "b");
        config.Options[1].Label.Should().Be("B");
    }

    [Fact]
    public void FromConfig_Options_NotAllowedOnOtherTypes()
    {
        var act = () => FormQuestionConfig.FromConfig(FormQuestionType.ShortText, "{\"options\":[{\"key\":\"a\",\"label\":\"A\"}]}");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void FromConfig_Options_RefusesMoreThanFifty()
    {
        var many = string.Join(",", Enumerable.Range(0, 51).Select(i => $"{{\"key\":\"k{i}\",\"label\":\"L{i}\"}}"));
        var act = () => FormQuestionConfig.FromConfig(FormQuestionType.MultiSelect, "{\"options\":[" + many + "]}");
        act.Should().Throw<BusinessRuleException>();
    }

    [Theory]
    [InlineData("{\"options\":[{\"key\":\"a\",\"label\":\"A\"},{\"key\":\"A\",\"label\":\"B\"}]}")] // duplicate key
    [InlineData("{\"options\":[{\"key\":\"\",\"label\":\"A\"}]}")]                                     // empty key
    [InlineData("{\"options\":[{\"key\":\"a\",\"label\":\"\"}]}")]                                     // empty label
    public void FromConfig_Options_RefusesInvalidEntries(string json)
    {
        var act = () => FormQuestionConfig.FromConfig(FormQuestionType.Select, json);
        act.Should().Throw<BusinessRuleException>();
    }
}
