using FluentAssertions;
using Notrelix.Domain.Automation.RulesEngine;

namespace Notrelix.Domain.Tests.Automation;

public class AutomationActionValidatorTests
{
    private static Action ValidateWebhook(string configuration) =>
        () => AutomationActionValidator.Validate(AutomationActionDefinition.Create("Webhook", configuration));

    [Theory]
    [InlineData("""{"webhookPath":"hook"}""")]
    [InlineData("""{"webhookPath":"notrelix-card_assigned"}""")]
    [InlineData("""{"webhookPath":"team/hooks/abc-1"}""")]
    public void Validate_Webhook_WithValidWebhookPath_ShouldNotThrow(string configuration) =>
        ValidateWebhook(configuration).Should().NotThrow();

    [Fact]
    public void Validate_Webhook_UrlOnly_ShouldThrow() =>
        ValidateWebhook("""{"url":"https://example.com/hook"}""")
            .Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be("Automation_ActionValidator_InvalidWebhookJson");

    [Fact]
    public void Validate_Webhook_UrlWithValidPath_ShouldThrow() =>
        ValidateWebhook("""{"url":"https://example.com/hook","webhookPath":"hook"}""")
            .Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be("Automation_ActionValidator_InvalidWebhookJson");

    [Fact]
    public void Validate_Webhook_WithoutWebhookPath_ShouldThrow() =>
        ValidateWebhook("""{"foo":"bar"}""")
            .Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be("Automation_ActionValidator_InvalidWebhookJson");

