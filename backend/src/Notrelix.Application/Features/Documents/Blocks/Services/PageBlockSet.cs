using System.Text.Json;
using Notrelix.Application.Features.Documents.Common;
using Notrelix.Domain.Documents.Rules;
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

    /// <summary>Refuses a set that holds more live blocks than a page may.</summary>
    public void EnsureWithinLimit() => PageRules.EnsureBlockCountWithinLimit(_blocks.Count);

    /// <summary>
    /// Deletes a block and every block under it, so no live block is left under a deleted parent. Every
    /// route that deletes a block goes through here, so the single and the batch route behave the same.
    /// </summary>
    public void Delete(Block block, Guid userId, DateTimeOffset now)
    {
        var doomed = new List<Block> { block };
        for (var i = 0; i < doomed.Count; i++)
            doomed.AddRange(Siblings(doomed[i].Id));

        foreach (var victim in doomed)
        {
            victim.Delete(userId, now);
            Remove(victim);
        }
    }

    /// <summary>Live children of a parent (null = page root) in display order, optionally leaving one block out.</summary>
    public List<Block> Siblings(Guid? parentId, Guid? excluding = null) =>
        _blocks.Values
            .Where(block => block.ParentId == parentId && block.Id != excluding)
            .OrderBy(block => block.Position.Value, StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// The position under <paramref name="parentId"/>: right after <paramref name="afterBlockId"/>, right before
    /// <paramref name="beforeBlockId"/> (both named must be neighbours), otherwise last. The server makes the key; a
    /// client key is kept only when a named sibling bounds it and it lies strictly between the siblings the request
    /// names (ordering is server-authoritative).
    /// </summary>
    public FractionalIndex PositionFor(Guid? parentId, Guid? afterBlockId, string? explicitPosition, Guid? moving, Guid? beforeBlockId = null)
    {
        var (lower, upper) = Bounds(Siblings(parentId, moving), afterBlockId, beforeBlockId);
        if (string.IsNullOrWhiteSpace(explicitPosition))
            return KeyBetween(lower, upper);

        FractionalIndex key;
        try
        {
            key = FractionalIndex.Create(explicitPosition);
        }
        catch (Exception ex) when (ex is Notrelix.Domain.Common.Exceptions.DomainException or ArgumentException or FormatException)
        {
            throw new Notrelix.Application.Common.Exceptions.ValidationException($"'{explicitPosition}' is not a valid block position.");
        }

        if (afterBlockId is null && beforeBlockId is null)
            throw new Notrelix.Application.Common.Exceptions.ValidationException("A position key needs the sibling it goes next to.");
        if (!IsBetween(key, lower, upper))
            throw new Notrelix.Application.Common.Exceptions.ValidationException($"'{explicitPosition}' does not lie between the siblings the request names.");
        return key;
    }

    /// <summary>
    /// A key strictly between the bounds. Bounds that are not in order (two siblings on one key) leave no room
    /// for one: that is a conflict the caller can reload from, not a failure of the server.
    /// </summary>
    private static FractionalIndex KeyBetween(FractionalIndex? lower, FractionalIndex? upper)
    {
        try
        {
            return FractionalIndexGenerator.GenerateKeyBetween(lower, upper);
        }
        catch (ArgumentException)
        {
            throw new ConflictException("The blocks around the position share a sort key. Reload and retry.", PositionConflictCode);
        }
    }

    /// <summary>The error code of a position that two blocks would share.</summary>
    public const string PositionConflictCode = "documents.block.position-conflict";

    /// <summary>The keys of the siblings a placement lies between; null at the start or end of the list.</summary>
    private (FractionalIndex? Lower, FractionalIndex? Upper) Bounds(List<Block> siblings, Guid? afterBlockId, Guid? beforeBlockId)
    {
        if (afterBlockId is { } afterId)
        {
            var index = SiblingIndex(siblings, afterId);
            var next = index + 1 < siblings.Count ? siblings[index + 1] : null;
            if (beforeBlockId is { } named && next?.Id != named)
                throw new Notrelix.Application.Common.Exceptions.ValidationException("The blocks named to go between are not neighbours.");
            return (siblings[index].Position, next?.Position);
        }

        if (beforeBlockId is { } beforeId)
        {
            var index = SiblingIndex(siblings, beforeId);
            return (index > 0 ? siblings[index - 1].Position : null, siblings[index].Position);
        }

        return (siblings.LastOrDefault()?.Position, null);
    }

    private int SiblingIndex(List<Block> siblings, Guid blockId)
    {
        if (Find(blockId) is null) throw new NotFoundException(nameof(Block), blockId);
        var index = siblings.FindIndex(block => block.Id == blockId);
        return index >= 0
            ? index
            : throw new Notrelix.Application.Common.Exceptions.ValidationException($"Block '{blockId}' is not a sibling to place the block next to.");
    }

    private static bool IsBetween(FractionalIndex key, FractionalIndex? lower, FractionalIndex? upper) =>
        (lower is null || key.CompareTo(lower) > 0) && (upper is null || key.CompareTo(upper) < 0);

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
    /// Places an existing block under <paramref name="parentId"/> next to the siblings named. A block that already
    /// sits between them under that parent (and whose key the request does not change) stays as it is: an
    /// unchanged place is a no-op. Returns whether the block moved.
    /// </summary>
    public bool Place(Block block, Guid? parentId, Guid? afterBlockId, string? explicitPosition, Guid userId, DateTimeOffset now, Guid? beforeBlockId = null)
    {
        var keepsKey = string.IsNullOrWhiteSpace(explicitPosition) || explicitPosition == block.Position.Value;
        if (keepsKey && block.ParentId == parentId)
        {
            var (lower, upper) = Bounds(Siblings(parentId, block.Id), afterBlockId, beforeBlockId);
            if (IsBetween(block.Position, lower, upper)) return false;
        }

        var position = PositionFor(parentId, afterBlockId, explicitPosition, block.Id, beforeBlockId);
        if (parentId is { } parent)
            block.MoveUnder(PathTo(parent), position, userId, now);
        else
            block.MoveToRoot(position, userId, now);
        return true;
    }

    /// <summary>
    /// Puts a block under <paramref name="parentId"/> at the key a stored version captured. Restoring a version
    /// replays an authoritative tree, so the key is kept as it is (no sibling to name); a block already there is
    /// a no-op. Returns whether the block moved.
    /// </summary>
    public bool PlaceAtCapturedKey(Block block, Guid? parentId, string position, Guid userId, DateTimeOffset now)
    {
        if (block.ParentId == parentId && block.Position.Value == position) return false;

        var key = FractionalIndex.Create(position);
        if (parentId is { } parent)
            block.MoveUnder(PathTo(parent), key, userId, now);
        else
            block.MoveToRoot(key, userId, now);
        return true;
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
