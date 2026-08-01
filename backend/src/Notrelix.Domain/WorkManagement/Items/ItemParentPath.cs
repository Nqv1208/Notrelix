namespace Notrelix.Domain.WorkManagement.Items;

public sealed class ItemParentPath : ValueObject
{
    public Guid AccountId { get; }
    public Guid WorkspaceId { get; }
    public Guid BoardId { get; }
    public Guid ParentItemId { get; }
    public int ParentLevel { get; }
    public IReadOnlyList<Guid> AncestorIds { get; }

    public int ChildLevel =>
        checked(ParentLevel + 1);

    private ItemParentPath(
        Guid accountId,
        Guid workspaceId,
        Guid boardId,
        Guid parentItemId,
        int parentLevel,
        IReadOnlyList<Guid> ancestorIds)
    {
        AccountId = accountId;
        WorkspaceId = workspaceId;
        BoardId = boardId;
        ParentItemId = parentItemId;
        ParentLevel = parentLevel;
        AncestorIds = ancestorIds;
    }

    public static ItemParentPath Create(
        Guid accountId,
        Guid workspaceId,
        Guid boardId,
        Guid parentItemId,
        int parentLevel,
        IReadOnlyList<Guid> ancestorIds)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(boardId);
        Guard.NotEmpty(parentItemId);
        Guard.NotNegative(parentLevel);
        Guard.NotNull(ancestorIds);

        if (ancestorIds.Any(id => id == Guid.Empty))
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_ParentPathContainsEmptyId, "Parent path must not contain empty GUIDs.");

        if (ancestorIds.Distinct().Count() != ancestorIds.Count)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_ParentPathContainsDuplicates, "Parent path must not contain duplicates.");

        if (ancestorIds.Contains(parentItemId))
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_ParentPathContainsTargetParent, "Parent path must not contain the target parent ID.");

        return new ItemParentPath(accountId, workspaceId, boardId, parentItemId, parentLevel, ancestorIds.ToArray());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AccountId;
        yield return WorkspaceId;
        yield return BoardId;
        yield return ParentItemId;
        yield return ParentLevel;
        foreach (var id in AncestorIds)
            yield return id;
    }
}
