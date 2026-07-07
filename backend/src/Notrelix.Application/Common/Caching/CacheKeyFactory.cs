using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Notrelix.Application.Common.Caching;

public sealed class CacheKeyFactory
{
    private static readonly JsonSerializerOptions HashJsonOptions = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = false
    };

    private readonly CacheKeyOptions _options;

    public CacheKeyFactory(IOptions<CacheKeyOptions> options)
    {
        _options = options.Value;
    }

    public string Public(string requestName, string requestHash)
    {
        return FormatKey("public", "_", "_", "_", "_", requestName, requestHash);
    }

    public string Account(Guid accountId, string requestName, string requestHash)
    {
        if (accountId == Guid.Empty)
            throw new ArgumentException("AccountId must not be empty.", nameof(accountId));

        return FormatKey("account", accountId.ToString(), "_", "_", "_", requestName, requestHash);
    }

    public string Workspace(Guid accountId, Guid workspaceId, string requestName, string requestHash)
    {
        if (accountId == Guid.Empty)
            throw new ArgumentException("AccountId must not be empty.", nameof(accountId));
        if (workspaceId == Guid.Empty)
            throw new ArgumentException("WorkspaceId must not be empty.", nameof(workspaceId));

        return FormatKey("workspace", accountId.ToString(), workspaceId.ToString(), "_", "_", requestName, requestHash);
    }

    public string User(Guid accountId, Guid workspaceId, Guid userId, string requestName, string requestHash)
    {
        if (accountId == Guid.Empty)
            throw new ArgumentException("AccountId must not be empty.", nameof(accountId));
        if (workspaceId == Guid.Empty)
            throw new ArgumentException("WorkspaceId must not be empty.", nameof(workspaceId));
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId must not be empty.", nameof(userId));

        return FormatKey("user", accountId.ToString(), workspaceId.ToString(), userId.ToString(), "_", requestName, requestHash);
    }

    public string Permissioned(Guid accountId, Guid workspaceId, Guid userId, string permissionVersion, string requestName, string requestHash)
    {
        if (accountId == Guid.Empty)
            throw new ArgumentException("AccountId must not be empty.", nameof(accountId));
        if (workspaceId == Guid.Empty)
            throw new ArgumentException("WorkspaceId must not be empty.", nameof(workspaceId));
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId must not be empty.", nameof(userId));
        if (string.IsNullOrWhiteSpace(permissionVersion))
            throw new ArgumentException("PermissionVersion must not be empty.", nameof(permissionVersion));

        return FormatKey("permissioned", accountId.ToString(), workspaceId.ToString(), userId.ToString(), permissionVersion, requestName, requestHash);
    }

    public string BuildHash(object identity)
    {
        var json = JsonSerializer.Serialize(identity, HashJsonOptions);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash).ToLowerInvariant()[..16];
    }

    private string FormatKey(
        string scope,
        string accountId,
        string workspaceId,
        string userId,
        string permissionVersion,
        string requestName,
        string requestHash)
    {
        return $"{_options.Prefix}:v{_options.SchemaVersion}:{_options.Environment}:{scope}:{accountId}:{workspaceId}:{userId}:{permissionVersion}:{requestName}:{requestHash}";
    }
}
