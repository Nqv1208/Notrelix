using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Automation.Ports.WorkManagement;
using Notrelix.Application.Features.Automation.Rules.Services;
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
    string Configuration,
    Guid? BoardId = null,
    string? TriggerConfiguration = null) : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IRequireAdditionalPermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageWorkspaceSettings;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("automation.rule"), WorkspaceId, WorkspaceId);

    /// <summary>
    /// AUT-020: a Webhook action reaches the n8n integration, so creating such a
    /// Rule also needs ManageIntegrations. Pipeline authorization only.
    /// </summary>
    public IReadOnlyList<string> AdditionalActionNames =>
        string.Equals(ActionType, "Webhook", StringComparison.Ordinal)
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

    private static bool TryReadTriggerGroup(string? configuration, out Guid groupId)
    {
        groupId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(configuration)) return false;
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(configuration);
            return document.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object
                && document.RootElement.TryGetProperty("groupId", out var group)
                && group.ValueKind == System.Text.Json.JsonValueKind.String
                && Guid.TryParse(group.GetString(), out groupId);
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }

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

        // AU-2: a board-scoped rule has its own trigger filter; a rule without a board keeps
        // the single configuration both parts read.
        var trigger = AutomationTriggerDefinition.Create(
            request.TriggerEvent,
            request.BoardId is null ? request.Configuration : request.TriggerConfiguration);
        var action = AutomationActionDefinition.Create(request.ActionType, request.Configuration);

        // AUT-028: a Webhook rule's path is allow-listed when the rule is created,
        // not only when it is enabled (the Domain rejects url and unsafe paths).
        if (string.Equals(action.Type, "Webhook", StringComparison.OrdinalIgnoreCase))
        {
            AutomationActionValidator.Validate(action);
        }

        var config = AutomationConfiguration.Create(trigger, action);

        // AUT-020: the rule runs as its creator, so the creator must hold the
        // action's permission on the target now (a Private board the creator
        // cannot see is rejected here, never bound).
        await _executorAuthority.EnsureCanExecuteAsync(
            _requestContext.RequireAccountId(),
            request.WorkspaceId,
            _requestContext.UserId,
            action,
            cancellationToken,
            request.BoardId);

        // The trigger filter's group must be on the rule's board too.
        if (request.BoardId is { } boardId
            && TryReadTriggerGroup(request.TriggerConfiguration, out var triggerGroupId))
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
