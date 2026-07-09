namespace Notrelix.Application.Common.Behaviors;

public class ConcurrencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IResourceVersionReader _versionReader;

    public ConcurrencyBehavior(IResourceVersionReader versionReader)
    {
        _versionReader = versionReader;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is IExpectedVersionRequest expectedVersionRequest)
        {
            if (expectedVersionRequest.ExpectedVersion <= 0)
            {
                throw new Exceptions.ValidationException(
                    $"ExpectedVersion must be a positive value for {typeof(TRequest).Name}. " +
                    $"Request {typeof(TRequest).Name} implements IExpectedVersionRequest " +
                    $"but provides ExpectedVersion={expectedVersionRequest.ExpectedVersion}.");
            }

            var currentVersion = await _versionReader.GetVersionAsync(expectedVersionRequest.Resource, ct);

            if (!currentVersion.HasValue)
            {
                throw new NotFoundException(
                    $"Resource {expectedVersionRequest.Resource} not found. " +
                    $"Cannot verify concurrency version for {typeof(TRequest).Name}.");
            }

            if (currentVersion.Value != expectedVersionRequest.ExpectedVersion)
            {
                throw new ConflictException(
                    $"Resource {expectedVersionRequest.Resource} version mismatch. " +
                    $"Expected {expectedVersionRequest.ExpectedVersion}, got {currentVersion.Value}.");
            }
        }

        return await next();
    }
}
