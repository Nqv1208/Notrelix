using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Automation.Rules.Services;

namespace Notrelix.Application.Features.Automation.Rules.Commands.SetAutomationRuleEnabled;

public record SetAutomationRuleEnabledCommand(Guid AutomationRuleId, bool IsEnabled, Guid? BoardId = null) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IRequireAdditionalPermission
{
    // AU-6: a caller that manages a board names it; the pipeline authorizes ManageBoard on that
    // board and the handler proves the rule belongs to it. Without a board the rule is authorized
    // as workspace settings (Owners and Admins).
    public ResourceRef Resource => BoardId is { } board
        ? ResourceRef.Create(ResourceKind.Create("work-management.board"), board)
        : ResourceRef.Create(ResourceKind.Create("automation.rule"), AutomationRuleId);
    public PermissionAction Action => BoardId is null ? PermissionAction.ManageWorkspaceSettings : PermissionAction.ManageBoard;

    /// <summary>
    /// AUT-020: enabling makes a Rule executable, and the request cannot know the
    /// Rule's action before the pipeline runs, so enabling also needs
    /// ManageIntegrations (a Webhook Rule reaches the n8n integration). Disabling
    /// stays a kill switch under ManageWorkspaceSettings alone. Pipeline
    /// authorization only; there is no handler role check.
    /// </summary>
    public IReadOnlyList<string> AdditionalActionNames =>
        IsEnabled && BoardId is null ? [nameof(PermissionAction.ManageIntegrations)] : [];
}

public class SetAutomationRuleEnabledCommandHandler : IRequestHandler<SetAutomationRuleEnabledCommand, Result>
{
    private readonly IAutomationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly AutomationExecutorAuthority _executorAuthority;

    public SetAutomationRuleEnabledCommandHandler(
        IAutomationDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        AutomationExecutorAuthority executorAuthority)
    {
        _executorAuthority = executorAuthority;
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(SetAutomationRuleEnabledCommand request, CancellationToken cancellationToken)
    {
        var rule = await _context.AutomationRules
            .FirstOrDefaultAsync(item => item.Id == request.AutomationRuleId, cancellationToken);
        if (rule is null || rule.IsRemoved) throw new NotFoundException(nameof(AutomationRule), request.AutomationRuleId);

        // Fail closed: authority was proven on the named board, so the rule must be that board's.
        if (request.BoardId is { } claimedBoard && rule.BoardId != claimedBoard)
            throw new NotFoundException(nameof(AutomationRule), request.AutomationRuleId);

        // An integration action (Webhook, Slack) reaches an external system, which needs
        // ManageIntegrations. The board path does not check it, so it never enables such a rule,
        // whatever the request says: the scope and the actions come from the stored rule.
        if (request.BoardId is not null && AutomationActionKinds.IsIntegration(rule.Configuration.Action.Type))
            throw new ForbiddenException("Enabling a rule with an integration action needs permission to manage integrations.");

        if (request.IsEnabled)
        {
            // AUT-020: enabling makes the rule executable as its bound executor, so
            // that executor must hold the action's permission on the target now.
            // A rule without an executor is rejected by the Domain on Enable.
            if (rule.ExecutorUserId is { } executor && executor != Guid.Empty)
            {
                await _executorAuthority.EnsureCanExecuteAsync(
                    rule.AccountId,
                    rule.WorkspaceId,
                    executor,
                    rule.Configuration.Action,
                    cancellationToken,
                    rule.BoardId);
            }

            rule.Enable(_currentUser.UserId, _dateTimeProvider.UtcNow);
        }
        else rule.Disable(_currentUser.UserId, _dateTimeProvider.UtcNow);

        return Result.Success();
    }
}
