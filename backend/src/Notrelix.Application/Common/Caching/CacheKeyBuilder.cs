namespace Notrelix.Application.Common.Caching;

/// <summary>
/// Builds structured cache keys with tenant/user scope isolation.
/// Pattern: notrelix:v{schemaVersion}:{environment}:{scope}:{accountId}:{workspaceId}:{userId}:{permissionVersion}:{requestName}:{requestHash}
/// </summary>
public static class CacheKeyBuilder
{
    public const int SchemaVersion = 1;

    public static string Build(
        CacheScope scope,
        string requestName,
        string requestHash,
        Guid? accountId = null,
        Guid? workspaceId = null,
        Guid? userId = null,
        string? permissionVersion = null,
        string? environment = null)
    {
        var parts = new List<string>
        {
            "notrelix",
            $"v{SchemaVersion}",
            environment ?? "unknown",
            scope.ToString().ToLowerInvariant(),
            accountId?.ToString() ?? "",
            workspaceId?.ToString() ?? "",
            userId?.ToString() ?? "",
            permissionVersion ?? "",
            requestName,
            requestHash
        };

        return string.Join(":", parts);
    }

    public static string BuildHash(string value)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant()[..16];
    }
}
