namespace Notrelix.Application.Common.CQRS;

public interface IPublicCacheableQuery<out TResponse> : IQuery<TResponse>
{
    object CacheIdentity { get; }

    TimeSpan? Ttl => null;
}
