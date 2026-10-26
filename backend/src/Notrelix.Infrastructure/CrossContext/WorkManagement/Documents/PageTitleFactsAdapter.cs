using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Public.PageTitles;

namespace Notrelix.Infrastructure.CrossContext.WorkManagement.Documents;

/// <summary>Documents-owned implementation of the published page title fact consumed by Work Management.</summary>
public sealed class PageTitleFactsAdapter(IDocumentDbContext context) : IPageTitleFacts
{
    public async Task<IReadOnlyDictionary<Guid, string>> TitlesAsync(
        Guid workspaceId, IReadOnlyCollection<Guid> pageIds, CancellationToken cancellationToken)
    {
        if (pageIds.Count == 0) return new Dictionary<Guid, string>();
        return await context.Pages
            .AsNoTracking()
            .Where(p => p.WorkspaceId == workspaceId && pageIds.Contains(p.Id) && p.DeletedAt == null)
            .ToDictionaryAsync(p => p.Id, p => p.Title, cancellationToken);
    }
}