    [Theory]
    [InlineData("")]
    [InlineData("/hook")]
    [InlineData("hook/")]
    [InlineData("a//b")]
    [InlineData("../admin")]
    [InlineData("a/../b")]
    [InlineData("a/./b")]
    [InlineData("%2e%2e/admin")]
    [InlineData("a%2fb")]
    [InlineData("hook?x=1")]
    [InlineData("hook#frag")]
    [InlineData("a\\b")]
    [InlineData("http://evil/hook")]
    [InlineData("host:5678/hook")]
    [InlineData("hook name")]
    public void Validate_Webhook_WithUnsafePath_ShouldThrow(string path)
    {
        var configuration = System.Text.Json.JsonSerializer.Serialize(new { webhookPath = path });
        ValidateWebhook(configuration).Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Validate_Webhook_WithNonStringPath_ShouldThrow() =>
        ValidateWebhook("""{"webhookPath":5}""").Should().Throw<BusinessRuleException>();

    [Fact]
    public void WebhookPath_EnforcesSegmentAndTotalLength()
    {
        AutomationWebhookPath.IsValid(new string('a', 64)).Should().BeTrue();
        AutomationWebhookPath.IsValid(new string('a', 65)).Should().BeFalse();
        AutomationWebhookPath.IsValid(string.Join('/', Enumerable.Repeat(new string('a', 64), 8))).Should().BeFalse("over the total length cap");
    }

    [Fact]
    public void Validate_SendEmail_WithTemplateId_ShouldNotThrow()
    {
        var action = AutomationActionDefinition.Create("SendEmail", "{\"templateId\":\"tpl_1\"}");
        var act = () => AutomationActionValidator.Validate(action);
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_SendEmail_WithSubject_ShouldNotThrow()
    {
        var action = AutomationActionDefinition.Create("SendEmail", "{\"subject\":\"Hello\"}");
        var act = () => AutomationActionValidator.Validate(action);
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_SendEmail_WithoutTemplateIdOrSubject_ShouldThrow()
    {
        var action = AutomationActionDefinition.Create("SendEmail", "{\"foo\":\"bar\"}");
        var act = () => AutomationActionValidator.Validate(action);
        act.Should().Throw<BusinessRuleException>().WithMessage("*'templateId'*'subject'*");
    }

    [Fact]
    public void Validate_SlackMessage_WithChannel_ShouldNotThrow()
    {
        var action = AutomationActionDefinition.Create("SlackMessage", "{\"channel\":\"#general\"}");
        var act = () => AutomationActionValidator.Validate(action);
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_SlackMessage_WithoutChannel_ShouldThrow()
    {
        var action = AutomationActionDefinition.Create("SlackMessage", "{\"foo\":\"bar\"}");
        var act = () => AutomationActionValidator.Validate(action);
        act.Should().Throw<BusinessRuleException>().WithMessage("*'channel'*");
    }

    [Fact]
    public void Validate_UpdateField_WithFieldIdAndValue_ShouldNotThrow()
    {
        var action = AutomationActionDefinition.Create("UpdateField", "{\"fieldId\":\"status\",\"value\":\"Done\"}");
        var act = () => AutomationActionValidator.Validate(action);
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_UpdateField_WithoutFieldId_ShouldThrow()
    {
        var action = AutomationActionDefinition.Create("UpdateField", "{\"value\":\"Done\"}");
        var act = () => AutomationActionValidator.Validate(action);
        act.Should().Throw<BusinessRuleException>().WithMessage("*'fieldId'*'value'*");
    }

    [Fact]
    public void Validate_UpdateField_WithoutValue_ShouldThrow()
    {
        var action = AutomationActionDefinition.Create("UpdateField", "{\"fieldId\":\"status\"}");
        var act = () => AutomationActionValidator.Validate(action);
        act.Should().Throw<BusinessRuleException>().WithMessage("*'fieldId'*'value'*");
    }

    [Fact]
    public void Validate_CreateItem_WithTargetGroupId_ShouldNotThrow()
    {
        var action = AutomationActionDefinition.Create("CreateItem", "{\"targetGroupId\":\"g1\"}");
        var act = () => AutomationActionValidator.Validate(action);
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_CreateItem_WithoutTargetGroupId_ShouldThrow()
    {
        var action = AutomationActionDefinition.Create("CreateItem", "{\"foo\":\"bar\"}");
        var act = () => AutomationActionValidator.Validate(action);
        act.Should().Throw<BusinessRuleException>().WithMessage("*'targetGroupId'*");
    }

    [Fact]
    public void Validate_MoveItem_WithTargetGroupId_ShouldNotThrow()
    {
        var action = AutomationActionDefinition.Create("MoveItem", "{\"targetGroupId\":\"g2\"}");
        var act = () => AutomationActionValidator.Validate(action);
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_MoveItem_WithoutTargetGroupId_ShouldThrow()
    {
        var action = AutomationActionDefinition.Create("MoveItem", "{\"foo\":\"bar\"}");
        var act = () => AutomationActionValidator.Validate(action);
        act.Should().Throw<BusinessRuleException>().WithMessage("*'targetGroupId'*");
    }

    [Fact]
    public void Validate_NotifyMember_WithUserId_ShouldNotThrow()
    {
        var action = AutomationActionDefinition.Create("NotifyMember", "{\"userId\":\"u1\"}");
        var act = () => AutomationActionValidator.Validate(action);
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_NotifyMember_WithTeamId_ShouldNotThrow()
    {
        var action = AutomationActionDefinition.Create("NotifyMember", "{\"teamId\":\"t1\"}");
        var act = () => AutomationActionValidator.Validate(action);
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_NotifyMember_WithoutUserIdOrTeamId_ShouldThrow()
    {
        var action = AutomationActionDefinition.Create("NotifyMember", "{\"foo\":\"bar\"}");
        var act = () => AutomationActionValidator.Validate(action);
        act.Should().Throw<BusinessRuleException>().WithMessage("*'userId'*'teamId'*");
    }

    [Fact]
    public void Validate_NonNullConfigOnNoConfigType_ShouldValidateJson()
    {
        var act = () => AutomationActionValidator.Validate(AutomationActionDefinition.Create("CreateItem", "not-json"));
        act.Should().Throw<BusinessRuleException>().WithMessage("*JSON*");
    }
}
