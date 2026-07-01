namespace Notrelix.Application.Common.Behaviors;

public class ExceptionMappingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<ExceptionMappingBehavior<TRequest, TResponse>> _logger;

    public ExceptionMappingBehavior(ILogger<ExceptionMappingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
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
        catch (Exceptions.ForbiddenException)
        {
            throw;
        }
        catch (Exceptions.ConflictException)
        {
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception processing {RequestType}", typeof(TRequest).Name);
            throw;
        }
    }
}
