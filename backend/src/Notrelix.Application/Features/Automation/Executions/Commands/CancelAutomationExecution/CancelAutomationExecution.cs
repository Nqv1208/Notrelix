using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Automation.Abstractions;

namespace Notrelix.Application.Features.Automation.Executions.Commands.CancelAutomationExecution;

/// <summary>
/// UC-AUT-H04: a person cancels an execution that is still Queued. It ends Cancelled and no action runs.
/// A running or finished execution is refused. Authorization follows the other rule commands: a caller
/// managing a board names it (ManageBoard there, and the rule must be that board's); otherwise workspace settings.
/// </summary>
[IdempotencyOperation("automation.executions.cancel.v1")]
public record CancelAutomationExecutionCommand(Guid AutomationRuleId, Guid AutomationExecutionId, Guid? BoardId = null)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public ResourceRef Resource => BoardId is { } board
        ? ResourceRef.Create(ResourceKind.Create("work-management.board"), board)
        : ResourceRef.Create(ResourceKind.Create("automation.rule"), AutomationRuleId);
    public PermissionAction Action => BoardId is null ? PermissionAction.ManageAutomation : PermissionAction.ManageBoard;
}

public class CancelAutomationExecutionCommandHandler : IRequestHandler<CancelAutomationExecutionCommand, Result>
{
    private readonly IAutomationDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _clock;

    public CancelAutomationExecutionCommandHandler(
        IAutomationDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider clock)
    {
        _context = context;
        _requestContext = requestContext;
        _clock = clock;
    }

    public async Task<Result> Handle(CancelAutomationExecutionCommand request, CancellationToken cancellationToken)
    {
        var rule = await _context.AutomationRules.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == request.AutomationRuleId, cancellationToken);
        if (rule is null) throw new NotFoundException(nameof(AutomationRule), request.AutomationRuleId);

        // Fail closed: authority was proven on the named board, so the rule must be that board's.
        if (request.BoardId is { } claimedBoard && rule.BoardId != claimedBoard)
            throw new NotFoundException(nameof(AutomationRule), request.AutomationRuleId);

        var execution = await _context.AutomationExecutions
            .FirstOrDefaultAsync(item => item.Id == request.AutomationExecutionId, cancellationToken);
        if (execution is null || execution.RuleId != request.AutomationRuleId)
            throw new NotFoundException(nameof(AutomationExecution), request.AutomationExecutionId);

        execution.CancelBeforeStart(_requestContext.UserId, _clock.UtcNow);
        return Result.Success();
    }
}
