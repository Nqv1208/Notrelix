namespace Notrelix.Application.Common.CQRS;

/// <summary>
/// Marker for private cacheable queries that require authorization before cache lookup.
/// AuthorizedCacheBehavior runs AFTER AuthorizationBehavior, ensuring only authorized data is cached.
/// </summary>
public interface IAuthorizedCacheableRequest
{
    string AuthorizedCacheKey { get; }
    TimeSpan AuthorizedCacheTtl { get; }
}
