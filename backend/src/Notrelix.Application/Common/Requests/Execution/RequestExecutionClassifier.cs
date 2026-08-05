using Notrelix.Application.Common.Requests.Scoping;

namespace Notrelix.Application.Common.Requests.Execution;

/// <summary>
/// Classifies a request into a categorical <see cref="RequestExecutionProfile"/>
/// by inspecting its marker interfaces. Each category (Principal, Scope, DataAccess, Cache)
/// must resolve to exactly one value.
/// </summary>
public static class RequestExecutionClassifier
{
    public static RequestExecutionProfile Classify<TRequest>(TRequest request)
        where TRequest : notnull
    {
        var requestType = request.GetType();

        var kind = ClassifyKind(request);
        var principal = ClassifyPrincipal(request);
        var scope = ClassifyScope(request);
        var dataAccess = ClassifyDataAccess(request);
        var cache = ClassifyCache(request);

        var isPublicCacheable = requestType.GetInterfaces()
            .Any(i => i.IsGenericType
                  && i.GetGenericTypeDefinition() == typeof(IPublicCacheableQuery<>));

        return new RequestExecutionProfile(
            RequestName: requestType.Name,
            Kind: kind,
            Principal: principal,
            Scope: scope,
            DataAccess: dataAccess,
            Cache: cache,
            RequiresPermission: request is IRequirePermission,
            RequiresVerifiedEmail: request is IRequireVerifiedEmail,
            RequiresSubscription: request is IRequireSubscription,
            RequiresFeature: request is IRequireFeature,
            RequiresExpectedVersion: request is IExpectedVersionRequest,
            IsIdempotent: request is IIdempotentRequest,
            EmitsRealtime: request is IRealtimeRequest,
            EnqueuesPostCommit: request is IRealtimeRequest);
    }

    private static ApplicationRequestKind ClassifyKind(object request)
    {
        var type = request.GetType();
        var isQuery = type.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>));

        return isQuery ? ApplicationRequestKind.Query : ApplicationRequestKind.Command;
    }

    private static ApplicationPrincipalKind ClassifyPrincipal(object request)
    {
        var count = 0;
        ApplicationPrincipalKind result = ApplicationPrincipalKind.Authenticated;

        if (request is IAnonymousRequest)
        {
            count++;
            result = ApplicationPrincipalKind.Anonymous;
        }

        if (request is ISystemInternalRequest)
        {
            count++;
            result = ApplicationPrincipalKind.System;
        }

        // IAuthenticatedRequest is the default — explicit implementation counts as a marker
        if (request is IAuthenticatedRequest)
        {
            count++;
            result = ApplicationPrincipalKind.Authenticated;
        }

        if (count > 1)
        {
            throw new SecurityMisconfigurationException(
                $"{request.GetType().Name} implements multiple principal markers. " +
                "Exactly one of IAnonymousRequest, IAuthenticatedRequest, or ISystemInternalRequest is allowed.");
        }

        // count == 0 → default to Authenticated (the secure default)
        return result;
    }

    private static ApplicationScopeKind ClassifyScope(object request)
    {
        var count = 0;
        ApplicationScopeKind result = ApplicationScopeKind.Global;

        if (request is IGlobalRequest)
        {
            count++;
            result = ApplicationScopeKind.Global;
        }

        if (request is IAccountRequest)
        {
            count++;
            result = ApplicationScopeKind.Account;
        }

        if (request is IWorkspaceRequest)
        {
            count++;
            result = ApplicationScopeKind.Workspace;
        }

        if (request is IResourceScopedRequest)
        {
            count++;
            result = ApplicationScopeKind.Resource;
        }

        if (request is ITokenScopedRequest)
        {
            count++;
            result = ApplicationScopeKind.Token;
        }

        if (count > 1)
        {
            throw new SecurityMisconfigurationException(
                $"{request.GetType().Name} implements multiple scope markers. " +
                "Exactly one of IGlobalRequest, IAccountRequest, IWorkspaceRequest, " +
                "IResourceScopedRequest, or ITokenScopedRequest is allowed.");
        }

        if (count == 0)
        {
            throw new SecurityMisconfigurationException(
                $"{request.GetType().Name} has no scope marker. " +
                "Every request must implement exactly one of IGlobalRequest, IAccountRequest, " +
                "IWorkspaceRequest, IResourceScopedRequest, or ITokenScopedRequest.");
        }

        return result;
    }

    private static ApplicationDataAccessKind ClassifyDataAccess(object request)
    {
        var isTransactional = request is ITransactionalRequest;
        var isRlsRead = request is IRlsReadRequest;

        if (isTransactional && isRlsRead)
        {
            // Transactional takes precedence; RLS is applied within the transaction
            return ApplicationDataAccessKind.Transactional;
        }

        if (isTransactional)
            return ApplicationDataAccessKind.Transactional;

        if (isRlsRead)
            return ApplicationDataAccessKind.ReadOnly;

        return ApplicationDataAccessKind.None;
    }

    private static ApplicationCacheKind ClassifyCache(object request)
    {
        var isPublic = request.GetType().GetInterfaces()
            .Any(i => i.IsGenericType
                  && i.GetGenericTypeDefinition() == typeof(IPublicCacheableQuery<>));
        var isAuthorized = request is IAuthorizedCacheableRequest;

        if (isPublic && isAuthorized)
        {
            throw new SecurityMisconfigurationException(
                $"{request.GetType().Name} implements both IPublicCacheableQuery and IAuthorizedCacheableRequest. " +
                "Exactly one cache mode is allowed.");
        }

        if (isPublic)
            return ApplicationCacheKind.Public;

        if (isAuthorized)
            return ApplicationCacheKind.Authorized;

        return ApplicationCacheKind.None;
    }
}
