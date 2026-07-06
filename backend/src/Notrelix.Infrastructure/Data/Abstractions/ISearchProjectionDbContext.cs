using Notrelix.Infrastructure.Data.Projections.Search;
// ReSharper disable InconsistentNaming — intentionally mirrors DbContext member naming

namespace Notrelix.Infrastructure.Data.Abstractions;

public interface ISearchProjectionDbContext
{
    DbSet<SearchDocumentRecord> SearchDocuments { get; }
    DbSet<SearchIndexJobRecord> SearchIndexJobs { get; }
}