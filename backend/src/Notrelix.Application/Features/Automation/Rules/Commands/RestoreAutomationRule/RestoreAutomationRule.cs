using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Automation.Executions.Queries.GetAutomationExecutions;

namespace Notrelix.Application.Features.Automation.Rules.Commands.RestoreAutomationRule;

/// <summary>
/// UC-AUT-A07: restores a rule deleted less than 90 days ago. It comes back Disabled, never auto-enabled,
/// and takes no plan slot. Restoring a live rule is a no-op. Authorization follows delete: a caller managing
/// a board names it (ManageBoard there, and the rule must be that board's); otherwise workspace settings.
/// </summary>
[IdempotencyOperation("automation.rules.restore.v1")]
public record RestoreAutomationRuleCommand(Guid AutomationRuleId, Guid? BoardId = null)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public ResourceRef Resource => BoardId is { } board
        ? ResourceRef.Create(ResourceKind.Create("work-management.board"), board)
        : ResourceRef.Create(ResourceKind.Create("automation.rule"), AutomationRuleId);
    public PermissionAction Action => BoardId is null ? PermissionAction.ManageAutomation : PermissionAction.ManageBoard;
}

public class RestoreAutomationRuleCommandHandler : IRequestHandler<RestoreAutomationRuleCommand, Result>
{
    private readonly IAutomationDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _clock;

    public RestoreAutomationRuleCommandHandler(
        IAutomationDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider clock)
    {
        _context = context;
        _requestContext = requestContext;
        _clock = clock;
    }

    public async Task<Result> Handle(RestoreAutomationRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await _context.AutomationRules
            .FirstOrDefaultAsync(item => item.Id == request.AutomationRuleId, cancellationToken);
        if (rule is null) throw new NotFoundException(nameof(AutomationRule), request.AutomationRuleId);

        if (request.BoardId is { } claimedBoard && rule.BoardId != claimedBoard)
            throw new NotFoundException(nameof(AutomationRule), request.AutomationRuleId);

        // A live rule has nothing to restore.
        if (!rule.IsRemoved) return Result.Success();

        // The deleted rule's history and restore window end together after the retention period.
        var now = _clock.UtcNow;
        if (rule.UpdatedAt < now - GetAutomationExecutionsQueryHandler.DeletedRuleRetention)
            throw new NotFoundException(nameof(AutomationRule), request.AutomationRuleId);

        rule.RestoreRemoved(_requestContext.UserId, now);
        return Result.Success();
    }
}
