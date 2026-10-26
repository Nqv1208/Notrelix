using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Public.PageBlocks;

namespace Notrelix.Infrastructure.CrossContext.Collaboration.Documents;

/// <summary>Documents-owned implementation of the published page block fact consumed by Collaboration.</summary>
public sealed class PageBlockFactsAdapter(IDocumentDbContext context) : IPageBlockFacts
{
    public async Task<bool> BlockBelongsToPageAsync(
        Guid workspaceId, Guid pageId, Guid blockId, CancellationToken cancellationToken)
    {
        return await context.Blocks
            .AsNoTracking()
            .AnyAsync(
                b => b.Id == blockId
                    && b.PageId == pageId
                    && b.WorkspaceId == workspaceId
                    && b.DeletedAt == null,
                cancellationToken);
    }
}
