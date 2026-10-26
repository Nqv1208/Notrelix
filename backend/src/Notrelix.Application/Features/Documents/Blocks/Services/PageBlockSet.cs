using System.Text.Json;
using Notrelix.Application.Common.Exceptions;
using Notrelix.Application.Features.Documents.Common;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Features.Documents.Blocks.Services;

/// <summary>
/// The live (not deleted) blocks of one page, held in memory while a write applies, so placement, ancestry
/// and sibling order are computed from facts the Application supplies to the Domain (DCT-009/010). A page
/// holds a bounded number of blocks, so the whole set is read once per request.
/// </summary>
internal sealed class PageBlockSet
{
    private readonly Dictionary<Guid, Block> _blocks;

    public PageBlockSet(Guid accountId, Guid workspaceId, Guid pageId, IEnumerable<Block> blocks)
    {
        AccountId = accountId;
        WorkspaceId = workspaceId;
        PageId = pageId;
        _blocks = blocks.ToDictionary(block => block.Id);
    }

    public Guid AccountId { get; }
    public Guid WorkspaceId { get; }
    public Guid PageId { get; }

    public IEnumerable<Block> All => _blocks.Values;

    public Block? Find(Guid id) => _blocks.GetValueOrDefault(id);

    public void Add(Block block) => _blocks[block.Id] = block;

    public void Remove(Block block) => _blocks.Remove(block.Id);

    /// <summary>Live children of a parent (null = page root) in display order, optionally leaving one block out.</summary>
    public List<Block> Siblings(Guid? parentId, Guid? excluding = null) =>
        _blocks.Values
            .Where(block => block.ParentId == parentId && block.Id != excluding)
            .OrderBy(block => block.Position.Value, StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// The position under <paramref name="parentId"/>: the explicit key when given, right after
    /// <paramref name="afterBlockId"/> when given, otherwise last.
    /// </summary>
    public FractionalIndex PositionFor(Guid? parentId, Guid? afterBlockId, string? explicitPosition, Guid? moving)
    {
        if (!string.IsNullOrWhiteSpace(explicitPosition))
        {
            try
            {
                return FractionalIndex.Create(explicitPosition);
            }
            catch (Exception ex) when (ex is Notrelix.Domain.Common.Exceptions.DomainException or ArgumentException or FormatException)
            {
                throw new Notrelix.Application.Common.Exceptions.ValidationException($"'{explicitPosition}' is not a valid block position.");
            }
        }

        var siblings = Siblings(parentId, moving);
        if (afterBlockId is not { } afterId)
            return FractionalIndexGenerator.GenerateKeyBetween(siblings.LastOrDefault()?.Position, null);

        if (Find(afterId) is null) throw new NotFoundException(nameof(Block), afterId);

        var index = siblings.FindIndex(block => block.Id == afterId);
        if (index < 0)
            throw new Notrelix.Application.Common.Exceptions.ValidationException($"Block '{afterId}' is not a sibling to place the block after.");

        return FractionalIndexGenerator.GenerateKeyBetween(
            siblings[index].Position,
            index + 1 < siblings.Count ? siblings[index + 1].Position : null);
    }

    /// <summary>The ancestry facts of a parent block, for the Domain's cycle and scope rules.</summary>
    public BlockAncestorPath PathTo(Guid parentId)
    {
        var parent = Find(parentId) ?? throw new NotFoundException(nameof(Block), parentId);

        var ancestors = new List<Guid>();
        var current = parent.ParentId;
        while (current is { } id)
        {
            if (ancestors.Contains(id)) break;
            ancestors.Add(id);
            current = Find(id)?.ParentId;
        }

        return BlockAncestorPath.Create(AccountId, WorkspaceId, PageId, parent.Id, ancestors);
    }

    /// <summary>
    /// Places a block (new or existing) under <paramref name="parentId"/> at the requested position.
    /// </summary>
    public void Place(Block block, Guid? parentId, Guid? afterBlockId, string? explicitPosition, Guid userId, DateTimeOffset now)
    {
        var position = PositionFor(parentId, afterBlockId, explicitPosition, block.Id);
        if (parentId is { } parent)
            block.MoveUnder(PathTo(parent), position, userId, now);
        else
            block.MoveToRoot(position, userId, now);
    }

    public static BlockContent? ContentOf(JsonElement? content) =>
        content is { } value ? BlockContent.Create(JsonValue.Create(value.GetRawText())) : null;

    public static BlockProperties? PropertiesOf(JsonElement? properties) =>
        properties is { } value ? BlockProperties.Create(JsonValue.Create(value.GetRawText())) : null;

    public static BlockType ParseType(string typeName) =>
        BlockTypeNames.TryParse(typeName, out var type)
            ? type
            : throw new Notrelix.Application.Common.Exceptions.ValidationException($"Unknown block type '{typeName}'.");

    /// <summary>The 412 a stale write gets: the block as it is now, so the editor can rebase without another read.</summary>
    public static PreconditionFailedException Stale(Block current, long expected) => new(
        "The block was changed elsewhere. Reload it and retry.",
        "documents.block.version-conflict",
        expected,
        current.Version)
    {
        Extensions = new Dictionary<string, object?> { ["currentBlock"] = DocumentDtoMapper.ToBlockDto(current) },
    };

    public static void EnsureVersion(Block block, long? expected)
    {
        if (expected is not > 0)
            throw new Notrelix.Application.Common.Exceptions.ValidationException("expectedVersion is required and must be the version of the block you read.");
        if (block.Version != expected)
            throw Stale(block, expected.Value);
    }
}
