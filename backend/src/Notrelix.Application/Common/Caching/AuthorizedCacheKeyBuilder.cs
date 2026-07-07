namespace Notrelix.Application.Common.Caching;

public static class AuthorizedCacheKeyBuilder
{
    public static string ForWorkspaceResource(
        string resourceType,
        Guid resourceId,
        Guid? accountId = null,
        Guid? workspaceId = null,
        Guid? userId = null,
        string? queryName = null)
    {
        var components = new List<string>();

        if (queryName is not null)
            components.Add(queryName);

        if (accountId.HasValue)
            components.Add($"acc:{accountId.Value}");
        if (workspaceId.HasValue)
            components.Add($"ws:{workspaceId.Value}");
        if (userId.HasValue)
            components.Add($"u:{userId.Value}");

        components.Add($"{resourceType}:{resourceId}");

        return string.Join(":", components);
    }

    public static string ForAccountResource(
        string resourceType,
        Guid resourceId,
        Guid? accountId = null,
        Guid? userId = null,
        string? queryName = null)
    {
        var components = new List<string>();

        if (queryName is not null)
            components.Add(queryName);

        if (accountId.HasValue)
            components.Add($"acc:{accountId.Value}");
        if (userId.HasValue)
            components.Add($"u:{userId.Value}");

        components.Add($"{resourceType}:{resourceId}");

        return string.Join(":", components);
    }
}
