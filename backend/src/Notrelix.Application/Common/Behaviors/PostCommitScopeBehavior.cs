namespace Notrelix.Application.Common.Behaviors;

public class PostCommitScopeBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IPostCommitActionQueue _queue;
    private readonly ILogger<PostCommitScopeBehavior<TRequest, TResponse>> _logger;

    public PostCommitScopeBehavior(
        IPostCommitActionQueue queue,
        ILogger<PostCommitScopeBehavior<TRequest, TResponse>> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        _queue.BeginScope();

        try
        {
            var response = await next();

            await _queue.FlushAsync(ct);

            return response;
        }
        catch
        {
            _queue.Clear();
            throw;
        }
        finally
        {
            _queue.EndScope();
        }
    }
}
