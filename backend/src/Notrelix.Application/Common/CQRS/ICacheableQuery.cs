namespace Notrelix.Application.Common.CQRS;

public interface ICacheableQuery<out TResponse> : IQuery<TResponse>
{
    string CacheKey { get; }
    TimeSpan? Ttl { get; }
}
