namespace Notrelix.Application.Common.Behaviors;

public class ExceptionMappingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<ExceptionMappingBehavior<TRequest, TResponse>> _logger;
    private readonly IExecutionContextReader _executionContext;

    public ExceptionMappingBehavior(
        ILogger<ExceptionMappingBehavior<TRequest, TResponse>> logger,
        IExecutionContextReader executionContext)
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
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(
                "Concurrency conflict: {RequestType} CorrelationId={CorrelationId} Message={Message}",
                typeof(TRequest).Name,
                _executionContext.CorrelationId,
                ex.Message);
            throw new Exceptions.ConflictException(
                "The resource was modified by another request. Reload and retry.");
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            _logger.LogWarning(
                "Unique constraint violation: {RequestType} CorrelationId={CorrelationId} Message={Message}",
                typeof(TRequest).Name,
                _executionContext.CorrelationId,
                ex.Message);
            throw new Exceptions.ConflictException(
                "A resource with the same unique identity already exists.");
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

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
            || message.Contains("23505");
    }
}
