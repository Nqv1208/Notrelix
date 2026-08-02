using Notrelix.Application.Common.Requests;
using Notrelix.Application.Common.Security;

namespace Notrelix.Application.Common.Idempotency;

/// <summary>
/// Builds canonical tenant-qualified partition strings for idempotency scope.
/// Canonical values:
///   system
///   account:{accountId:N}
///   account:{accountId:N}:workspace:{workspaceId:N}
///   account:{accountId:N}:user:{userId:N}
/// </summary>
public sealed class IdempotencyPartitionFactory
{
    private readonly ICurrentTenantContext _tenantContext;

    public IdempotencyPartitionFactory(ICurrentTenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public string BuildPartition(IIdempotentRequest request)
    {
        if (request is IWorkspaceRequest workspaceRequest)
        {
            var accountId = _tenantContext.AccountId
                ?? throw new SecurityMisconfigurationException(
                    "AccountId is required for workspace-scoped idempotency partition.");

            var workspaceId = workspaceRequest.WorkspaceId;

            if (_tenantContext.WorkspaceId is not null && _tenantContext.WorkspaceId != workspaceId)
            {
                throw new SecurityMisconfigurationException(
                    $"WorkspaceId mismatch: request declares '{workspaceId}' but resolved tenant context has '{_tenantContext.WorkspaceId}'.");
            }

            return $"account:{accountId:N}:workspace:{workspaceId:N}";
        }

        if (request is IAccountRequest)
        {
            var accountId = _tenantContext.AccountId
                ?? throw new SecurityMisconfigurationException(
                    "AccountId is required for account-scoped idempotency partition.");

            return $"account:{accountId:N}";
        }

        if (_tenantContext.IsSystemContext)
        {
            return "system";
        }

        if (_tenantContext.UserId is not null && _tenantContext.AccountId is not null)
        {
            return $"account:{_tenantContext.AccountId:N}:user:{_tenantContext.UserId:N}";
        }

        throw new SecurityMisconfigurationException(
            $"Cannot determine idempotency partition for request '{request.GetType().Name}'. " +
            "Request must implement IWorkspaceRequest, IAccountRequest, run in system context, " +
            "or have resolved AccountId+UserId.");
    }
}
