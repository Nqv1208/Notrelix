namespace Notrelix.Application.Common.Behaviors;

public class ExceptionMappingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<ExceptionMappingBehavior<TRequest, TResponse>> _logger;
    private readonly IExecutionContext _executionContext;

    public ExceptionMappingBehavior(
        ILogger<ExceptionMappingBehavior<TRequest, TResponse>> logger,
        IExecutionContext executionContext)
    {
        _logger = logger;
        _executionContext = executionContext;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (Exceptions.ValidationException)
        {
            throw;
        }
        catch (Exceptions.NotFoundException)
        {
            throw;
        }
        catch (Exceptions.ForbiddenException ex)
        {
            _logger.LogWarning(
                "Forbidden: {RequestType} CorrelationId={CorrelationId} UserId={UserId} Message={Message}",
                typeof(TRequest).Name,
                _executionContext.CorrelationId,
                _executionContext.UserId,
                ex.Message);
            throw;
        }
        catch (Exceptions.ConflictException)
        {
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning(
                "Unauthorized: {RequestType} CorrelationId={CorrelationId}",
                typeof(TRequest).Name,
                _executionContext.CorrelationId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled exception processing {RequestType} CorrelationId={CorrelationId}",
                typeof(TRequest).Name,
                _executionContext.CorrelationId);
            throw;
        }
    }
}
