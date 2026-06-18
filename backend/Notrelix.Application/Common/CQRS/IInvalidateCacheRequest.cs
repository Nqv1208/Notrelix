namespace Notrelix.Application.Common.CQRS;

public interface IInvalidateCacheRequest
{
    IReadOnlyCollection<CacheInvalidationKey> GetInvalidationKeys();
}
