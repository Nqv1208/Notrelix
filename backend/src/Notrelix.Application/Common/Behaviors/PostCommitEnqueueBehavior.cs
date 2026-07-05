namespace Notrelix.Application.Common.Behaviors;

public class PostCommitEnqueueBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IPostCommitActionQueue _queue;
    private readonly IRealtimePublisher _publisher;
    private readonly IExecutionContextReader _executionContext;
    private readonly ILogger<PostCommitEnqueueBehavior<TRequest, TResponse>> _logger;

    public PostCommitEnqueueBehavior(
        IPostCommitActionQueue queue,
        IRealtimePublisher publisher,
        IExecutionContextReader executionContext,
        ILogger<PostCommitEnqueueBehavior<TRequest, TResponse>> logger)
    {
        _queue = queue;
        _publisher = publisher;
        _executionContext = executionContext;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var response = await next();

        if (request is IRealtimeRequest realtimeRequest)
        {
            var topic = realtimeRequest.Topic;
            _logger.LogTrace(
                "Enqueuing realtime: {Namespace}/{ResourceType}/{ResourceId} for {RequestType}",
                topic.Namespace, topic.ResourceType, topic.ResourceId, typeof(TRequest).Name);

            _queue.Enqueue(new DelegatePostCommitAction(async ct2 =>
                await _publisher.PublishAsync(topic, response!, ct2)));
        }

        return response;
    }
}
