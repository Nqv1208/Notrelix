namespace Notrelix.Application.Features.Governance.Public.Access;

/// <summary>
/// Producer-owned read for a resource invitation (WSP-026): the display title of
/// the one Board or Page the invitation names, so the invitation email can name
/// it. The resource is checked in the request's Workspace; a missing, deleted,
/// archived or other-Workspace resource yields <c>null</c> and the invitation is
/// not issued.
/// </summary>
public interface IGuestResourceInvitationFacts
{
    Task<string?> ResolveTitleAsync(
        string resourceKind,
        Guid resourceId,
        Guid workspaceId,
        CancellationToken cancellationToken);
}
