using System.Reflection;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Common.Requests.Security;

namespace Notrelix.Application.Common.Requests.Execution;

public static class RequestDescriptorValidator
{
    public static RequestDescriptor Create(Type requestType)
    {
        ArgumentNullException.ThrowIfNull(requestType);

        var principal = SingleClassification(
            requestType,
            "principal",
            (typeof(IAnonymousRequest), ApplicationPrincipalKind.Anonymous),
            (typeof(IAuthenticatedRequest), ApplicationPrincipalKind.Authenticated),
            (typeof(ISystemInternalRequest), ApplicationPrincipalKind.System));
        var scope = SingleClassification(
            requestType,
            "scope",
            (typeof(IGlobalRequest), ApplicationScopeKind.Global),
            (typeof(IAccountRequest), ApplicationScopeKind.Account),
            (typeof(IWorkspaceRequest), ApplicationScopeKind.Workspace),
            (typeof(IResourceScopedRequest), ApplicationScopeKind.Resource),
            (typeof(ITokenScopedRequest), ApplicationScopeKind.Token));
        var dataAccess = SingleClassification(
            requestType,
            "data access",
            (typeof(INoDataRequest), ApplicationDataAccessKind.None),
            (typeof(IReadRequest), ApplicationDataAccessKind.Read),
            (typeof(IWriteRequest), ApplicationDataAccessKind.Write));

        var kind = ImplementsOpenGeneric(requestType, typeof(IQuery<>))
            ? ApplicationRequestKind.Query
            : ApplicationRequestKind.Command;
        var isIdempotent = typeof(IIdempotentRequest).IsAssignableFrom(requestType);
        var requiresExpectedVersion = typeof(IExpectedVersionRequest).IsAssignableFrom(requestType);

        if (isIdempotent && (kind != ApplicationRequestKind.Command || dataAccess != ApplicationDataAccessKind.Write))
        {
            throw Misconfigured(requestType, "Idempotency is valid only for write commands.");
        }

        if (requiresExpectedVersion && (kind != ApplicationRequestKind.Command || dataAccess != ApplicationDataAccessKind.Write))
        {
            throw Misconfigured(requestType, "ExpectedVersion is valid only for write commands.");
        }

        if (scope == ApplicationScopeKind.Token)
        {
            var tokenProperty = requestType.GetProperty("Token", BindingFlags.Instance | BindingFlags.Public);
            if (tokenProperty?.PropertyType != typeof(string))
            {
                throw Misconfigured(requestType, "Token-scoped requests require a public string Token property.");
            }
        }

        if (principal == ApplicationPrincipalKind.Anonymous
            && scope is ApplicationScopeKind.Account or ApplicationScopeKind.Workspace or ApplicationScopeKind.Resource)
        {
            throw Misconfigured(requestType, "Anonymous request cannot be tenant/resource scoped.");
        }

        if (principal == ApplicationPrincipalKind.Anonymous
            && typeof(IRequirePermission).IsAssignableFrom(requestType))
        {
            throw Misconfigured(requestType, "Anonymous request cannot require permission.");
        }

        if (scope == ApplicationScopeKind.Global && typeof(IRequirePermission).IsAssignableFrom(requestType))
        {
            throw Misconfigured(requestType, "Global request cannot require tenant/resource permission.");
        }

        if (requiresExpectedVersion && scope == ApplicationScopeKind.Global)
        {
            throw Misconfigured(requestType, "ExpectedVersion command must be Resource or Workspace scoped, not Global.");
        }

        return new RequestDescriptor(
            requestType,
            kind,
            principal,
            scope,
            dataAccess,
            new AccessRequirements(
                typeof(IRequirePermission).IsAssignableFrom(requestType),
                typeof(IRequireVerifiedEmail).IsAssignableFrom(requestType),
                typeof(IRequireSubscription).IsAssignableFrom(requestType),
                typeof(IRequireFeature).IsAssignableFrom(requestType)),
            isIdempotent,
            requiresExpectedVersion);
    }

    private static TClassification SingleClassification<TClassification>(
        Type requestType,
        string category,
        params (Type Marker, TClassification Classification)[] candidates)
    {
        var matches = candidates
            .Where(candidate => candidate.Marker.IsAssignableFrom(requestType))
            .ToArray();

        if (matches.Length != 1)
        {
            throw Misconfigured(
                requestType,
                $"Exactly one {category} marker is required; found {matches.Length}.");
        }

        return matches[0].Classification;
    }

    private static bool ImplementsOpenGeneric(Type type, Type genericDefinition) =>
        type.GetInterfaces().Any(candidate =>
            candidate.IsGenericType && candidate.GetGenericTypeDefinition() == genericDefinition);

    private static SecurityMisconfigurationException Misconfigured(Type requestType, string message) =>
        new($"Invalid request contract for {requestType.FullName}: {message}");
}
