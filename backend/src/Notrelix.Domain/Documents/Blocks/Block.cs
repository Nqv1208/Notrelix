using Notrelix.Domain.Documents.Rules;
using Notrelix.Domain.Documents.Blocks.Events;
namespace Notrelix.Domain.Documents.Blocks;

public class Block : SoftDeletableAggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid PageId { get; private set; }
    public Guid? ParentId { get; private set; }
    public BlockType Type { get; private set; }
    public BlockContent Content { get; private set; } = null!;
    public BlockProperties Properties { get; private set; } = null!;
    public FractionalIndex Position { get; private set; } = null!;

    private Block() : base() { }

    public static Block Create(
        Guid accountId,
        Guid workspaceId,
        Guid pageId,
        BlockType type,
        BlockContent content,
        FractionalIndex position,
        Guid createdBy,
        DateTimeOffset createdAt,
        Guid? parentId = null,
        BlockProperties? properties = null)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(pageId);
        Guard.NotEmpty(createdBy);
        Guard.NotNull(content);
        Guard.NotNull(position);

        Rules.BlockContentValidator.Validate(type, content);

        var block = new Block
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            PageId = pageId,
            ParentId = parentId,
            Type = type,
            Content = content,
            Properties = properties ?? BlockProperties.Empty(),
            Position = position
        };

        block.SetAuditOnCreate(createdBy, createdAt);
        block.RaiseDomainEvent(new BlockCreatedDomainEvent(accountId, workspaceId, pageId, block.Id, type, createdBy, createdAt));

        return block;
    }

    public void UpdateContent(BlockContent newContent, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(newContent);
        Rules.BlockContentValidator.Validate(Type, newContent);

        if (Content == newContent) return;

        Content = newContent;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new BlockContentUpdatedDomainEvent(AccountId, WorkspaceId, Id, PageId, updatedBy, updatedAt));
    }

    public void UpdateProperties(BlockProperties newProperties, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(newProperties);

        if (Properties == newProperties) return;

        Properties = newProperties;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new BlockPropertiesUpdatedDomainEvent(AccountId, WorkspaceId, Id, PageId, updatedBy, updatedAt));
    }

    public void MoveToRoot(FractionalIndex newPosition, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(newPosition);

        if (ParentId == null && Position == newPosition) return;

        var oldParentId = ParentId;
        ParentId = null;
        Position = newPosition;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new BlockMovedDomainEvent(AccountId, WorkspaceId, Id, PageId, oldParentId, null, newPosition.Value, updatedBy, updatedAt));
    }

    public void MoveUnder(BlockAncestorPath parentPath, FractionalIndex newPosition, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(parentPath);
        Guard.NotNull(newPosition);

        // Validate scope match: parent must be in the same account/workspace/page
        if (parentPath.AccountId != AccountId)
            throw new BusinessRuleException(DocumentRuleCodes.Documents_BlockTree_ScopeMismatch, "Parent block must belong to the same account.");
        if (parentPath.WorkspaceId != WorkspaceId)
            throw new BusinessRuleException(DocumentRuleCodes.Documents_BlockTree_ScopeMismatch, "Parent block must belong to the same workspace.");
        if (parentPath.PageId != PageId)
            throw new BusinessRuleException(DocumentRuleCodes.Documents_BlockTree_ScopeMismatch, "Parent block must belong to the same page.");

        BlockTreeRules.EnsureNoCycle(Id, parentPath);

        if (ParentId == parentPath.TargetParentId && Position == newPosition) return;

        var oldParentId = ParentId;
        ParentId = parentPath.TargetParentId;
        Position = newPosition;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new BlockMovedDomainEvent(AccountId, WorkspaceId, Id, PageId, oldParentId, parentPath.TargetParentId, newPosition.Value, updatedBy, updatedAt));
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        if (!MarkDeleted(deletedBy, deletedAt, reason)) return;
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        RaiseDomainEvent(new BlockSoftDeletedDomainEvent(AccountId, WorkspaceId, Id, PageId, deletedBy, deletedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        if (!MarkRestored(restoredBy, restoredAt)) return;
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        RaiseDomainEvent(new BlockRestoredDomainEvent(AccountId, WorkspaceId, Id, PageId, restoredBy, restoredAt));
    }
}
