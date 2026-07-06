namespace Notrelix.Application.Common.Behaviors;

public class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly ILogger<IdempotencyBehavior<TRequest, TResponse>> _logger;

    public IdempotencyBehavior(IIdempotencyStore idempotencyStore, ILogger<IdempotencyBehavior<TRequest, TResponse>> logger)
    {
        _idempotencyStore = idempotencyStore;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is IIdempotentRequest idempotentRequest)
        {
            var key = idempotentRequest.IdempotencyKey;

            var acquired = await _idempotencyStore.TryAcquireLockAsync(key, TimeSpan.FromMinutes(5));
            if (!acquired)
            {
                var existingResult = await _idempotencyStore.GetResultAsync(key);
                if (existingResult is TResponse cachedResponse)
                {
                    return cachedResponse;
                }

                throw new ConflictException($"Idempotency key '{key}' is already being processed.");
            }

            try
            {
                var response = await next();

                await _idempotencyStore.SetResultAsync(key, response);

                return response;
            }
            catch
            {
                await _idempotencyStore.ReleaseLockAsync(key);
                throw;
            }
        }

        return await next();
    }
}
