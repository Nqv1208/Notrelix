namespace Notrelix.API.Security;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class PublicEndpointAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class AuthenticatedEndpointAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class AccountScopedEndpointAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class WorkspaceScopedEndpointAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class ResourceScopedEndpointAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class AdminEndpointAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class InternalEndpointAttribute : Attribute;

/// <summary>
/// The platform operator console: authenticated by the operator's own scheme (its own key and audience), served on
/// the operator database login, and never reachable with a tenant token.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class OperatorEndpointAttribute : Attribute;
