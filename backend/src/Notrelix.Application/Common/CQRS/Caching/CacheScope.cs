namespace Notrelix.Application.Common.CQRS.Caching;

/// <summary>
/// Defines the isolation scope for a cache entry.
/// Public entries are shared across all tenants/users.
/// Scoped entries are isolated by tenant, user, or both.
/// </summary>
public enum CacheScope
{
    /// <summary>Shared across all tenants and users. Must not contain private data.</summary>
    Public = 0,

    /// <summary>Isolated by account ID.</summary>
    Account = 1,

    /// <summary>Isolated by workspace ID.</summary>
    Workspace = 2,

    /// <summary>Isolated by user ID.</summary>
    User = 3,

    /// <summary>Isolated by workspace + user.</summary>
    WorkspaceUser = 4,

    /// <summary>Isolated by workspace + user + permission version.</summary>
    WorkspaceUserPermission = 5
}
