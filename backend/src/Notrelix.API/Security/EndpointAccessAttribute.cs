namespace Notrelix.API.Security;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class PublicEndpointAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class WorkspaceScopedEndpointAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class ResourceScopedEndpointAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class AdminEndpointAttribute : Attribute;
