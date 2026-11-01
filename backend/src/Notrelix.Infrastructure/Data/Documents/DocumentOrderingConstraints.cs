namespace Notrelix.Infrastructure.Data.Documents;

/// <summary>
/// The named constraint that keeps two live sibling blocks off one sort key. It is a deferrable exclusion
/// constraint (not a unique index) so a write that moves blocks onto keys another block of the same write
/// leaves is judged on the committed result, at commit. Only a violation of this name is an ordering
/// conflict; any other violation keeps its existing mapping.
/// </summary>
public static class DocumentOrderingConstraints
{
    public const string BlockSiblings = "ex_docs_blocks_sibling_position";

    public const string BlockPositionConflictCode = "documents.block.position-conflict";

    public static bool IsBlockOrderingViolation(Exception exception) =>
        (exception as PostgresException ?? exception.InnerException as PostgresException) is
        { SqlState: PostgresErrorCodes.ExclusionViolation, ConstraintName: BlockSiblings };
}
