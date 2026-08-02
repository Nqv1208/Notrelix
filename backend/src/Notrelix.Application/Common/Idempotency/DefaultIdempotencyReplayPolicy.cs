using System.Text;
using Microsoft.Extensions.Options;

namespace Notrelix.Application.Common.Idempotency;

/// <summary>
/// Default replay policy enforcing size limits and sensitive-type exclusion.
/// No generic reflection secret scanner — only explicit type exclusion.
/// </summary>
public sealed class DefaultIdempotencyReplayPolicy : IIdempotencyReplayPolicy
{
    private readonly IdempotencyOptions _options;
    private readonly HashSet<string> _sensitiveTypes;

    public DefaultIdempotencyReplayPolicy(IOptions<IdempotencyOptions> options)
    {
        _options = options.Value;
        _sensitiveTypes = new HashSet<string>(_options.SensitiveResultTypes, StringComparer.Ordinal);
    }

    public bool CanCacheResult<TResponse>(TResponse response, string serializedResult)
    {
        var typeName = typeof(TResponse).FullName ?? typeof(TResponse).Name;

        if (_sensitiveTypes.Contains(typeName))
            return false;

        var byteCount = Encoding.UTF8.GetByteCount(serializedResult);
        if (byteCount > _options.MaxResultBytes)
            return false;

        return true;
    }
}
