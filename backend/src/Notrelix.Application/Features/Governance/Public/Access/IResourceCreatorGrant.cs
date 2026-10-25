namespace Notrelix.Application.Features.Governance.Public.Access;

/// <summary>
/// Producer-owned public target action: Governance records the explicit
/// Manager entry the creator of a page holds on it, so the creator can share
/// and manage what they made. The owning context (Documents, CreatePage) only
/// requests it; Governance decides and persists the entry.
/// </summary>
/// <remarks>
/// Runs on the caller's unit of work: the entry is saved with the new resource
/// or not at all. The grant is a plain User entry like any other: it can be
/// changed or revoked through the normal resource-permission commands.
/// </remarks>
public interface IResourceCreatorGrant
{
    /// <param name="resourceKind">Canonical kind, e.g. <c>documents.page</c>.</param>
    void GrantManager(
        Guid accountId,
        Guid workspaceId,
        string resourceKind,
        Guid resourceId,
        Guid creatorUserId,
        DateTimeOffset now);
}
