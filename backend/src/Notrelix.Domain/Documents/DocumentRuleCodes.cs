namespace Notrelix.Domain.Documents;

/// <summary>
/// Rule codes for the Documents bounded context.
/// </summary>
public static class DocumentRuleCodes
{
    // ── Page ──────────────────────────────────────────────────────────────
    public const string Documents_Page_CannotRenameArchived = "Documents_Page_CannotRenameArchived";
    public const string Documents_Page_CannotMoveArchived = "Documents_Page_CannotMoveArchived";
    public const string Documents_Page_CannotEditArchived = "Documents_Page_CannotEditArchived";

    // ── PageTree ──────────────────────────────────────────────────────────
    public const string Documents_PageTree_CannotBeOwnParent = "Documents_PageTree_CannotBeOwnParent";
    public const string Documents_PageTree_MoveWouldCreateCycle = "Documents_PageTree_MoveWouldCreateCycle";

    // ── Block ─────────────────────────────────────────────────────────────
    public const string Documents_Block_ContentCannotBeNull = "Documents_Block_ContentCannotBeNull";

    // ── BlockTree ─────────────────────────────────────────────────────────
    public const string Documents_BlockTree_CannotBeOwnParent = "Documents_BlockTree_CannotBeOwnParent";
    public const string Documents_BlockTree_MoveWouldCreateCycle = "Documents_BlockTree_MoveWouldCreateCycle";
    public const string Documents_BlockTree_ParentNotFound = "Documents_BlockTree_ParentNotFound";
    public const string Documents_BlockTree_ParentMustBeInSamePage = "Documents_BlockTree_ParentMustBeInSamePage";
    public const string Documents_BlockTree_AncestorPathEmpty = "Documents_BlockTree_AncestorPathEmpty";
    public const string Documents_BlockTree_AncestorPathContainsEmptyId = "Documents_BlockTree_AncestorPathContainsEmptyId";
    public const string Documents_BlockTree_AncestorPathContainsDuplicates = "Documents_BlockTree_AncestorPathContainsDuplicates";
    public const string Documents_BlockTree_AncestorPathContainsTargetParent = "Documents_BlockTree_AncestorPathContainsTargetParent";

    // ── ResourceLink ──────────────────────────────────────────────────────
    public const string Documents_ResourceLink_CannotCreateSelfReferencing = "Documents_ResourceLink_CannotCreateSelfReferencing";
    public const string Documents_ResourceLink_TargetMustBeInSameWorkspace = "Documents_ResourceLink_TargetMustBeInSameWorkspace";

    // ── PageTemplate ──────────────────────────────────────────────────────
    public const string Documents_PageTemplate_CannotPublishArchived = "Documents_PageTemplate_CannotPublishArchived";
}
