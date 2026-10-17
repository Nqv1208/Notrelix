namespace Notrelix.API.Contracts.Workspaces.Spaces.Requests;

/// <param name="Visibility"><c>Private</c> or <c>Workspace</c>; when omitted a Department Space is Private and other types are Workspace-visible (WSP-023).</param>
/// <param name="SpaceType"><c>Folder</c> (default), <c>Portfolio</c>, <c>Department</c> or <c>ProjectArea</c>.</param>
/// <param name="ParentSpaceId">Parent Space in the same workspace; omitted for a root Space.</param>
/// <param name="PreviousSiblingId">Place after this sibling (with <paramref name="NextSiblingId"/>: between the two). Both omitted appends.</param>
/// <param name="NextSiblingId">Place before this sibling.</param>
public sealed record CreateSpaceRequest(
    string Name,
    string? Visibility = null,
    string? Description = null,
    string? SpaceType = null,
    Guid? ParentSpaceId = null,
    Guid? PreviousSiblingId = null,
    Guid? NextSiblingId = null);
