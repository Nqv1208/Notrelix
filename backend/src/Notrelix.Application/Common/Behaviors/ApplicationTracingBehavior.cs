using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Common.Requests.Execution;

namespace Notrelix.Application.Common.Behaviors;

public class ApplicationTracingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IRequestDescriptorRegistry _descriptors;
    private readonly ILogger<ApplicationTracingBehavior<TRequest, TResponse>> _logger;
    private readonly IExecutionContextReader _executionContext;
    private readonly IHostEnvironment _hostEnvironment;

    public ApplicationTracingBehavior(
        IRequestDescriptorRegistry descriptors,
        ILogger<ApplicationTracingBehavior<TRequest, TResponse>> logger,
        IExecutionContextReader executionContext,
        IHostEnvironment hostEnvironment)
    {
        _descriptors = descriptors;
        _logger = logger;
        _executionContext = executionContext;
        _hostEnvironment = hostEnvironment;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var descriptor = _descriptors.GetRequired(typeof(TRequest));
        var workspaceId = (request as IWorkspaceRequest)?.WorkspaceId.ToString();

        using var activity = PipelineActivitySource.Instance.StartActivity(
            $"App.Handler.{requestName}",
            System.Diagnostics.ActivityKind.Internal)
            ?.AddTag("app.request", requestName)
            ?.AddTag("app.correlation_id", _executionContext.CorrelationId)
            ?.AddTag("app.user_id", _executionContext.UserId?.ToString() ?? "")
            ?.AddTag("app.account_id", _executionContext.AccountId?.ToString() ?? "")
            ?.AddTag("app.workspace_id", workspaceId ?? _executionContext.WorkspaceId?.ToString() ?? "")
            ?.AddTag("request.name", requestName)
            ?.AddTag("request.kind", descriptor.Kind.ToString())
            ?.AddTag("principal.kind", descriptor.Principal.ToString())
            ?.AddTag("scope.kind", descriptor.Scope.ToString())
            ?.AddTag("data_access.kind", descriptor.DataAccess.ToString())
            ?.AddTag("deployment.environment", _hostEnvironment.EnvironmentName)
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
            TResponse response;
            using (PipelineActivitySource.Instance.StartActivity("handler"))
            {
                response = await next();
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "Handled {RequestName} in {ElapsedMs}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok);
            activity?.AddTag("pipeline.outcome", "success");
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
            activity?.AddTag("pipeline.outcome", $"failure:{ex.GetType().Name}");
            activity?.AddTag("app.elapsed_ms", stopwatch.ElapsedMilliseconds);
            activity?.AddTag("app.error", ex.Message);

            throw;
        }
    }
}
