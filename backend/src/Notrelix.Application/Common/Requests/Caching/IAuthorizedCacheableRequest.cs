namespace Notrelix.Application.Common.Requests;

public interface IAuthorizedCacheableRequest
{
    AuthorizedCacheScope CacheScope { get; }

    object CacheIdentity { get; }

    TimeSpan? CacheTtl => null;
}
