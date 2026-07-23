namespace Notrelix.Domain.Documents.Blocks;

public class Block : AggregateRoot, IWorkspaceScoped
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

    public void Move(Guid? newParentId, FractionalIndex newPosition, Guid updatedBy, DateTimeOffset updatedAt, Func<Guid, Guid?>? getParentId = null)
    {
        EnsureNotDeleted();
        Guard.NotNull(newPosition);

        if (getParentId != null)
            BlockTreeRules.EnsureNoCycle(Id, newParentId, getParentId);

        if (ParentId == newParentId && Position == newPosition) return;

        var oldParentId = ParentId;
        ParentId = newParentId;
        Position = newPosition;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new BlockMovedDomainEvent(AccountId, WorkspaceId, Id, PageId, oldParentId, newParentId, newPosition.Value, updatedBy, updatedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        RaiseDomainEvent(new BlockSoftDeletedDomainEvent(AccountId, WorkspaceId, Id, PageId, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        base.Restore(restoredBy, restoredAt);
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        RaiseDomainEvent(new BlockRestoredDomainEvent(AccountId, WorkspaceId, Id, PageId, restoredBy, restoredAt));
    }
}
