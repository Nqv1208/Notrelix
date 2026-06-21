using MediatR;
using Microsoft.Extensions.Logging;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.CQRS;

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

            await _realtimePublisher.PublishAsync(
                realtimeRequest.Topic,
                new { Request = request, Response = response },
                cancellationToken);

            return response;
        }

        return await next();
    }
}
