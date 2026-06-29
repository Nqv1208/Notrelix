using FluentAssertions;
using Notrelix.Domain.Automation.RulesEngine;

namespace Notrelix.Domain.Tests.Automation;

public class AutomationActionValidatorTests
{
    [Fact]
    public void Validate_Webhook_WithUrl_ShouldNotThrow()
    {
        var action = AutomationActionDefinition.Create("Webhook", "{\"url\":\"https://example.com\"}");
        var act = () => AutomationActionValidator.Validate(action);
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_Webhook_WithWebhookPath_ShouldNotThrow()
    {
        var action = AutomationActionDefinition.Create("Webhook", "{\"webhookPath\":\"/hooks/abc\"}");
        var act = () => AutomationActionValidator.Validate(action);
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_Webhook_WithoutUrlOrWebhookPath_ShouldThrow()
    {
        var action = AutomationActionDefinition.Create("Webhook", "{\"foo\":\"bar\"}");
        var act = () => AutomationActionValidator.Validate(action);
        act.Should().Throw<BusinessRuleException>().WithMessage("*'url'*'webhookPath'*");
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
