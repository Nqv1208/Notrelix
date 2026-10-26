using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Billing.Public.Capacity;
using Notrelix.Application.Features.Billing.Public.Facts;

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
    public PermissionAction Action => BoardId is null ? PermissionAction.ManageWorkspaceSettings : PermissionAction.ManageBoard;
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
        rule.Remove(user, now);

        // The rule no longer counts against the plan's rule limit.
        await _capacity.ReleaseAsync(
            new ReleaseCapacityRequest(new BillingCapacityOperationIdentity(
                AccountId: rule.AccountId,
                WorkspaceId: rule.WorkspaceId,
                CapabilityCode: BillingCapabilityCode.AutomationRule,
                Amount: 1,
                LogicalOperationId: ReleaseOperationId(rule.Id),
                SourceResource: rule.Id.ToString(),
                ActorUserId: user,
                OccurredAt: now)),
            cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// The identity of the release: stable per rule (a retry never releases twice) and different
    /// from the rule's own id, which names the consume the rule's creation made.
    /// </summary>
    public static Guid ReleaseOperationId(Guid ruleId) =>
        new(System.Security.Cryptography.MD5.HashData(
            System.Text.Encoding.UTF8.GetBytes($"automation-rule-release:{ruleId:N}")));
}
