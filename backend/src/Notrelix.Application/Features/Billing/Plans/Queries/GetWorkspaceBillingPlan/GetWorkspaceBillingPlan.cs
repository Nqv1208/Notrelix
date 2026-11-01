using Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Billing.Plans.Queries.GetWorkspaceBillingPlan;

/// <summary>One capability limit of a plan; a null limit with IsUnlimited true means no cap.</summary>
public sealed record BillingPlanLimitDto(string Code, bool IsUnlimited, int? Limit);

/// <summary>A plan of the catalog with its capability limits.</summary>
public sealed record BillingPlanDto(string Code, string Name, IReadOnlyList<BillingPlanLimitDto> Limits);

/// <summary>
/// The plan the workspace's account is on and the catalog's sellable plans. A plan change is made by the
/// platform operator, never from a tenant session, so the answer carries no change right.
/// </summary>
public sealed record WorkspaceBillingPlanDto(
    BillingPlanDto CurrentPlan,
    IReadOnlyList<BillingPlanDto> AvailablePlans);

/// <summary>
/// Reads the account's plan for a workspace. Workspace Owners and Admins may view billing
/// (the permission that manages the features these limits gate).
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
}

public class GetWorkspaceBillingPlanQueryHandler
    : IRequestHandler<GetWorkspaceBillingPlanQuery, Result<WorkspaceBillingPlanDto>>
{
    private readonly BillingPlanResolver _resolver;
    private readonly BillingPlanCatalogReader _catalog;
    private readonly ICurrentRequestContext _requestContext;

    public GetWorkspaceBillingPlanQueryHandler(
        BillingPlanResolver resolver, BillingPlanCatalogReader catalog, ICurrentRequestContext requestContext)
    {
        _resolver = resolver;
        _catalog = catalog;
        _requestContext = requestContext;
    }

    public async Task<Result<WorkspaceBillingPlanDto>> Handle(
        GetWorkspaceBillingPlanQuery request, CancellationToken cancellationToken)
    {
        var accountId = _requestContext.RequireAccountId();
        var plan = await _resolver.ResolveAsync(accountId, cancellationToken);
        var available = await _catalog.ListSellableAsync(cancellationToken);

        return Result<WorkspaceBillingPlanDto>.Success(new WorkspaceBillingPlanDto(
            BillingPlanMapper.ToDto(plan),
            available.Select(BillingPlanMapper.ToDto).ToList()));
    }
}
