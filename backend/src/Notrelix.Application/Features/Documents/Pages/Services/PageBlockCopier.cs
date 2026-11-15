using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Blocks.Services;
using Notrelix.Application.Features.Documents.Versions.Services;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Features.Documents.Pages.Services;

/// <summary>
/// Copies a page's blocks into another page with new ids (templates and duplicate share it). Images, files and
/// videos are not copied: an attachment belongs to the page it was uploaded to. The result says how many blocks
/// were left out, the blocks under a dropped block included.
/// </summary>
internal static class PageBlockCopier
{
    private static readonly HashSet<BlockType> AttachmentBlocks = [BlockType.Image, BlockType.File, BlockType.Video];

    public static int Copy(IDocumentDbContext context, Page page, IReadOnlyList<SnapshotBlock> blocks, Guid userId, DateTimeOffset now)
    {
        // A dropped block takes the blocks under it along.
        var skipped = new HashSet<Guid>(blocks.Where(b => AttachmentBlocks.Contains(b.Type)).Select(b => b.Id));
        var grew = true;
        while (grew)
        {
            grew = false;
            foreach (var block in blocks)
                if (block.ParentId is { } parent && skipped.Contains(parent) && skipped.Add(block.Id)) grew = true;
        }

        var map = new Dictionary<Guid, Guid>();
        var set = new PageBlockSet(page.AccountId, page.WorkspaceId, page.Id, []);
        foreach (var block in blocks.Where(b => !skipped.Contains(b.Id)))
        {
            var id = Guid.CreateVersion7();
            map[block.Id] = id;
            var content = PageBlockSet.ContentOf(ToElement(block.Content));
            var properties = PageBlockSet.PropertiesOf(ToElement(block.Properties));
            var position = FractionalIndex.Create(block.Position);

            var created = block.ParentId is { } parent && map.TryGetValue(parent, out var newParent)
                ? Block.CreateChild(page.AccountId, page.WorkspaceId, page.Id, block.Type, content!, position, userId, now, set.PathTo(newParent), properties, id)
                : Block.CreateRoot(page.AccountId, page.WorkspaceId, page.Id, block.Type, content!, position, userId, now, properties, id);
            context.Blocks.Add(created);
            set.Add(created);
        }

        return skipped.Count;
    }

    private static System.Text.Json.JsonElement ToElement(string json)
    {
        using var document = System.Text.Json.JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
