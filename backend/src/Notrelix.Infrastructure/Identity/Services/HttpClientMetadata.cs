namespace Notrelix.Infrastructure.Identity.Services;

/// <summary>
/// Client request metadata resolved from the current HTTP context.
/// Null when no HTTP context exists (background/system execution).
/// </summary>
public class HttpClientMetadata : IClientMetadata
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpClientMetadata(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.Request.Headers.UserAgent;
            return string.IsNullOrEmpty(value) ? null : value.ToString();
        }
    }
}