using FluentAssertions;
using Notrelix.Domain.Automation;
using Notrelix.Domain.Automation.RulesEngine;

namespace Notrelix.Domain.Tests.Automation;

public class AutomationSecretFieldGuardTests
{
    private static Action ValidateAction(string type, string configuration) =>
        () => AutomationActionValidator.Validate(AutomationActionDefinition.Create(type, configuration));

    private static Action ValidateTrigger(string type, string configuration) =>
        () => AutomationTriggerValidator.Validate(AutomationTriggerDefinition.Create(type, configuration));

    [Theory]
    [InlineData("""{"channel":"general","apiKey":"abc123"}""")]
    [InlineData("""{"channel":"general","api_key":"abc123"}""")]
    [InlineData("""{"channel":"general","API-KEY":"abc123"}""")]
    [InlineData("""{"channel":"general","password":"hunter2"}""")]
    [InlineData("""{"channel":"general","Passwd":"hunter2"}""")]
    [InlineData("""{"channel":"general","clientSecret":"s"}""")]
    [InlineData("""{"channel":"general","botToken":"xoxb-1"}""")]
    [InlineData("""{"channel":"general","accessToken":"ya29"}""")]
    [InlineData("""{"channel":"general","refresh_token":"1//x"}""")]
    [InlineData("""{"channel":"general","authorization":"Basic Zm9vOmJhcg=="}""")]
    [InlineData("""{"channel":"general","Authorization":"anything"}""")]
    [InlineData("""{"channel":"general","bearer":"abc"}""")]
    [InlineData("""{"channel":"general","privateKey":"-----BEGIN"}""")]
    [InlineData("""{"channel":"general","header":"Bearer abc.def.ghi"}""")]
    [InlineData("""{"channel":"general","header":"  bearer abc"}""")]
    [InlineData("""{"channel":"general","nested":{"auth":{"Password":"x"}}}""")]
    [InlineData("""{"channel":"general","list":[{"ok":1},{"apiKey":"x"}]}""")]
    public void Validate_Action_WithSecretLikeFieldOrValue_ShouldThrowSecretFieldNotAllowed(string configuration)
    {
        ValidateAction("SlackMessage", configuration)
            .Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(AutomationRuleCodes.Automation_Configuration_SecretFieldNotAllowed);
    }

    [Fact]
    public void Validate_Webhook_WithSecretField_ShouldThrowSecretFieldNotAllowedBeforePathCheck()
    {
        ValidateAction("Webhook", """{"webhookPath":"hook","apiKey":"abc"}""")
            .Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(AutomationRuleCodes.Automation_Configuration_SecretFieldNotAllowed);
    }

    [Theory]
    [InlineData("""{"cron":"0 9 * * *","apiKey":"abc"}""")]
    [InlineData("""{"fieldId":"f1","token":"abc"}""")]
    [InlineData("""{"groupId":"g1","password":"abc"}""")]
    public void Validate_Trigger_WithSecretLikeField_ShouldThrowSecretFieldNotAllowed(string configuration)
    {
        var type = configuration.Contains("cron") ? "ScheduleTrigger" : configuration.Contains("fieldId") ? "FieldChanged" : "ItemMovedToGroup";
        ValidateTrigger(type, configuration)
            .Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(AutomationRuleCodes.Automation_Configuration_SecretFieldNotAllowed);
    }

    [Theory]
    [InlineData("""{"webhookPath":"notrelix-card_assigned"}""")]
    [InlineData("""{"channel":"general","message":"rotate your password soon"}""")]
    [InlineData("""{"fieldId":"f1","value":"plain text"}""")]
    [InlineData("""{"targetGroupId":"4a1f0c2e-1b7d-4f0e-9c1a-2d3e4f5a6b7c"}""")]
    [InlineData("""{"userId":"4a1f0c2e-1b7d-4f0e-9c1a-2d3e4f5a6b7c","optionId":"4a1f0c2e-1b7d-4f0e-9c1a-2d3e4f5a6b7c"}""")]
    [InlineData("""{"templateId":"tpl_1","subject":"Hello","connectionId":"c1"}""")]
    public void Validate_Action_WithNormalNames_ShouldNotThrow(string configuration)
    {
        var type = configuration.Contains("webhookPath") ? "Webhook"
            : configuration.Contains("targetGroupId") ? "MoveItem"
            : configuration.Contains("optionId") ? "ChangeStatus"
            : configuration.Contains("templateId") ? "SendEmail"
            : configuration.Contains("fieldId") ? "UpdateField"
            : "SlackMessage";
        ValidateAction(type, configuration).Should().NotThrow();
    }

    [Theory]
    [InlineData("""{"cron":"0 9 * * *"}""")]
    [InlineData("""{"groupId":"g1"}""")]
    [InlineData("""{"fieldId":"status"}""")]
    public void Validate_Trigger_WithNormalNames_ShouldNotThrow(string configuration)
    {
        var type = configuration.Contains("cron") ? "ScheduleTrigger"
            : configuration.Contains("fieldId") ? "FieldChanged"
            : "ItemMovedToGroup";
        ValidateTrigger(type, configuration).Should().NotThrow();
    }

    [Fact]
    public void Ensure_InvalidJson_LeavesReportingToTheTypeValidator()
    {
        var act = () => AutomationSecretFieldGuard.Ensure("{not json");
        act.Should().NotThrow();
    }

    [Fact]
    public void Ensure_NullOrBlank_ShouldNotThrow()
    {
        AutomationSecretFieldGuard.Ensure(null);
        AutomationSecretFieldGuard.Ensure("   ");
    }
}
