using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Workspaces.Members;

namespace Notrelix.Application.Common.Tenancy;

/// <summary>
/// Maintains the authz.access_grants projection synchronously with membership
/// changes, inside the same transaction as the membership mutation. The
/// projection is read by RLS for tenant isolation, so it must never diverge
/// from the authoritative membership state.
/// </summary>
public interface IAccessGrantProjectionService
{
    Task SyncAccountMemberGrantAsync(Guid accountId, Guid userId, AccountRole role, DateTimeOffset now, CancellationToken ct);

    Task SyncWorkspaceMemberGrantAsync(Guid accountId, Guid workspaceId, Guid userId, WorkspaceRole role, DateTimeOffset now, CancellationToken ct);

    Task RevokeWorkspaceMemberGrantAsync(Guid accountId, Guid workspaceId, Guid userId, DateTimeOffset now, CancellationToken ct);
}