namespace Notrelix.Infrastructure.Search.Providers;

/// <summary>
/// Skeleton PostgreSQL search provider (v4 §14). Real implementation issues
/// permission-aware full-text queries; indexing happens async via outbox/job.
/// Swapping providers (OpenSearch/Meilisearch) must not affect Application.
/// Not yet wired.
/// </summary>
public sealed class PostgresSearchProvider
{
    // TODO(v4 §14): implement permission-aware search query + async indexing.
    // Add the Application-side interface (ISearchQueryService) when implemented.
}
