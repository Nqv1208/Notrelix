namespace Notrelix.Domain.Workspaces.Spaces;

/// <summary>
/// Application-supplied fact about the intended parent of a Space. The Domain
/// never queries the hierarchy: Application loads the parent and its ancestry
/// (root → parent, including the parent itself) under the hierarchy lock.
/// </summary>
public sealed record SpaceParentFact(
    Guid SpaceId,
    Guid ParentWorkspaceId,
    bool IsActive,
    IReadOnlyCollection<Guid> AncestorPathIds);

/// <summary>
/// Where a Space is placed: its parent (null = Workspace root), its sibling
/// position, and the names of the other non-deleted siblings under that parent
/// (for the sibling-name uniqueness rule, WSP-021).
/// </summary>
public sealed record SpacePlacement(
    SpaceParentFact? Parent,
    FractionalIndex Position,
    IReadOnlyCollection<string> SiblingNames)
{
    public static SpacePlacement Root(FractionalIndex position, IReadOnlyCollection<string> siblingNames) =>
        new(null, position, siblingNames);
}

/// <summary>
/// Application-supplied content of a Space used by archive/delete (WSP-022):
/// child Spaces and Boards/Pages placed in it. "Active" excludes archived and
/// deleted; "existing" excludes only deleted.
/// </summary>
public sealed record SpaceContentFact(
    int ActiveChildSpaces,
    int ExistingChildSpaces,
    int ActiveResources,
    int ExistingResources)
{
    public static SpaceContentFact Empty { get; } = new(0, 0, 0, 0);
}
