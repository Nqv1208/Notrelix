using Microsoft.Net.Http.Headers;
using Notrelix.Application.Features.Collaboration.Attachments.DTOs;
using Notrelix.Application.Features.Collaboration.Attachments.Services;

namespace Notrelix.API.Endpoints.Collaboration.Attachments;

/// <summary>
/// The one response shape of an authorised attachment download, shared by every target route:
/// inline only for sniffed png/jpeg/gif/webp, attachment for everything else, never cacheable
/// by shared caches, never sniffed, never scriptable.
/// </summary>
internal static class AttachmentContentResponse
{
    public static IResult Create(HttpContext httpContext, AttachmentContentDto content)
    {
        var inline = AttachmentContentClassifier.IsInlineSafe(content.ContentType);

        var disposition = new ContentDispositionHeaderValue(inline ? "inline" : "attachment");
        disposition.SetHttpFileName(content.FileName);

        var headers = httpContext.Response.Headers;
        headers[HeaderNames.ContentDisposition] = disposition.ToString();
        headers[HeaderNames.XContentTypeOptions] = "nosniff";
        headers[HeaderNames.CacheControl] = "private, no-store";
        headers[HeaderNames.ContentSecurityPolicy] = "default-src 'none'; sandbox";

        // The result disposes the stream once the response has been written.
        return Results.Stream(content.Content, content.ContentType, enableRangeProcessing: false);
    }
}
