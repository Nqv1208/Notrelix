namespace Notrelix.Application.Common.Requests;

public interface IPublicCacheableQuery<out TResponse> : IQuery<TResponse>
{
    object CacheIdentity { get; }

    TimeSpan? Ttl => null;
}
