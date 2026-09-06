using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Public.PageAuthorization;

namespace Notrelix.Infrastructure.Data.Authz;

/// <summary>
/// Documents-owned implementation of the published page authorization facts
/// contract. Reads the Documents-managed docs.pages storage and projects the
/// authorization-relevant facts; consumers never touch Documents tables.
/// </summary>
public sealed class PostgresPageAuthorizationFacts : IPageAuthorizationFacts
{
    private readonly IDocumentDbContext _context;

    public PostgresPageAuthorizationFacts(IDocumentDbContext context)
    {
        _context = context;
    }

    public async Task<PageAuthorizationFact?> ResolveAsync(Guid pageId, CancellationToken cancellationToken)
    {
        var row = await _context.Pages
            .IgnoreQueryFilters()
            .Where(p => p.Id == pageId)
            .Select(p => new { p.Id, p.WorkspaceId, p.DeletedAt, p.Status, p.Visibility })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        return new PageAuthorizationFact(
            row.Id,
            row.WorkspaceId,
            Exists: true,
            IsActive: row.DeletedAt is null && row.Status != Domain.Documents.Pages.PageStatus.Archived,
            Visibility: row.Visibility.ToString());
    }
}