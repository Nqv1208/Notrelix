using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Billing.Public.Facts;
using Notrelix.Domain.Automation.RulesEngine;

namespace Notrelix.Application.Features.Automation.Rules.Commands.CreateAutomationRule;

public record CreateAutomationRuleCommand(
    Guid WorkspaceId,
    string Name,
    string TriggerEvent,
    string ActionType,
    string Configuration) : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageWorkspaceSettings;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("automation.rule"), WorkspaceId, WorkspaceId);
}

public class CreateAutomationRuleCommandHandler : IRequestHandler<CreateAutomationRuleCommand, Result<Guid>>
{
    private readonly IAutomationDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IBillingCapabilityFacts _billingCapabilityFacts;

    public CreateAutomationRuleCommandHandler(
        IAutomationDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IBillingCapabilityFacts billingCapabilityFacts)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _billingCapabilityFacts = billingCapabilityFacts;
    }

    public async Task<Result<Guid>> Handle(CreateAutomationRuleCommand request, CancellationToken cancellationToken)
    {
        // Billing owns the commercial decision: the consumer asks whether the
        // capability is available, never what plan the account is on.
        var capability = await _billingCapabilityFacts.GetCapabilityAsync(
            _requestContext.RequireAccountId(),
            request.WorkspaceId,
            BillingCapabilityCode.AutomationRule,
            requestedAmount: 1,
            cancellationToken);

        if (capability is null || !capability.IsAvailable)
            return Result<Guid>.Failure(
                "The automation rule limit for this workspace's plan has been reached.");

        var trigger = AutomationTriggerDefinition.Create(request.TriggerEvent, request.Configuration);
        var action = AutomationActionDefinition.Create(request.ActionType, request.Configuration);
        var config = AutomationConfiguration.Create(trigger, action);

        var rule = AutomationRule.Create(
            _requestContext.RequireAccountId(),
            request.WorkspaceId,
            request.Name,
            config,
            _requestContext.UserId,
            _dateTimeProvider.UtcNow);

        _context.AutomationRules.Add(rule);

        return Result<Guid>.Success(rule.Id);
    }
}
