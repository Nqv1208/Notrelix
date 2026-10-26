using Notrelix.Domain.Documents.Rules;

namespace Notrelix.Application.Features.Documents.Blocks.Services;

/// <summary>
/// Follows one save of a page's blocks so the page can announce it afterwards: that something changed, and which
/// people a block mentions that it did not mention before (so a person is told once, when first mentioned, and
/// never again for an unchanged or removed mention).
/// </summary>
internal sealed class BlockSaveTracker
{
    private readonly Dictionary<Guid, (long Version, IReadOnlySet<Guid> Mentions)> _before = [];
    private bool _deleted;

    /// <summary>Call for an existing block before it is edited.</summary>
    public void Before(Block block) => _before.TryAdd(block.Id, (block.Version, BlockMentions.Of(block)));

    public void Deleted() => _deleted = true;

    /// <summary>Raises the page's saved fact when the save changed anything; a save that changed nothing says nothing.</summary>
    public void Complete(Page page, IEnumerable<Block> saved, Guid userId, DateTimeOffset now)
    {
        var added = new List<BlockMentionAdded>();
        var changed = _deleted;

        foreach (var block in saved.Where(block => !block.IsDeleted).DistinctBy(block => block.Id))
        {
            if (_before.TryGetValue(block.Id, out var before))
            {
                if (block.Version == before.Version) continue;
                changed = true;
                foreach (var user in BlockMentions.Of(block).Except(before.Mentions))
                    added.Add(new BlockMentionAdded(block.Id, user));
            }
            else
            {
                changed = true;
                foreach (var user in BlockMentions.Of(block))
                    added.Add(new BlockMentionAdded(block.Id, user));
            }
        }

        if (changed) page.RecordBlocksSaved(userId, now, added);
    }
}
