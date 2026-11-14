using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Billing.Public.Capacity;
using Notrelix.Application.Features.Automation.Rules.Services;

namespace Notrelix.Application.Features.Automation.Rules.Commands.DeleteAutomationRule;

/// <summary>
/// Deleting a rule stops it from triggering and removes it from the lists. Executions already queued
/// or running finish on the version they started with; the history stays readable for a while.
/// </summary>
[IdempotencyOperation("automation.rules.delete.v1")]
public record DeleteAutomationRuleCommand(Guid AutomationRuleId, Guid? BoardId = null)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public ResourceRef Resource => BoardId is { } board
        ? ResourceRef.Create(ResourceKind.Create("work-management.board"), board)
        : ResourceRef.Create(ResourceKind.Create("automation.rule"), AutomationRuleId);
    public PermissionAction Action => BoardId is null ? PermissionAction.ManageAutomation : PermissionAction.ManageBoard;
}

public class DeleteAutomationRuleCommandHandler : IRequestHandler<DeleteAutomationRuleCommand, Result>
{
    private readonly IAutomationDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _clock;
    private readonly IBillingCapacityActions _capacity;

    public DeleteAutomationRuleCommandHandler(
        IAutomationDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider clock,
        IBillingCapacityActions capacity)
    {
        _context = context;
        _requestContext = requestContext;
        _clock = clock;
        _capacity = capacity;
    }

    public async Task<Result> Handle(DeleteAutomationRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await _context.AutomationRules
            .FirstOrDefaultAsync(item => item.Id == request.AutomationRuleId, cancellationToken);
        if (rule is null) throw new NotFoundException(nameof(AutomationRule), request.AutomationRuleId);

        if (request.BoardId is { } claimedBoard && rule.BoardId != claimedBoard)
            throw new NotFoundException(nameof(AutomationRule), request.AutomationRuleId);

        var now = _clock.UtcNow;
        var user = _requestContext.UserId;
        if (rule.IsRemoved) throw new NotFoundException(nameof(AutomationRule), request.AutomationRuleId);
        var wasHoldingSlot = AutomationRuleCapacity.HoldsSlot(rule);
        var fromVersion = rule.Version;
        rule.Remove(user, now);

        // Only a rule that holds a slot (enabled, or auto-disabled and still holding it) releases one.
        if (wasHoldingSlot)
            await _capacity.ReleaseAsync(
                new ReleaseCapacityRequest(AutomationRuleCapacity.Identity(rule, "delete", fromVersion, user, now)),
                cancellationToken);

        return Result.Success();
    }
}
