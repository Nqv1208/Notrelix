namespace Notrelix.Infrastructure.Data.Workspaces;

/// <summary>
/// One row of the Space closure index <c>workspace.space_paths</c> (ADR-009):
/// <see cref="AncestorId"/> is an ancestor-or-self of <see cref="DescendantId"/>
/// at <see cref="Depth"/> levels (0 = the Space itself). Infrastructure-owned
/// derived index maintained in the same transaction as the Space write by
/// <c>SpaceHierarchyStore</c>; never a product fact of its own.
/// </summary>
public sealed class SpacePathRecord : IWorkspaceScoped
{
    private SpacePathRecord() { }

    public SpacePathRecord(Guid accountId, Guid workspaceId, Guid ancestorId, Guid descendantId, int depth)
    {
        AccountId = accountId;
        WorkspaceId = workspaceId;
        AncestorId = ancestorId;
        DescendantId = descendantId;
        Depth = depth;
    }

    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid AncestorId { get; private set; }
    public Guid DescendantId { get; private set; }
    public int Depth { get; private set; }

    /// <summary>Re-points the distance of an existing ancestor/descendant pair after a move.</summary>
    public void Rebase(int depth) => Depth = depth;
}
