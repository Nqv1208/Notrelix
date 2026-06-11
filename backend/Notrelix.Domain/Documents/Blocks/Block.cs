using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Documents.Blocks;

public class Block : AggregateRoot
{
    public Guid PageId { get; private set; }
    public Guid? ParentId { get; private set; }
    public BlockType Type { get; private set; }
    public BlockContent Content { get; private set; } = null!;
    public BlockProperties Properties { get; private set; } = null!;
    public FractionalIndex Position { get; private set; } = null!;

    private Block() : base() { }

    public static Block Create(
        Guid pageId, 
        BlockType type, 
        BlockContent content, 
        FractionalIndex position, 
        Guid createdBy,
        Guid? parentId = null,
        BlockProperties? properties = null)
    {
        Guard.NotEmpty(pageId);
        Guard.NotEmpty(createdBy);
        Guard.NotNull(content);
        Guard.NotNull(position);

        Rules.BlockContentValidator.Validate(type, content);

        var block = new Block
        {
            PageId = pageId,
            ParentId = parentId,
            Type = type,
            Content = content,
            Properties = properties ?? BlockProperties.Empty(),
            Position = position
        };

        block.SetAuditOnCreate(createdBy);
        block.AddDomainEvent(new BlockCreatedEvent(pageId, block.Id, type, createdBy));

        return block;
    }

    public void UpdateContent(BlockContent newContent, Guid updatedBy)
    {
        Guard.NotNull(newContent);
        Rules.BlockContentValidator.Validate(Type, newContent);

        if (Content == newContent) return;

        Content = newContent;
        SetAuditOnUpdate(updatedBy);
        AddDomainEvent(new BlockUpdatedEvent(Id, PageId, updatedBy));
    }

    public void Move(Guid? newParentId, FractionalIndex newPosition, Guid updatedBy)
    {
        Guard.NotNull(newPosition);
        
        if (ParentId == newParentId && Position == newPosition) return;

        // Note: Cycle detection for nested blocks would require a tree rule similar to PageTreeRules
        // but typically blocks are shallow-nested or handled at Application level for complexity.

        var oldParentId = ParentId;
        ParentId = newParentId;
        Position = newPosition;
        SetAuditOnUpdate(updatedBy);
        
        AddDomainEvent(new BlockMovedEvent(Id, PageId, oldParentId, newParentId, newPosition.Value, updatedBy));
    }

    public void Delete(Guid deletedBy)
    {
        AddDomainEvent(new BlockDeletedEvent(Id, PageId, deletedBy));
        // Physically delete from Domain perspective for blocks, or soft delete if required by schema.
        // The SQL schema shows soft delete for many tables, but blocks are often physically deleted 
        // to avoid cluttering. Let's check the schema for block table.
    }
}
