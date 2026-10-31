namespace Notrelix.Application.Features.Identity.OAuth;

/// <summary>
/// The provider's <c>error</c> and <c>error_description</c> arrive in the callback query string, so anyone
/// can write them. A value that goes into a log line loses its control characters (a line break would
/// forge a log entry) and is cut short.
/// </summary>
public static class OAuthCallbackLog
{
    private const int MaxLength = 100;

    public static string Sanitize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var cleaned = new string(value.Select(c => char.IsControl(c) ? ' ' : c).ToArray());
        return cleaned.Length <= MaxLength ? cleaned : cleaned[..MaxLength];
    }
}
