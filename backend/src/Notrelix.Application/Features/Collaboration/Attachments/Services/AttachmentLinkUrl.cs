namespace Notrelix.Application.Features.Collaboration.Attachments.Services;

/// <summary>
/// The only addresses a link attachment (board item or page, UC-COL-B07 and B09) may register: an absolute
/// http or https URL with a host. Every other scheme (javascript:, data:, vbscript:, file:, mailto: and any
/// other) and every value that does not parse as an absolute URL is refused.
/// </summary>
public static class AttachmentLinkUrl
{
    public const int MaxLength = 2048;

    public static bool IsAllowed(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        return (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && !string.IsNullOrEmpty(uri.Host);
    }
}
