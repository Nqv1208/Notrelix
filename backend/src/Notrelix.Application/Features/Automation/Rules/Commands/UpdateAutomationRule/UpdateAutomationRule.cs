using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Automation.Rules.Services;

namespace Notrelix.Application.Features.Automation.Rules.Commands.UpdateAutomationRule;

/// <summary>
/// Edits a rule: the new definition is a new version of the rule. Executions already queued or
/// running finish on the version they started with. The board is never changed by an edit.
/// Authorization follows the other rule commands: a caller managing a board names it (ManageBoard
/// there, and the stored rule must be that board's); otherwise workspace settings.
/// </summary>
[IdempotencyOperation("automation.rules.update.v1")]
public record UpdateAutomationRuleCommand(
    Guid AutomationRuleId,
    string Name,
    string TriggerEvent,
    string? TriggerConfiguration,
    IReadOnlyList<RuleActionInput> Actions,
    string? Conditions = null,
    Guid? BoardId = null) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IRequireAdditionalPermission, IIdempotentRequest
{
    public ResourceRef Resource => BoardId is { } board
        ? ResourceRef.Create(ResourceKind.Create("work-management.board"), board)
        : ResourceRef.Create(ResourceKind.Create("automation.rule"), AutomationRuleId);
    public PermissionAction Action => BoardId is null ? PermissionAction.ManageAutomation : PermissionAction.ManageBoard;

    /// <summary>An integration action (Webhook, Slack) needs ManageIntegrations on the workspace path.</summary>
    public IReadOnlyList<string> AdditionalActionNames =>
        BoardId is null && Actions.Any(a => AutomationActionKinds.IsIntegration(a.Type))
            ? [nameof(PermissionAction.ManageIntegrations)]
            : [];
}

public class UpdateAutomationRuleCommandHandler : IRequestHandler<UpdateAutomationRuleCommand, Result>
{
    private readonly IAutomationDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _clock;
    private readonly AutomationExecutorAuthority _authority;
    private readonly Ports.WorkManagement.IWorkActionPort _workActions;

    public UpdateAutomationRuleCommandHandler(
        IAutomationDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider clock,
        AutomationExecutorAuthority authority,
        Ports.WorkManagement.IWorkActionPort workActions)
    {
        _context = context;
        _requestContext = requestContext;
        _clock = clock;
        _authority = authority;
        _workActions = workActions;
    }

    public async Task<Result> Handle(UpdateAutomationRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await _context.AutomationRules
            .FirstOrDefaultAsync(item => item.Id == request.AutomationRuleId, cancellationToken);
        if (rule is null || rule.IsRemoved) throw new NotFoundException(nameof(AutomationRule), request.AutomationRuleId);

        // Fail closed: authority was proven on the named board, so the rule must be that board's,
        // and the board path never touches a rule that reaches an integration.
        if (request.BoardId is { } claimedBoard)
        {
            if (rule.BoardId != claimedBoard)
                throw new NotFoundException(nameof(AutomationRule), request.AutomationRuleId);
            if (rule.Configuration.Actions.Any(a => AutomationActionKinds.IsIntegration(a.Type)))
                throw new ForbiddenException("Editing a rule with an integration action needs permission to manage integrations.");
        }

        var configuration = AutomationRuleAssembler.Assemble(
            request.TriggerEvent, request.TriggerConfiguration, rule.BoardId, request.Actions, request.Conditions);

        // The editor becomes the executor, so the editor must hold every action's permission now.
        var accountId = _requestContext.RequireAccountId();
        await _authority.EnsureRuleAsync(
            accountId, rule.WorkspaceId, _requestContext.UserId, configuration, rule.BoardId, cancellationToken);

        if (rule.BoardId is { } boardId
            && AutomationTriggerFilter.TryReadGroup(request.TriggerConfiguration, out var triggerGroupId))
        {
            await _workActions.EnsureBoardGroupAsync(
                boardId,
                triggerGroupId,
                new Ports.WorkManagement.AutomationPrincipal(accountId, _requestContext.UserId, rule.WorkspaceId),
                cancellationToken);
        }

        rule.Revise(request.Name, configuration, _requestContext.UserId, _clock.UtcNow);
        return Result.Success();
    }
}
