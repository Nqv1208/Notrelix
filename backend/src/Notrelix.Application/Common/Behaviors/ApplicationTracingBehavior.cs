namespace Notrelix.Application.Common.Behaviors;

public class ApplicationTracingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<ApplicationTracingBehavior<TRequest, TResponse>> _logger;
    private readonly IExecutionContext _executionContext;

    public ApplicationTracingBehavior(
        ILogger<ApplicationTracingBehavior<TRequest, TResponse>> logger,
        IExecutionContext executionContext)
    {
        _logger = logger;
        _executionContext = executionContext;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var workspaceId = (request as IWorkspaceRequest)?.WorkspaceId.ToString();

        using var activity = System.Diagnostics.Activity.Current?.Source.CreateActivity(
            $"App.Handler.{requestName}",
            System.Diagnostics.ActivityKind.Internal)
            ?.AddTag("app.request", requestName)
            ?.AddTag("app.correlation_id", _executionContext.CorrelationId)
            ?.AddTag("app.user_id", _executionContext.UserId?.ToString() ?? "")
            ?.AddTag("app.account_id", _executionContext.AccountId?.ToString() ?? "")
            ?.AddTag("app.workspace_id", workspaceId ?? _executionContext.WorkspaceId?.ToString() ?? "")
            ?.AddTag("app.request_type", request is ICommand ? "Command" : request is IQuery<object> ? "Query" : "Other")
            ?.Start();

        using var _ = _logger.BeginScope(new Dictionary<string, object>
        {
            ["RequestName"] = requestName,
            ["CorrelationId"] = _executionContext.CorrelationId,
            ["UserId"] = _executionContext.UserId?.ToString() ?? "",
            ["AccountId"] = _executionContext.AccountId?.ToString() ?? "",
            ["WorkspaceId"] = workspaceId ?? _executionContext.WorkspaceId?.ToString() ?? "",
            ["RequestType"] = request is ICommand ? "Command" : request is IQuery<object> ? "Query" : "Other",
        });

        _logger.LogInformation("Handling {RequestName}", requestName);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var response = await next();

            stopwatch.Stop();
            _logger.LogInformation(
                "Handled {RequestName} in {ElapsedMs}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok);
            activity?.AddTag("app.elapsed_ms", stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Error handling {RequestName} after {ElapsedMs}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
            activity?.AddTag("app.elapsed_ms", stopwatch.ElapsedMilliseconds);
            activity?.AddTag("app.error", ex.Message);

            throw;
        }
    }
}
