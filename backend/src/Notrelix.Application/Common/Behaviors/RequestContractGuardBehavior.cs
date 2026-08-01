using Notrelix.Application.Common.Requests.Execution;

namespace Notrelix.Application.Common.Behaviors;

public sealed class RequestContractGuardBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var profile = RequestExecutionClassifier.Classify(request);

        var violations = RequestContractValidator.Validate(profile);

        if (violations.Count > 0)
        {
            throw new SecurityMisconfigurationException(
                $"{profile.RequestName} has invalid request contract. " +
                string.Join(" ", violations));
        }

        return next();
    }
}
