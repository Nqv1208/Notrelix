using Notrelix.Application.Common.CQRS.Caching;

namespace Notrelix.Application.Common.CQRS;

public interface IAuthorizedCacheableRequest
{
    AuthorizedCacheScope CacheScope { get; }

    object CacheIdentity { get; }

    TimeSpan? CacheTtl => null;
}
