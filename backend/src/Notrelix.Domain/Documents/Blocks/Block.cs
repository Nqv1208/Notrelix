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

    public static Block CreateRoot(
        Guid accountId,
        Guid workspaceId,
        Guid pageId,
        BlockType type,
        BlockContent content,
        FractionalIndex position,
        Guid createdBy,
        DateTimeOffset createdAt,
        BlockProperties? properties = null,
        Guid? id = null)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(pageId);
        Guard.NotEmpty(createdBy);
        Guard.NotNull(content);
        Guard.NotNull(position);
        if (id == Guid.Empty) throw new ArgumentException("Block id cannot be empty.", nameof(id));

        var block = new Block
        {
            Id = id ?? Guid.CreateVersion7(),
            AccountId = accountId,
            WorkspaceId = workspaceId,
            PageId = pageId,
            ParentId = null,
            Type = type,
            Content = Rules.BlockContentValidator.Normalize(type, content),
            Properties = Rules.BlockPropertiesValidator.Normalize(type, properties ?? BlockProperties.Empty()),
            Position = position
        };

        block.SetAuditOnCreate(createdBy, createdAt);
        block.RaiseDomainEvent(new BlockCreatedDomainEvent(accountId, workspaceId, pageId, block.Id, type, createdBy, createdAt));

        return block;
    }

    public static Block CreateChild(
        Guid accountId,
        Guid workspaceId,
        Guid pageId,
        BlockType type,
        BlockContent content,
        FractionalIndex position,
        Guid createdBy,
        DateTimeOffset createdAt,
        BlockAncestorPath parentPath,
        BlockProperties? properties = null,
        Guid? id = null)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(pageId);
        Guard.NotEmpty(createdBy);
        Guard.NotNull(content);
        Guard.NotNull(position);
        Guard.NotNull(parentPath);
        if (id == Guid.Empty) throw new ArgumentException("Block id cannot be empty.", nameof(id));

        if (parentPath.AccountId != accountId)
            throw new BusinessRuleException(DocumentRuleCodes.Documents_BlockTree_ScopeMismatch, "Parent block must belong to the same account.");
        if (parentPath.WorkspaceId != workspaceId)
            throw new BusinessRuleException(DocumentRuleCodes.Documents_BlockTree_ScopeMismatch, "Parent block must belong to the same workspace.");
        if (parentPath.PageId != pageId)
            throw new BusinessRuleException(DocumentRuleCodes.Documents_BlockTree_ScopeMismatch, "Parent block must belong to the same page.");

        var block = new Block
        {
            Id = id ?? Guid.CreateVersion7(),
            AccountId = accountId,
            WorkspaceId = workspaceId,
            PageId = pageId,
            ParentId = parentPath.TargetParentId,
            Type = type,
            Content = Rules.BlockContentValidator.Normalize(type, content),
            Properties = Rules.BlockPropertiesValidator.Normalize(type, properties ?? BlockProperties.Empty()),
            Position = position
        };

        block.SetAuditOnCreate(createdBy, createdAt);
        block.RaiseDomainEvent(new BlockCreatedDomainEvent(accountId, workspaceId, pageId, block.Id, type, createdBy, createdAt));

        return block;
    }

    public void UpdateContent(BlockContent newContent, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        Guard.NotNull(newContent);
        var normalised = Rules.BlockContentValidator.Normalize(Type, newContent);

        if (Rules.BlockContentValidator.AreSame(Type, Content, normalised)) return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Content = normalised;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BlockContentUpdatedDomainEvent(AccountId, WorkspaceId, Id, PageId, Type, updatedBy, updatedAt));
    }

    /// <summary>
    /// One edit of a block: the new type (rich text and the properties the new type has carry over), the
    /// authored content and the properties, each optional. Everything is validated against the resulting
    /// type before anything changes, and the whole edit is one version step. Nothing changed = no-op.
    /// </summary>
    public void Edit(BlockType? newType, BlockContent? newContent, BlockProperties? newProperties, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);

        var type = newType ?? Type;
        var typeChanged = type != Type;
        var baseContent = typeChanged ? Rules.BlockContentConversion.Convert(Type, type, Content) : Content;
        var baseProperties = typeChanged ? Rules.BlockPropertiesValidator.Convert(type, Properties) : Properties;

        var content = Rules.BlockContentValidator.Normalize(type, newContent ?? baseContent);
        var properties = Rules.BlockPropertiesValidator.Normalize(type, newProperties ?? baseProperties);
        var contentChanged = typeChanged || !Rules.BlockContentValidator.AreSame(type, Content, content);
        var propertiesChanged = !Rules.BlockPropertiesValidator.AreSame(type, Properties, properties);
        if (!contentChanged && !propertiesChanged) return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Type = type;
        Content = content;
        Properties = properties;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        if (contentChanged)
            RaiseDomainEvent(new BlockContentUpdatedDomainEvent(AccountId, WorkspaceId, Id, PageId, Type, updatedBy, updatedAt));
        if (propertiesChanged)
            RaiseDomainEvent(new BlockPropertiesUpdatedDomainEvent(AccountId, WorkspaceId, Id, PageId, updatedBy, updatedAt));
    }

    /// <summary>
    /// Changes the block's type in place (the block keeps its id, position and
    /// parent). Rich text carries over between text-like types and the
    /// properties the new type has; a change to or from any other shape starts
    /// with empty content. Same type = no-op.
    /// </summary>
    public void ChangeType(BlockType newType, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);

        if (Type == newType) return;

        var converted = Rules.BlockContentValidator.Normalize(
            newType, Rules.BlockContentConversion.Convert(Type, newType, Content));
        var convertedProperties = Rules.BlockPropertiesValidator.Normalize(
            newType, Rules.BlockPropertiesValidator.Convert(newType, Properties));

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Type = newType;
        Content = converted;
        Properties = convertedProperties;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BlockContentUpdatedDomainEvent(AccountId, WorkspaceId, Id, PageId, Type, updatedBy, updatedAt));
    }

    public void UpdateProperties(BlockProperties newProperties, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        Guard.NotNull(newProperties);
        var normalised = Rules.BlockPropertiesValidator.Normalize(Type, newProperties);

        if (Rules.BlockPropertiesValidator.AreSame(Type, Properties, normalised)) return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Properties = normalised;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BlockPropertiesUpdatedDomainEvent(AccountId, WorkspaceId, Id, PageId, updatedBy, updatedAt));
    }

    public void MoveToRoot(FractionalIndex newPosition, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        Guard.NotNull(newPosition);

        if (ParentId == null && Position == newPosition) return;

        var oldParentId = ParentId;
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        ParentId = null;
        Position = newPosition;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BlockMovedDomainEvent(AccountId, WorkspaceId, Id, PageId, oldParentId, null, newPosition.Value, updatedBy, updatedAt));
    }

    public void MoveUnder(BlockAncestorPath parentPath, FractionalIndex newPosition, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
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
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        ParentId = parentPath.TargetParentId;
        Position = newPosition;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BlockMovedDomainEvent(AccountId, WorkspaceId, Id, PageId, oldParentId, parentPath.TargetParentId, newPosition.Value, updatedBy, updatedAt));
    }

    public void Delete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;
        var pendingDeletion = PrepareDeletion(deletedBy, deletedAt, reason);
        ApplyDeletion(pendingDeletion);
        IncrementVersion();
        RaiseDomainEvent(new BlockDeletedDomainEvent(AccountId, WorkspaceId, Id, PageId, deletedBy, deletedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;
        var pendingRestore = PrepareRestore(restoredBy, restoredAt);
        ApplyRestore(pendingRestore);
        IncrementVersion();
        RaiseDomainEvent(new BlockRestoredDomainEvent(AccountId, WorkspaceId, Id, PageId, restoredBy, restoredAt));
    }
}
