using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Automation.Ports.WorkManagement;
using Notrelix.Application.Features.Automation.Rules.Services;
using Notrelix.Application.Features.Billing.Public.Capacity;
using Notrelix.Application.Features.Billing.Public.Facts;

namespace Notrelix.Application.Features.Automation.Rules.Commands.CreateAutomationRule;

[IdempotencyOperation("automation.rules.create.v1")]
public record CreateAutomationRuleCommand(
    Guid WorkspaceId,
    string Name,
    string TriggerEvent,
    string ActionType,
    string Configuration,
    Guid? BoardId = null,
    string? TriggerConfiguration = null,
    IReadOnlyList<RuleActionInput>? Actions = null,
    string? Conditions = null) : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IRequireAdditionalPermission, IIdempotentRequest
{
    // Board-scoped rules are managed by the board's Managers (and workspace Owners/Admins, who are
    // Managers everywhere); a rule without a board stays workspace settings.
    public PermissionAction Action => BoardId is null ? PermissionAction.ManageWorkspaceSettings : PermissionAction.ManageBoard;
    public ResourceRef Resource => BoardId is { } board
        ? ResourceRef.Create(ResourceKind.Create("work-management.board"), board, WorkspaceId)
        : ResourceRef.Create(ResourceKind.Create("automation.rule"), WorkspaceId, WorkspaceId);

    /// <summary>
    /// AUT-020: a Webhook action reaches the n8n integration, so creating such a
    /// Rule also needs ManageIntegrations. Pipeline authorization only.
    /// </summary>
    public IReadOnlyList<string> AdditionalActionNames =>
        BoardId is null && (Actions is { Count: > 0 }
            ? Actions.Any(a => string.Equals(a.Type, "Webhook", StringComparison.Ordinal))
            : string.Equals(ActionType, "Webhook", StringComparison.Ordinal))
            ? [nameof(PermissionAction.ManageIntegrations)]
            : [];
}

public class CreateAutomationRuleCommandHandler : IRequestHandler<CreateAutomationRuleCommand, Result<Guid>>
{
    private readonly IAutomationDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IBillingCapabilityFacts _billingCapabilityFacts;
    private readonly IBillingCapacityActions _billingCapacityActions;
    private readonly AutomationExecutorAuthority _executorAuthority;
    private readonly IWorkActionPort _workActions;

    public CreateAutomationRuleCommandHandler(
        IAutomationDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IBillingCapabilityFacts billingCapabilityFacts,
        IBillingCapacityActions billingCapacityActions,
        AutomationExecutorAuthority executorAuthority,
        IWorkActionPort workActions)
    {
        _workActions = workActions;
        _executorAuthority = executorAuthority;
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

        // One trigger, optional AND conditions and up to five ordered actions (no branching).
        IReadOnlyList<RuleActionInput> actions = request.Actions is { Count: > 0 }
            ? request.Actions
            : [new RuleActionInput(request.ActionType, request.Configuration)];
        var config = AutomationRuleAssembler.Assemble(
            request.TriggerEvent, request.TriggerConfiguration, request.BoardId, actions, request.Conditions);

        // AUT-020: the rule runs as its creator, so the creator must hold every action's
        // permission on its targets now (a Private board the creator cannot see is rejected
        // here, never bound), and every condition must name things of the rule's board.
        await _executorAuthority.EnsureRuleAsync(
            _requestContext.RequireAccountId(),
            request.WorkspaceId,
            _requestContext.UserId,
            config,
            request.BoardId,
            cancellationToken);

        // The trigger filter's group must be on the rule's board too.
        if (request.BoardId is { } boardId
            && AutomationTriggerFilter.TryReadGroup(request.TriggerConfiguration, out var triggerGroupId))
        {
            await _workActions.EnsureBoardGroupAsync(
                boardId,
                triggerGroupId,
                new AutomationPrincipal(_requestContext.RequireAccountId(), _requestContext.UserId, request.WorkspaceId),
                cancellationToken);
        }

        var rule = AutomationRule.Create(
            _requestContext.RequireAccountId(),
            request.WorkspaceId,
            request.Name,
            config,
            _requestContext.UserId,
            _dateTimeProvider.UtcNow,
            request.BoardId);

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
