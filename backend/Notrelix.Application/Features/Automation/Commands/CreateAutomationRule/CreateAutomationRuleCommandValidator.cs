using FluentValidation;
using Notrelix.Application.Features.Integrations;

namespace Notrelix.Application.Features.Automation.Commands.CreateAutomationRule;

public class CreateAutomationRuleCommandValidator : AbstractValidator<CreateAutomationRuleCommand>
{
    public CreateAutomationRuleCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.TriggerEvent).NotEmpty().MaximumLength(120);
        RuleFor(x => x.ActionType).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Configuration).NotEmpty();
        RuleFor(x => x.Configuration)
            .Must((command, configuration) =>
                !string.Equals(command.ActionType, "n8n.webhook", StringComparison.OrdinalIgnoreCase) ||
                N8nAutomationConfiguration.TryGetWebhookPath(configuration, out _))
            .WithMessage("N8n webhook automations require configuration.webhookPath.");
    }
}
