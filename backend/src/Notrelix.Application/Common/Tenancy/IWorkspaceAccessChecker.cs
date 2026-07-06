using Notrelix.Application.Common.Models;

namespace Notrelix.Application.Common.Tenancy;

public interface IWorkspaceAccessChecker
{
    Task<Result> EnsureWorkspaceExistsAsync(Guid workspaceId, CancellationToken ct);

    Task<Result> EnsureWorkspaceIsActiveAsync(Guid workspaceId, CancellationToken ct);
}
