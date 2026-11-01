using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Billing.Abstractions;
using Notrelix.Application.Features.Billing.Entitlements.Services;
using Notrelix.Application.Features.Billing.Public.Facts;

namespace Notrelix.Application.Features.Billing.Entitlements.Queries.GetWorkspaceBillingCapabilities;

/// <summary>
/// One commercial capability as it applies to a workspace: the stable capability
/// code, whether it is granted, its limit (null when unlimited or not granted),
/// what the workspace has used and what remains (null when unlimited).
/// </summary>
public sealed record BillingCapabilityDto(
    string Code,
    bool IsGranted,
    bool IsUnlimited,
    int? Limit,
    decimal Used,
    int? Remaining);

/// <summary>
/// The capabilities a workspace's billing grants. Billing owns capability
/// meaning, so only the stable capability facts cross this seam (never plan
/// tiers or subscriptions). Billing administration has no dedicated Governance
/// action yet; the read needs ManageWorkspaceSettings, the permission that
/// manages the features these capabilities gate.
/// </summary>
public record GetWorkspaceBillingCapabilitiesQuery(Guid WorkspaceId)
    : IQuery<Result<IReadOnlyList<BillingCapabilityDto>>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBilling;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("billing.capability"), WorkspaceId, WorkspaceId);
}

public class GetWorkspaceBillingCapabilitiesQueryHandler
    : IRequestHandler<GetWorkspaceBillingCapabilitiesQuery, Result<IReadOnlyList<BillingCapabilityDto>>>
{
    private readonly IBillingCapabilityFacts _facts;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IBillingUsageReader _usageReader;

    public GetWorkspaceBillingCapabilitiesQueryHandler(
        IBillingCapabilityFacts facts,
        ICurrentRequestContext requestContext,
        IBillingUsageReader usageReader)
    {
        _facts = facts;
        _requestContext = requestContext;
        _usageReader = usageReader;
    }

    public async Task<Result<IReadOnlyList<BillingCapabilityDto>>> Handle(
        GetWorkspaceBillingCapabilitiesQuery request,
        CancellationToken cancellationToken)
    {
        var accountId = _requestContext.RequireAccountId();
        var capabilities = new List<BillingCapabilityDto>();

        foreach (var code in BillingCapabilityCode.All)
        {
            var fact = await _facts.GetCapabilityAsync(
                accountId, request.WorkspaceId, code, requestedAmount: 0, cancellationToken);

            capabilities.Add(fact is null
                ? new BillingCapabilityDto(code, false, false, null, 0, null)
                : new BillingCapabilityDto(
                    code,
                    IsGranted: fact.IsAvailable || fact.Limit is not null,
                    IsUnlimited: fact.IsAvailable && fact.Limit is null,
                    fact.Limit,
                    code == BillingCapabilityCode.StorageGb
                        ? await StorageUsedGbAsync(accountId, request.WorkspaceId, cancellationToken)
                        : fact.Used ?? 0,
                    fact.Remaining));
        }

        return Result<IReadOnlyList<BillingCapabilityDto>>.Success(capabilities);
    }

    /// <summary>Storage is shown to two decimals so a few megabytes do not read as a whole gigabyte.</summary>
    private async Task<decimal> StorageUsedGbAsync(Guid accountId, Guid workspaceId, CancellationToken cancellationToken)
    {
        var usage = await _usageReader.GetAccountUsageAsync(accountId, workspaceId, cancellationToken);
        return Math.Round(usage.StorageBytes / (decimal)BillingCapabilityFactsProvider.BytesPerGb, 2);
    }
}
