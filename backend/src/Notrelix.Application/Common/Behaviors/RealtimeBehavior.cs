using MediatR;
using Microsoft.Extensions.Logging;

namespace Notrelix.Application.Common.Behaviors;

public class RealtimeBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IRealtimePublisher _realtimePublisher;
    private readonly ILogger<RealtimeBehavior<TRequest, TResponse>> _logger;

    public RealtimeBehavior(IRealtimePublisher realtimePublisher, ILogger<RealtimeBehavior<TRequest, TResponse>> logger)
    {
        _realtimePublisher = realtimePublisher;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is IRealtimeRequest realtimeRequest)
        {
            var response = await next();

            try
            {
                await _realtimePublisher.PublishAsync(
                    realtimeRequest.Topic,
                    new { Request = request, Response = response },
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Realtime publish failed after {RequestType}; data already committed",
                    typeof(TRequest).Name);
            }

            return response;
        }

        return await next();
    }
}
