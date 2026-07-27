namespace Notrelix.Domain.Documents.Blocks;

public sealed class BlockAncestorPath : ValueObject
{
    public Guid AccountId { get; }
    public Guid WorkspaceId { get; }
    public Guid PageId { get; }
    public Guid TargetParentId { get; }
    public IReadOnlyList<Guid> AncestorIds { get; }

    private BlockAncestorPath(
        Guid accountId,
        Guid workspaceId,
        Guid pageId,
        Guid targetParentId,
        IReadOnlyList<Guid> ancestorIds)
    {
        AccountId = accountId;
        WorkspaceId = workspaceId;
        PageId = pageId;
        TargetParentId = targetParentId;
        AncestorIds = ancestorIds;
    }

    public static BlockAncestorPath Create(
        Guid accountId,
        Guid workspaceId,
        Guid pageId,
        Guid targetParentId,
        IReadOnlyList<Guid> ancestorIds)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(pageId);
        Guard.NotEmpty(targetParentId);
        Guard.NotNull(ancestorIds);

        if (ancestorIds.Any(id => id == Guid.Empty))
            throw new BusinessRuleException(DocumentRuleCodes.Documents_BlockTree_AncestorPathContainsEmptyId, "Ancestor path must not contain empty GUIDs.");

        if (ancestorIds.Distinct().Count() != ancestorIds.Count)
            throw new BusinessRuleException(DocumentRuleCodes.Documents_BlockTree_AncestorPathContainsDuplicates, "Ancestor path must not contain duplicates.");

        if (ancestorIds.Contains(targetParentId))
            throw new BusinessRuleException(DocumentRuleCodes.Documents_BlockTree_AncestorPathContainsTargetParent, "Ancestor path must not contain the target parent ID.");

        return new BlockAncestorPath(accountId, workspaceId, pageId, targetParentId, ancestorIds.ToArray());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AccountId;
        yield return WorkspaceId;
        yield return PageId;
        yield return TargetParentId;
        foreach (var id in AncestorIds)
            yield return id;
    }
}