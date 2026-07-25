namespace Notrelix.Domain.Documents.Blocks;

public sealed class BlockAncestorPath : ValueObject
{
    public Guid TargetParentId { get; }
    public IReadOnlyList<Guid> AncestorIds { get; }

    private BlockAncestorPath(Guid targetParentId, IReadOnlyList<Guid> ancestorIds)
    {
        TargetParentId = targetParentId;
        AncestorIds = ancestorIds;
    }

    public static BlockAncestorPath Create(Guid targetParentId, IReadOnlyList<Guid> ancestorIds)
    {
        Guard.NotEmpty(targetParentId);
        Guard.NotNull(ancestorIds);

        if (ancestorIds.Any(id => id == Guid.Empty))
            throw new BusinessRuleException(DocumentRuleCodes.Documents_BlockTree_AncestorPathContainsEmptyId, "Ancestor path must not contain empty GUIDs.");

        if (ancestorIds.Distinct().Count() != ancestorIds.Count)
            throw new BusinessRuleException(DocumentRuleCodes.Documents_BlockTree_AncestorPathContainsDuplicates, "Ancestor path must not contain duplicates.");

        if (ancestorIds.Contains(targetParentId))
            throw new BusinessRuleException(DocumentRuleCodes.Documents_BlockTree_AncestorPathContainsTargetParent, "Ancestor path must not contain the target parent ID.");

        return new BlockAncestorPath(targetParentId, ancestorIds.ToArray());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return TargetParentId;
        foreach (var id in AncestorIds)
            yield return id;
    }
}
