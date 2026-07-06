using Notrelix.Domain.Automation.RulesEngine;

namespace Notrelix.Application.Features.Automation.Rules.Commands.CreateAutomationRule;

public class CreateAutomationRuleCommandValidator : AbstractValidator<CreateAutomationRuleCommand>
{
    public CreateAutomationRuleCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.TriggerEvent).NotEmpty().MaximumLength(120);
        RuleFor(x => x.ActionType).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Configuration).NotEmpty();
        RuleFor(x => x)
            .Must(x =>
            {
                try
                {
                    var trigger = AutomationTriggerDefinition.Create(x.TriggerEvent, x.Configuration);
                    var action = AutomationActionDefinition.Create(x.ActionType, x.Configuration);
                    AutomationConfiguration.Create(trigger, action);
                    return true;
                }
                catch
                {
                    return false;
                }
            })
            .WithMessage("Invalid automation trigger and/or action configuration.");
    }
}
