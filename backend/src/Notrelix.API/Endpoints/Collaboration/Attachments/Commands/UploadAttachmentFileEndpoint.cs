using Microsoft.Extensions.Options;
using Notrelix.API.Contracts.Collaboration.Attachments.Requests;
using Notrelix.API.ErrorHandling;
using Notrelix.API.Extensions;
using Notrelix.API.Uploads;
using Notrelix.Application.Features.Collaboration.Attachments;
using Notrelix.Application.Features.Collaboration.Attachments.Commands.UploadBoardItemAttachment;
using Notrelix.Application.Features.Collaboration.Attachments.DTOs;

namespace Notrelix.API.Endpoints.Collaboration.Attachments.Commands;

public static class UploadAttachmentFileEndpoint
{
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
        var part = await MultipartUpload.ReadFilePartAsync(
            httpContext,
            options.Value.MaxFileSizeBytes,
            ErrorCodes.AttachmentUnsupportedMediaType,
            ErrorCodes.AttachmentPayloadTooLarge,
            cancellationToken);
        if (part.Failure is not null)
        {
            return part.Failure;
        }

        var result = await sender.Send(
            new UploadBoardItemAttachmentCommand(boardItemId, part.FileName, part.Body!), cancellationToken);

        if (!result.Succeeded)
        {
            return result.ToApiResult();
        }

        return Results.Created(
            $"/api/v1/board-items/{boardItemId}/attachments/{result.Data!.Id}/content", result.Data);
    }
}
