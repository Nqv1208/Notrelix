using Notrelix.Application.Common.CQRS.Scoping;

namespace Notrelix.Application.Common.Behaviors;

public sealed class RequestContractGuardBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var isGlobal = request is IGlobalRequest;
        var isAnonymous = request is IAnonymousRequest;
        var isAccount = request is IAccountRequest;
        var isWorkspace = request is IWorkspaceRequest;
        var isResource = request is IResourceScopedRequest;
        var requiresPermission = request is IRequirePermission;
        var isPublicCache = request is IPublicCacheableQuery<TResponse>;
        var isAuthorizedCache = request is IAuthorizedCacheableRequest;

        if (isGlobal && (isAccount || isWorkspace || isResource))
        {
            throw Misconfigured(
                request,
                "Global request cannot also be account/workspace/resource scoped.");
        }

        if (isGlobal && requiresPermission)
        {
            throw Misconfigured(
                request,
                "Global request cannot require tenant/resource permission.");
        }

        if (isAnonymous && (isAccount || isWorkspace || isResource))
        {
            throw Misconfigured(
                request,
                "Anonymous request cannot be tenant/resource scoped.");
        }

        if (isPublicCache && (isAccount || isWorkspace || isResource))
        {
            throw Misconfigured(
                request,
                "Public cache cannot be used for tenant/account/workspace/resource scoped requests.");
        }

        if (isPublicCache && isAuthorizedCache)
        {
            throw Misconfigured(
                request,
                "A request cannot use both public cache and authorized/private cache.");
        }

        return next();
    }

    private static SecurityMisconfigurationException Misconfigured<T>(T request, string reason)
        where T : notnull
        => new($"{typeof(T).Name} has invalid request contract. {reason}");
}
