namespace Notrelix.Application.Common.CQRS;

public interface IPublicCacheableQuery<out TResponse> : IQuery<TResponse>
{
    string CacheKey { get; }
    TimeSpan? Ttl { get; }
}
