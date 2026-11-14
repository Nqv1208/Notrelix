using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Automation.Ports.WorkManagement;
using Notrelix.Application.Features.Automation.Rules.Services;

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
    public PermissionAction Action => BoardId is null ? PermissionAction.ManageAutomation : PermissionAction.ManageBoard;
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
    private readonly AutomationExecutorAuthority _executorAuthority;
    private readonly IWorkActionPort _workActions;

    public CreateAutomationRuleCommandHandler(
        IAutomationDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        AutomationExecutorAuthority executorAuthority,
        IWorkActionPort workActions)
    {
        _workActions = workActions;
        _executorAuthority = executorAuthority;
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateAutomationRuleCommand request, CancellationToken cancellationToken)
    {
        // A Draft holds no plan slot (only enabled rules count); the slot is consumed on enable.
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

        return Result<Guid>.Success(rule.Id);
    }
}
