using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Notrelix.API.Contracts.Collaboration.Attachments.Requests;
using Notrelix.API.ErrorHandling;
using Notrelix.API.Extensions;
using Notrelix.API.Idempotency;
using Notrelix.Application.Features.Collaboration.Attachments;
using Notrelix.Application.Features.Collaboration.Attachments.Commands.UploadBoardItemAttachment;
using Notrelix.Application.Features.Collaboration.Attachments.DTOs;

namespace Notrelix.API.Endpoints.Collaboration.Attachments.Commands;

public static class UploadAttachmentFileEndpoint
{
    /// <summary>Allowance for multipart framing (boundaries, part headers) on top of the file cap.</summary>
    private const long MultipartOverheadBytes = 1024 * 1024;

    private const string FilePartName = "file";

    public static IEndpointRouteBuilder MapUploadAttachmentFile(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/files", HandleAsync)
            .WithIdempotencyKey()
            .WithName("Collaboration.Attachments.UploadFile")
            .WithTags("Collaboration.Attachments")
            .WithSummary("Upload a file as a board item attachment (multipart/form-data, streamed)")
            .Accepts<UploadBoardItemAttachmentForm>("multipart/form-data")
            .Produces<AttachmentDto>(StatusCodes.Status201Created, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardItemId,
        HttpContext httpContext,
        ISender sender,
        IOptions<AttachmentUploadOptions> options,
        CancellationToken cancellationToken)
    {
        var request = httpContext.Request;
        var maxRequestBytes = options.Value.MaxFileSizeBytes + MultipartOverheadBytes;

        if (!MediaTypeHeaderValue.TryParse(request.ContentType, out var mediaType)
            || !mediaType.MediaType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(HeaderUtilities.RemoveQuotes(mediaType.Boundary).Value))
        {
            return Problem(StatusCodes.Status415UnsupportedMediaType, ErrorCodes.AttachmentUnsupportedMediaType,
                "Unsupported media type", "The upload must be multipart/form-data with a boundary.");
        }

        // Cheap early rejection from the declared length; the real cap is enforced while streaming.
        if (request.ContentLength > maxRequestBytes)
        {
            return Problem(StatusCodes.Status413PayloadTooLarge, ErrorCodes.AttachmentPayloadTooLarge,
                "Payload too large", $"The upload exceeds the maximum allowed size of {options.Value.MaxFileSizeBytes} bytes.");
        }

        // Hard transport limit, raised for this route only (null/read-only on hosts without the feature).
        var sizeFeature = httpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (sizeFeature is { IsReadOnly: false })
        {
            sizeFeature.MaxRequestBodySize = maxRequestBytes;
        }

        var reader = new MultipartReader(HeaderUtilities.RemoveQuotes(mediaType.Boundary).Value!, request.Body)
        {
            HeadersLengthLimit = 16 * 1024,
        };

        MultipartSection? section;
        try
        {
            do
            {
                section = await reader.ReadNextSectionAsync(cancellationToken);
            }
            while (section is not null && !IsFilePart(section, out _));
        }
        catch (InvalidDataException)
        {
            return EndpointExtensions.InvalidInput("The multipart body is malformed.");
        }

        if (section is null || !IsFilePart(section, out var fileName))
        {
            return EndpointExtensions.InvalidInput("A multipart part named 'file' with a file name is required.");
        }

        var result = await sender.Send(
            new UploadBoardItemAttachmentCommand(boardItemId, fileName, section.Body), cancellationToken);

        if (!result.Succeeded)
        {
            return result.ToApiResult();
        }

        return Results.Created(
            $"/api/v1/board-items/{boardItemId}/attachments/{result.Data!.Id}/content", result.Data);
    }

    private static bool IsFilePart(MultipartSection section, out string fileName)
    {
        fileName = string.Empty;
        if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var disposition)
            || !disposition.DispositionType.Equals("form-data", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(HeaderUtilities.RemoveQuotes(disposition.Name).Value, FilePartName, StringComparison.Ordinal))
        {
            return false;
        }

        var raw = disposition.FileNameStar.HasValue ? disposition.FileNameStar : disposition.FileName;
        var name = HeaderUtilities.RemoveQuotes(raw).Value;
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        fileName = name;
        return true;
    }

    private static IResult Problem(int status, string errorCode, string title, string detail)
    {
        var problem = new ProblemDetails
        {
            Type = $"https://docs.notrelix.com/problems/{errorCode.Replace('.', '-')}",
            Title = title,
            Status = status,
            Detail = detail,
        };
        problem.Extensions["errorCode"] = errorCode;
        problem.Extensions["traceId"] = System.Diagnostics.Activity.Current?.Id ?? "unknown";
        return TypedResults.Problem(problem);
    }
}
