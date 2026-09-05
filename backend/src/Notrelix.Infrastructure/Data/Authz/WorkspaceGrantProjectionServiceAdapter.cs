using Notrelix.Application.Features.Workspaces.Members.Services;
using Notrelix.Domain.Workspaces.Members;

namespace Notrelix.Infrastructure.Data.Authz;

public sealed class WorkspaceGrantProjectionServiceAdapter : IWorkspaceGrantProjectionService
{
    private readonly AccessGrantProjectionService _projection;

    public WorkspaceGrantProjectionServiceAdapter(AccessGrantProjectionService projection)
    {
        _projection = projection;
    }

    public Task SyncWorkspaceMemberGrantAsync(
        Guid accountId,
        Guid workspaceId,
        Guid userId,
        WorkspaceRole role,
        DateTimeOffset now,
        CancellationToken ct)
        => _projection.SyncWorkspaceMemberGrantAsync(accountId, workspaceId, userId, role, now, ct);

    public Task RevokeWorkspaceMemberGrantAsync(
        Guid accountId,
        Guid workspaceId,
        Guid userId,
        DateTimeOffset now,
        CancellationToken ct)
        => _projection.RevokeWorkspaceMemberGrantAsync(accountId, workspaceId, userId, now, ct);
}
