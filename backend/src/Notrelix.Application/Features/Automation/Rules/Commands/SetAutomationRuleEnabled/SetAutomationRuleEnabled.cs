using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Automation.Abstractions;

namespace Notrelix.Application.Features.Automation.Rules.Commands.SetAutomationRuleEnabled;

public record SetAutomationRuleEnabledCommand(Guid AutomationRuleId, bool IsEnabled) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IRequireAdditionalPermission
{
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("automation.rule"), AutomationRuleId);
    public PermissionAction Action => PermissionAction.ManageWorkspaceSettings;

    /// <summary>
    /// AUT-020: enabling makes a Rule executable, and the request cannot know the
    /// Rule's action before the pipeline runs, so enabling also needs
    /// ManageIntegrations (a Webhook Rule reaches the n8n integration). Disabling
    /// stays a kill switch under ManageWorkspaceSettings alone. Pipeline
    /// authorization only; there is no handler role check.
    /// </summary>
    public IReadOnlyList<string> AdditionalActionNames =>
        IsEnabled ? [nameof(PermissionAction.ManageIntegrations)] : [];
}

public class SetAutomationRuleEnabledCommandHandler : IRequestHandler<SetAutomationRuleEnabledCommand, Result>
{
    private readonly IAutomationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SetAutomationRuleEnabledCommandHandler(
        IAutomationDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(SetAutomationRuleEnabledCommand request, CancellationToken cancellationToken)
    {
        var rule = await _context.AutomationRules
            .FirstOrDefaultAsync(item => item.Id == request.AutomationRuleId, cancellationToken);
        if (rule is null) throw new NotFoundException(nameof(AutomationRule), request.AutomationRuleId);

        if (request.IsEnabled) rule.Enable(_currentUser.UserId, _dateTimeProvider.UtcNow);
        else rule.Disable(_currentUser.UserId, _dateTimeProvider.UtcNow);

        return Result.Success();
    }
}
