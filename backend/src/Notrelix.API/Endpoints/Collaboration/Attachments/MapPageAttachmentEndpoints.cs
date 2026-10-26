using Microsoft.Extensions.Options;
using Notrelix.API.Contracts.Collaboration.Attachments.Requests;
using Notrelix.API.ErrorHandling;
using Notrelix.API.Extensions;
using Notrelix.API.Uploads;
using Notrelix.Application.Features.Collaboration.Attachments;
using Notrelix.Application.Features.Collaboration.Attachments.Commands.CreatePageAttachment;
using Notrelix.Application.Features.Collaboration.Attachments.Commands.DeletePageAttachment;
using Notrelix.Application.Features.Collaboration.Attachments.Commands.UploadPageAttachment;
using Notrelix.Application.Features.Collaboration.Attachments.DTOs;
using Notrelix.Application.Features.Collaboration.Attachments.Queries.GetPageAttachmentContent;
using Notrelix.Application.Features.Collaboration.Attachments.Queries.GetPageAttachments;

namespace Notrelix.API.Endpoints.Collaboration.Attachments;

/// <summary>
/// Attachments of a Documents page (image and file blocks). Same file policy and download shape as
/// the board item attachments; authorization is the page's own: view to list and read, edit to
/// register, upload and delete. There is no public or unauthenticated address.
/// </summary>
public static class MapPageAttachmentEndpoints
{
    public static IEndpointRouteBuilder MapPageAttachmentsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/pages/{pageId:guid}/attachments")
            .WithTags("Collaboration.PageAttachments")
            .WithOpenApi();

        group.MapResourceGet("/", ListAsync)
            .WithName("Collaboration.PageAttachments.Get")
            .WithTags("Collaboration.PageAttachments")
            .WithSummary("Get page attachments");

        group.MapResourcePost("/", CreateAsync)
            .WithName("Collaboration.PageAttachments.Create")
            .WithTags("Collaboration.PageAttachments")
            .WithSummary("Register page attachment metadata (link)");

        group.MapResourcePost("/files", UploadAsync)
            .WithIdempotencyKey()
            .WithName("Collaboration.PageAttachments.UploadFile")
            .WithTags("Collaboration.PageAttachments")
            .WithSummary("Upload a file as a page attachment (multipart/form-data, streamed)")
            .Accepts<UploadBoardItemAttachmentForm>("multipart/form-data")
            .Produces<AttachmentDto>(StatusCodes.Status201Created, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType);

        group.MapResourceGet("/{attachmentId:guid}/content", GetContentAsync)
            .WithName("Collaboration.PageAttachments.GetContent")
            .WithTags("Collaboration.PageAttachments")
            .WithSummary("Download the stored file of a page attachment (authorised, streamed)")
            .Produces(StatusCodes.Status200OK, contentType: "application/octet-stream")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapResourceDelete("/{attachmentId:guid}", DeleteAsync)
            .WithName("Collaboration.PageAttachments.Delete")
            .WithTags("Collaboration.PageAttachments")
            .WithSummary("Delete a page attachment");

        return app;
    }

    private static async Task<IResult> ListAsync(Guid pageId, ISender sender)
    {
        var result = await sender.Send(new GetPageAttachmentsQuery(pageId));
        return result.ToApiResult();
    }

    private static async Task<IResult> CreateAsync(Guid pageId, CreatePageAttachmentRequest body, ISender sender)
    {
        var result = await sender.Send(new CreatePageAttachmentCommand(pageId, body.Filename, body.Url, body.SizeBytes, body.ContentType));
        return result.ToCreatedResult();
    }

    private static async Task<IResult> UploadAsync(
        Guid pageId,
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

        var result = await sender.Send(new UploadPageAttachmentCommand(pageId, part.FileName, part.Body!), cancellationToken);
        if (!result.Succeeded)
        {
            return result.ToApiResult();
        }

        return Results.Created($"/api/v1/pages/{pageId}/attachments/{result.Data!.Id}/content", result.Data);
    }

    private static async Task<IResult> GetContentAsync(
        Guid pageId,
        Guid attachmentId,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPageAttachmentContentQuery(pageId, attachmentId), cancellationToken);
        return result.Succeeded ? AttachmentContentResponse.Create(httpContext, result.Data!) : result.ToApiResult();
    }

    private static async Task<IResult> DeleteAsync(Guid pageId, Guid attachmentId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeletePageAttachmentCommand(pageId, attachmentId), cancellationToken);
        return result.ToNoContentResult();
    }
}
