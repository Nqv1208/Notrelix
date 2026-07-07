using Notrelix.Application.Common.CQRS.Execution;

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

        Validate(profile);

        return next();
    }

    private static void Validate(RequestExecutionProfile profile)
    {
        if (profile.IsAnonymous && profile.IsSystemInternal)
        {
            Throw(profile, "Request cannot be both anonymous and system-internal.");
        }

        if (profile.IsGlobal && profile.IsTenantScoped)
        {
            Throw(profile, "Global request cannot also be account/workspace/resource scoped.");
        }

        if (profile.IsGlobal && profile.RequiresPermission)
        {
            Throw(profile, "Global request cannot require tenant/resource permission.");
        }

        if (profile.IsAnonymous && profile.IsTenantScoped)
        {
            Throw(profile, "Anonymous request cannot be tenant/resource scoped.");
        }

        if (profile.IsPublicCacheable && profile.IsTenantScoped)
        {
            Throw(profile, "Public cache cannot be used for tenant/account/workspace/resource scoped requests.");
        }

        if (profile.IsPublicCacheable && profile.IsAuthorizedCacheable)
        {
            Throw(profile, "A request cannot use both public cache and authorized/private cache.");
        }
    }

    private static void Throw(RequestExecutionProfile profile, string reason)
    {
        throw new SecurityMisconfigurationException(
            $"{profile.RequestName} has invalid request contract. {reason}");
    }
}
