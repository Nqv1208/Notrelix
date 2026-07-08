namespace Notrelix.Application.Common.Behaviors;

public class ConcurrencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IResourceVersionReader _versionReader;
    private readonly ILogger<ConcurrencyBehavior<TRequest, TResponse>> _logger;

    public ConcurrencyBehavior(
        IResourceVersionReader versionReader,
        ILogger<ConcurrencyBehavior<TRequest, TResponse>> logger)
    {
        _versionReader = versionReader;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is IExpectedVersionRequest expectedVersionRequest)
        {
            if (expectedVersionRequest.ExpectedVersion > 0)
            {
                try
                {
                    var currentVersion = await _versionReader.GetVersionAsync(expectedVersionRequest.Resource, ct);

                    if (currentVersion.HasValue && currentVersion.Value != expectedVersionRequest.ExpectedVersion)
                    {
                        throw new ConflictException(
                            $"Resource {expectedVersionRequest.Resource} version mismatch. " +
                            $"Expected {expectedVersionRequest.ExpectedVersion}, got {currentVersion.Value}.");
                    }
                }
                catch (NotSupportedException ex)
                {
                    _logger.LogWarning(
                        "Version check skipped for {RequestType}: {Message}",
                        typeof(TRequest).Name,
                        ex.Message);
                }
            }
        }

        return await next();
    }
}
