using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Billing.Abstractions;
using Notrelix.Application.Features.Billing.Plans.Queries.GetWorkspaceBillingPlan;
using Notrelix.Domain.Billing.Subscriptions;

namespace Notrelix.Application.Features.Billing.Plans.Commands.ChangeAccountPlan;

/// <summary>
/// Moves the workspace's account to another catalog plan. Development-only administrative
/// action (no payment provider yet): it is refused when the environment does not allow it and
/// it needs the account owner on top of the workspace permission. A downgrade never deletes
/// anything; usage above the new limit only blocks new consumption.
/// </summary>
public record ChangeAccountPlanCommand(Guid WorkspaceId, string PlanCode)
    : ICommand<Result<WorkspaceBillingPlanDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageWorkspaceSettings;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("billing.plan"), WorkspaceId, WorkspaceId);
}

public class ChangeAccountPlanCommandHandler
    : IRequestHandler<ChangeAccountPlanCommand, Result<WorkspaceBillingPlanDto>>
{
    private static readonly TimeSpan SubscriptionTerm = TimeSpan.FromDays(3650);

    private readonly IBillingDbContext _context;
    private readonly IBillingUsageReader _reader;
    private readonly IBillingPlanChangePolicy _policy;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _clock;

    public ChangeAccountPlanCommandHandler(
        IBillingDbContext context,
        IBillingUsageReader reader,
        IBillingPlanChangePolicy policy,
        ICurrentRequestContext requestContext,
        IDateTimeProvider clock)
    {
        _context = context;
        _reader = reader;
        _policy = policy;
        _requestContext = requestContext;
        _clock = clock;
    }

    public async Task<Result<WorkspaceBillingPlanDto>> Handle(
        ChangeAccountPlanCommand request, CancellationToken cancellationToken)
    {
        if (!_policy.IsPlanChangeEnabled)
            throw new ForbiddenException("Changing the plan is not available in this environment.");

        var target = BillingPlanCatalog.FindByCode(request.PlanCode);
        if (target is null)
            return Result<WorkspaceBillingPlanDto>.Failure("The plan does not exist.");

        var accountId = _requestContext.RequireAccountId();
        var userId = _requestContext.UserId;

        if (!await _reader.IsAccountOwnerAsync(accountId, userId, cancellationToken))
            throw new ForbiddenException("Only the account owner can change the plan.");

        var now = _clock.UtcNow;
        var current = await _context.Subscriptions
            .Where(s => s.AccountId == accountId && s.WorkspaceId == null && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (current is null)
        {
            _context.Subscriptions.Add(Subscription.Create(
                accountId, target.PlanId, target.Tier, now, now.Add(SubscriptionTerm), userId, now));
        }
        else
        {
            current.ChangePlan(target.PlanId, target.Tier, userId, now);
            if (current.CurrentPeriodEnd <= now)
                current.Renew(now, now.Add(SubscriptionTerm), userId, now);
        }

        return Result<WorkspaceBillingPlanDto>.Success(BillingPlanMapper.ToOverview(target, canChange: true));
    }
}
