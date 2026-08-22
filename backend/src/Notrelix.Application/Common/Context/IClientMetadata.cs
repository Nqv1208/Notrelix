namespace Notrelix.Application.Common.Context;

/// <summary>
/// Client-originated request metadata (IP address, user agent) captured from the
/// transport and bound into new session records. Infrastructure supplies the
/// actual values; Application consumes them as plain facts.
/// </summary>
public interface IClientMetadata
{
    string? IpAddress { get; }
    string? UserAgent { get; }
}
