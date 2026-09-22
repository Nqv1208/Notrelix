using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Billing.Public.Capacity;
using Notrelix.Application.Features.Billing.Public.Facts;
using Notrelix.Domain.Automation.RulesEngine;

namespace Notrelix.Application.Features.Automation.Rules.Commands.CreateAutomationRule;

[IdempotencyOperation("automation.rules.create.v1")]
public record CreateAutomationRuleCommand(
    Guid WorkspaceId,
    string Name,
    string TriggerEvent,
    string ActionType,
    string Configuration) : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IIdempotentRequest
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
    private readonly IBillingCapacityActions _billingCapacityActions;

    public CreateAutomationRuleCommandHandler(
        IAutomationDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IBillingCapabilityFacts billingCapabilityFacts,
        IBillingCapacityActions billingCapacityActions)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _billingCapabilityFacts = billingCapabilityFacts;
        _billingCapacityActions = billingCapacityActions;
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

        // BOUND-TX-003: the capacity consume and the rule create share the
        // request transaction (same ApplicationDbContext, one SaveChanges owned
        // by the data session). If the capacity slot is lost to a concurrent
        // request the whole mutation rolls back; no separate compensation is
        // needed. The rule identity is the stable logical operation id, so a
        // retry after a partial infrastructure failure never double-consumes.
        await _billingCapacityActions.ConsumeAsync(
            new ConsumeCapacityRequest(new BillingCapacityOperationIdentity(
                AccountId: _requestContext.RequireAccountId(),
                WorkspaceId: request.WorkspaceId,
                CapabilityCode: BillingCapabilityCode.AutomationRule,
                Amount: 1,
                LogicalOperationId: rule.Id,
                SourceResource: rule.Id.ToString(),
                ActorUserId: _requestContext.UserId,
                OccurredAt: _dateTimeProvider.UtcNow)),
            cancellationToken);

        return Result<Guid>.Success(rule.Id);
    }
}
