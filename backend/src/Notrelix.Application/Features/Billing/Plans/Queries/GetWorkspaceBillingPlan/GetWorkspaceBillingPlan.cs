using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Billing.Abstractions;

namespace Notrelix.Application.Features.Billing.Plans.Queries.GetWorkspaceBillingPlan;

/// <summary>One capability limit of a plan; a null limit with IsUnlimited true means no cap.</summary>
public sealed record BillingPlanLimitDto(string Code, bool IsUnlimited, int? Limit);

/// <summary>A plan of the catalog with its capability limits.</summary>
public sealed record BillingPlanDto(string Code, string Name, IReadOnlyList<BillingPlanLimitDto> Limits);

/// <summary>
/// The plan the workspace's account is on, the catalog it can move within, and whether the
/// caller may change the plan (account owner, in an environment that allows it).
/// </summary>
public sealed record WorkspaceBillingPlanDto(
    BillingPlanDto CurrentPlan,
    IReadOnlyList<BillingPlanDto> AvailablePlans,
    bool CanChangePlan);

/// <summary>
/// Reads the account's plan for a workspace. Workspace Owners and Admins may view billing
/// (the permission that manages the features these limits gate); changing the plan is a
/// separate command that needs the account owner.
/// </summary>
public record GetWorkspaceBillingPlanQuery(Guid WorkspaceId)
    : IQuery<Result<WorkspaceBillingPlanDto>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBilling;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("billing.plan"), WorkspaceId, WorkspaceId);
}

public static class BillingPlanMapper
{
    public static BillingPlanDto ToDto(BillingPlanDefinition plan) => new(
        plan.Code,
        plan.Name,
        plan.Limits
            .Select(l => new BillingPlanLimitDto(l.Key, l.Value is null, l.Value))
            .ToList());

    public static WorkspaceBillingPlanDto ToOverview(BillingPlanDefinition current, bool canChange) => new(
        ToDto(current),
        BillingPlanCatalog.All.Select(ToDto).ToList(),
        canChange);
}

public class GetWorkspaceBillingPlanQueryHandler
    : IRequestHandler<GetWorkspaceBillingPlanQuery, Result<WorkspaceBillingPlanDto>>
{
    private readonly BillingPlanResolver _resolver;
    private readonly IBillingUsageReader _reader;
    private readonly IBillingPlanChangePolicy _policy;
    private readonly ICurrentRequestContext _requestContext;

    public GetWorkspaceBillingPlanQueryHandler(
        BillingPlanResolver resolver,
        IBillingUsageReader reader,
        IBillingPlanChangePolicy policy,
        ICurrentRequestContext requestContext)
    {
        _resolver = resolver;
        _reader = reader;
        _policy = policy;
        _requestContext = requestContext;
    }

    public async Task<Result<WorkspaceBillingPlanDto>> Handle(
        GetWorkspaceBillingPlanQuery request, CancellationToken cancellationToken)
    {
        var accountId = _requestContext.RequireAccountId();
        var plan = await _resolver.ResolveAsync(accountId, cancellationToken);
        var canChange = _policy.IsPlanChangeEnabled
            && await _reader.IsAccountOwnerAsync(accountId, _requestContext.UserId, cancellationToken);

        return Result<WorkspaceBillingPlanDto>.Success(BillingPlanMapper.ToOverview(plan, canChange));
    }
}
