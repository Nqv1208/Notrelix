using Microsoft.Extensions.Options;
using Notrelix.API.Contracts.Chat.Requests;
using Notrelix.API.Endpoints.Collaboration.Attachments;
using Notrelix.API.ErrorHandling;
using Notrelix.API.Extensions;
using Notrelix.API.Uploads;
using Notrelix.Application.Features.Chat.Attachments.Commands.DeleteMessageAttachment;
using Notrelix.Application.Features.Chat.Attachments.Commands.UploadMessageAttachment;
using Notrelix.Application.Features.Chat.Attachments.DTOs;
using Notrelix.Application.Features.Chat.Attachments.Queries.GetMessageAttachmentContent;
using Notrelix.Application.Features.Chat.Attachments.Queries.ListMessageAttachments;
using Notrelix.Application.Features.Collaboration.Attachments;
using Notrelix.Application.Features.Collaboration.Attachments.DTOs;

namespace Notrelix.API.Endpoints.Chat.Attachments;

/// <summary>
/// Files on a message (UC-CHT-G08, CHT-012). The upload, the file policy and the download shape are the shared attachment flow's, under
/// the target kind chat.message; Chat decides who may attach (an active member, the author) and who may read (a reader of the space).
/// There is no public or unauthenticated address: every download is permission checked.
/// </summary>
public static class MapChatAttachmentEndpoints
{
    public static IEndpointRouteBuilder MapChatAttachmentRoutes(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/chat")
            .WithTags("Chat.Attachments")
            .WithOpenApi();

        group.MapWorkspaceGet("/messages/{messageId:guid}/attachments", ListAsync)
            .WithName("Chat.Attachments.List")
            .WithSummary("List the files of a message")
            .WithDescription("Oldest first. Each file carries its name, size, content type and a downloadPath that streams the bytes with a permission check on every request. A message the caller cannot see, or a private channel the caller is not a member of, is 404. A deleted message has no files.")
            .Produces<List<MessageAttachmentDto>>(StatusCodes.Status200OK)
            .ProducesProblems(StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound);

        group.MapWorkspacePost("/messages/{messageId:guid}/attachments", UploadAsync)
            .WithIdempotencyKey()
            .WithName("Chat.Attachments.Upload")
            .WithSummary("Attach a file to your own message (multipart/form-data, streamed)")
            .WithDescription("Only the author of the message, as an active member of its space, attaches. The shared file policy applies: a blocked extension or an executable file is 415 before any byte is stored, a file over the size cap is 413, a message with the maximum number of files is 400. A system or deleted message is 409; an archived channel is 409 channel-archived; another person's message is 403; a space the caller is not a member of is 404.")
            .Accepts<UploadChatAttachmentForm>("multipart/form-data")
            .Produces<MessageAttachmentDto>(StatusCodes.Status201Created, "application/json")
            .ProducesProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status403Forbidden,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict,
                StatusCodes.Status413PayloadTooLarge,
                StatusCodes.Status415UnsupportedMediaType);

        group.MapWorkspaceGet("/messages/{messageId:guid}/attachments/{attachmentId:guid}/content", GetContentAsync)
            .WithName("Chat.Attachments.GetContent")
            .WithSummary("Download a file of a message (authorised, streamed)")
            .WithDescription("Permission checked on every request: only a reader of the space opens it. The file must belong to this message; any other attachment id is 404. Inline only for png, jpeg, gif and webp; everything else downloads as an attachment. Never cacheable.")
            .Produces(StatusCodes.Status200OK, contentType: "application/octet-stream")
            .ProducesProblems(StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound);

        group.MapWorkspaceDelete("/messages/{messageId:guid}/attachments/{attachmentId:guid}", DeleteAsync)
            .WithName("Chat.Attachments.Delete")
            .WithSummary("Remove a file from your own message")
            .WithDescription("Only the author removes a file, while the channel is not archived. A file that is not on this message is 404.")
            .ProducesProblems(StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> ListAsync(Guid workspaceId, Guid messageId, ISender sender)
    {
        var result = await sender.Send(new ListMessageAttachmentsQuery(workspaceId, messageId));
        return result.ToApiResult();
    }

    private static async Task<IResult> UploadAsync(
        Guid workspaceId,
        Guid messageId,
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
            return part.Failure;

        var result = await sender.Send(new UploadMessageAttachmentCommand(workspaceId, messageId, part.FileName, part.Body!), cancellationToken);
        if (!result.Succeeded)
            return result.ToApiResult();

        return Results.Created(result.Data!.DownloadPath, result.Data);
    }

    private static async Task<IResult> GetContentAsync(
        Guid workspaceId,
        Guid messageId,
        Guid attachmentId,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMessageAttachmentContentQuery(workspaceId, messageId, attachmentId), cancellationToken);
        if (!result.Succeeded)
            return result.ToApiResult();

        var content = result.Data!;
        return AttachmentContentResponse.Create(
            httpContext, new AttachmentContentDto(content.Content, content.FileName, content.ContentType, content.SizeBytes));
    }

    private static async Task<IResult> DeleteAsync(Guid workspaceId, Guid messageId, Guid attachmentId, ISender sender)
    {
        var result = await sender.Send(new DeleteMessageAttachmentCommand(workspaceId, messageId, attachmentId));
        return result.ToNoContentResult();
    }
}
