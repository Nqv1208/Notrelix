using Notrelix.Application.Common.Requests.Execution;

namespace Notrelix.Application.Common.Behaviors;

public sealed class TokenValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var profile = RequestExecutionClassifier.Classify(request);

        if (profile.IsTokenScoped)
        {
            ValidateTokenProperty(request, profile);
        }

        return next();
    }

    private static void ValidateTokenProperty(TRequest request, RequestExecutionProfile profile)
    {
        var tokenProperty = typeof(TRequest).GetProperty("Token");

        if (tokenProperty is null || tokenProperty.PropertyType != typeof(string))
        {
            throw new SecurityMisconfigurationException(
                $"{profile.RequestName} implements ITokenScopedRequest but has no string Token property.");
        }

        var tokenValue = tokenProperty.GetValue(request) as string;

        if (string.IsNullOrWhiteSpace(tokenValue))
        {
            throw new SecurityMisconfigurationException(
                $"{profile.RequestName} has an empty or null Token. Token-scoped requests must provide a non-empty token.");
        }
    }
}
