namespace Notrelix.Application.Features.Governance.Abstractions;

/// <summary>
/// The workspace's sharing setting, owned by Workspaces and enforced by Governance where a link is made.
/// A workspace that has not turned links on does not allow them.
/// </summary>
public interface IWorkspaceSharingPolicy
{
    Task<bool> AllowsShareLinksAsync(Guid workspaceId, CancellationToken cancellationToken);
}
