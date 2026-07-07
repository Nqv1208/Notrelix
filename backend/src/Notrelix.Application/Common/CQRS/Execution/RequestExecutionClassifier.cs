using Notrelix.Application.Common.CQRS.Scoping;

namespace Notrelix.Application.Common.CQRS.Execution;

public static class RequestExecutionClassifier
{
    public static RequestExecutionProfile Classify<TRequest>(TRequest request)
        where TRequest : notnull
    {
        var requestType = request.GetType();
        var isPublicCacheable = requestType.GetInterfaces()
            .Any(i => i.IsGenericType
                  && i.GetGenericTypeDefinition() == typeof(IPublicCacheableQuery<>));

        return new RequestExecutionProfile(
            RequestName: typeof(TRequest).Name,
            IsAnonymous: request is IAnonymousRequest,
            IsSystemInternal: request is ISystemInternalRequest,
            IsGlobal: request is IGlobalRequest,
            IsAccountScoped: request is IAccountRequest,
            IsWorkspaceScoped: request is IWorkspaceRequest,
            IsResourceScoped: request is IResourceScopedRequest,
            IsTransactional: request is ITransactionalRequest,
            IsRlsRead: request is IRlsReadRequest,
            RequiresPermission: request is IRequirePermission,
            RequiresSubscription: request is IRequireSubscription,
            RequiresFeature: request is IRequireFeature,
            IsPublicCacheable: isPublicCacheable,
            IsAuthorizedCacheable: request is IAuthorizedCacheableRequest);
    }
}
